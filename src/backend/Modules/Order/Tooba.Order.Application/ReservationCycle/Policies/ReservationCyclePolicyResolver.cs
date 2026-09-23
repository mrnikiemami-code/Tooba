using Microsoft.Extensions.Options;
using Tooba.Catalog.Contracts.Reservation;

using Tooba.Order.Application.ReservationCycle.Contracts;
namespace Tooba.Order.Application.ReservationCycle.Policies;

/// <summary>
/// Order-owned precedence merge: platform → store → category → offer; multi-line = strictest (min).
/// Catalog overrides via <see cref="IReservationCycleHoldPolicyReader"/> only.
/// </summary>
public sealed class ReservationCyclePolicyResolver : IReservationCyclePolicyResolver
{
    private readonly ReservationCycleOptions _platform;
    private readonly IReservationCycleHoldPolicyReader _holds;

    /// <summary>Platform options + Catalog hold-policy contract.</summary>
    public ReservationCyclePolicyResolver(
        IOptions<ReservationCycleOptions> platform,
        IReservationCycleHoldPolicyReader holds)
    {
        _platform = platform.Value;
        _holds = holds;
    }

    /// <inheritdoc />
    public async Task<ReservationCyclePolicySnapshot> ResolveAsync(
        IReadOnlyList<ReservationCyclePolicyLine> lines,
        CancellationToken cancellationToken)
    {
        var platform = Platform();
        var store = await _holds.GetStoreOverrideAsync(cancellationToken);
        var storeSnap = Merge(
            platform,
            store?.InitialReservationHoldMinutes,
            store?.RetryReservationHoldMinutes,
            store?.MaxReservationCycles,
            "store");

        if (lines.Count == 0)
        {
            return storeSnap.Source == "store" ? storeSnap : platform;
        }

        var offerIds = lines.Select(x => x.OfferId).Distinct().ToArray();
        var categoryIds = lines.Where(x => x.CategoryId is not null).Select(x => x.CategoryId!.Value).Distinct().ToArray();
        var overrides = await _holds.GetOverridesAsync(offerIds, categoryIds, cancellationToken);

        ReservationCyclePolicySnapshot? tightest = null;
        foreach (var line in lines)
        {
            var offer = overrides.FirstOrDefault(x =>
                x.ScopeKind == ReservationCycleHoldOverrideScopes.Offer && x.ScopeId == line.OfferId);
            var category = line.CategoryId is { } cid
                ? overrides.FirstOrDefault(x =>
                    x.ScopeKind == ReservationCycleHoldOverrideScopes.Category && x.ScopeId == cid)
                : null;
            var resolved = storeSnap;
            if (category is not null)
            {
                resolved = Merge(
                    resolved,
                    category.InitialReservationHoldMinutes,
                    category.RetryReservationHoldMinutes,
                    category.MaxReservationCycles,
                    "category");
            }

            if (offer is not null)
            {
                resolved = Merge(
                    resolved,
                    offer.InitialReservationHoldMinutes,
                    offer.RetryReservationHoldMinutes,
                    offer.MaxReservationCycles,
                    "offer");
            }

            tightest = tightest is null ? resolved : Min(tightest, resolved);
        }

        return tightest ?? storeSnap;
    }

    /// <inheritdoc />
    public async Task<ReservationPolicyPreview> PreviewAsync(
        Guid? offerId,
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var many = await PreviewManyAsync(
            [(offerId ?? Guid.Empty, categoryId)],
            cancellationToken);
        return many[0];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReservationPolicyPreview>> PreviewManyAsync(
        IReadOnlyList<(Guid OfferId, Guid? CategoryId)> lines,
        CancellationToken cancellationToken)
    {
        var platform = PlatformLayer();
        var store = await _holds.GetStoreOverrideAsync(cancellationToken);
        var afterStore = Overlay(
            platform,
            store?.InitialReservationHoldMinutes,
            store?.RetryReservationHoldMinutes,
            store?.MaxReservationCycles,
            "store");
        var offerIds = lines
            .Where(x => x.OfferId != Guid.Empty)
            .Select(x => x.OfferId)
            .Distinct()
            .ToArray();
        var categoryIds = lines.Where(x => x.CategoryId is not null).Select(x => x.CategoryId!.Value).Distinct().ToArray();
        var overrides = await _holds.GetOverridesAsync(offerIds, categoryIds, cancellationToken);

        var result = new List<ReservationPolicyPreview>(lines.Count);
        foreach (var line in lines)
        {
            var category = line.CategoryId is { } cid
                ? overrides.FirstOrDefault(x =>
                    x.ScopeKind == ReservationCycleHoldOverrideScopes.Category && x.ScopeId == cid)
                : null;
            var offer = line.OfferId == Guid.Empty
                ? null
                : overrides.FirstOrDefault(x =>
                    x.ScopeKind == ReservationCycleHoldOverrideScopes.Offer && x.ScopeId == line.OfferId);
            var afterCategory = Overlay(
                afterStore,
                category?.InitialReservationHoldMinutes,
                category?.RetryReservationHoldMinutes,
                category?.MaxReservationCycles,
                "category");
            var afterOffer = Overlay(
                afterCategory,
                offer?.InitialReservationHoldMinutes,
                offer?.RetryReservationHoldMinutes,
                offer?.MaxReservationCycles,
                "offer");
            result.Add(new ReservationPolicyPreview(
                platform,
                afterStore,
                afterCategory,
                afterOffer,
                store?.InitialReservationHoldMinutes,
                store?.RetryReservationHoldMinutes,
                store?.MaxReservationCycles,
                category?.InitialReservationHoldMinutes,
                category?.RetryReservationHoldMinutes,
                category?.MaxReservationCycles,
                offer?.InitialReservationHoldMinutes,
                offer?.RetryReservationHoldMinutes,
                offer?.MaxReservationCycles));
        }

        return result;
    }

    private ReservationCyclePolicySnapshot Platform() =>
        new(
            ClampMinutes(_platform.InitialReservationHoldMinutes),
            ClampMinutes(_platform.RetryReservationHoldMinutes),
            ClampMax(_platform.MaxReservationCycles),
            "platform");

    private static ReservationCyclePolicySnapshot Merge(
        ReservationCyclePolicySnapshot fallback,
        int? initial,
        int? retry,
        int? max,
        string source)
    {
        var used = initial is not null || retry is not null || max is not null;
        return new ReservationCyclePolicySnapshot(
            initial is int i ? ClampMinutes(i) : fallback.InitialHoldMinutes,
            retry is int r ? ClampMinutes(r) : fallback.RetryHoldMinutes,
            max is int m ? ClampMax(m) : fallback.MaxCycles,
            used ? source : fallback.Source);
    }

    private static ReservationCyclePolicySnapshot Min(
        ReservationCyclePolicySnapshot a,
        ReservationCyclePolicySnapshot b) =>
        new(
            Math.Min(a.InitialHoldMinutes, b.InitialHoldMinutes),
            Math.Min(a.RetryHoldMinutes, b.RetryHoldMinutes),
            Math.Min(a.MaxCycles, b.MaxCycles),
            a.InitialHoldMinutes <= b.InitialHoldMinutes
            && a.RetryHoldMinutes <= b.RetryHoldMinutes
            && a.MaxCycles <= b.MaxCycles
                ? a.Source
                : $"{a.Source}+{b.Source}");

    private ReservationPolicyLayerPreview PlatformLayer() =>
        new(
            ClampMinutes(_platform.InitialReservationHoldMinutes),
            "platform",
            ClampMinutes(_platform.RetryReservationHoldMinutes),
            "platform",
            ClampMax(_platform.MaxReservationCycles),
            "platform");

    private static ReservationPolicyLayerPreview Overlay(
        ReservationPolicyLayerPreview fallback,
        int? initial,
        int? retry,
        int? max,
        string source) =>
        new(
            initial is int i ? ClampMinutes(i) : fallback.InitialHoldMinutes,
            initial is not null ? source : fallback.InitialSource,
            retry is int r ? ClampMinutes(r) : fallback.RetryHoldMinutes,
            retry is not null ? source : fallback.RetrySource,
            max is int m ? ClampMax(m) : fallback.MaxCycles,
            max is not null ? source : fallback.MaxSource);

    private static int ClampMinutes(int value) => Math.Clamp(value <= 0 ? 120 : value, 1, 24 * 60 * 30);

    private static int ClampMax(int value) => Math.Clamp(value <= 0 ? 3 : value, 1, 20);
}
