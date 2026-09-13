#pragma warning disable CS1591
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Order.Application;

namespace Tooba.Host;

/// <summary>تقدم Offer &gt; Category &gt; Store &gt; Platform؛ چندخط = حداقل TTL و سخت‌گیرانه‌ترین سقف.</summary>
public sealed class ReservationCyclePolicyResolver : IReservationCyclePolicyResolver
{
    private readonly ReservationCycleOptions _platform;
    private readonly CatalogDbContext _catalog;

    public ReservationCyclePolicyResolver(
        IOptions<ReservationCycleOptions> platform,
        CatalogDbContext catalog)
    {
        _platform = platform.Value;
        _catalog = catalog;
    }

    public async Task<ReservationCyclePolicySnapshot> ResolveAsync(
        IReadOnlyList<ReservationCyclePolicyLine> lines,
        CancellationToken cancellationToken)
    {
        var platform = Platform();
        var store = await _catalog.StoreHoldPolicySettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreHoldPolicySettings.SingletonId, cancellationToken);
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
        var overrides = await _catalog.ReservationCyclePolicyOverrides.AsNoTracking()
            .Where(x =>
                (x.ScopeKind == ReservationCyclePolicyOverride.OfferScope && offerIds.Contains(x.ScopeId))
                || (x.ScopeKind == ReservationCyclePolicyOverride.CategoryScope && categoryIds.Contains(x.ScopeId)))
            .ToListAsync(cancellationToken);

        ReservationCyclePolicySnapshot? tightest = null;
        foreach (var line in lines)
        {
            var offer = overrides.FirstOrDefault(x =>
                x.ScopeKind == ReservationCyclePolicyOverride.OfferScope && x.ScopeId == line.OfferId);
            var category = line.CategoryId is { } cid
                ? overrides.FirstOrDefault(x =>
                    x.ScopeKind == ReservationCyclePolicyOverride.CategoryScope && x.ScopeId == cid)
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

    public async Task<ReservationPolicyPreview> PreviewAsync(
        Guid? offerId,
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var platform = PlatformLayer();
        var store = await _catalog.StoreHoldPolicySettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreHoldPolicySettings.SingletonId, cancellationToken);
        var afterStore = Overlay(
            platform,
            store?.InitialReservationHoldMinutes,
            store?.RetryReservationHoldMinutes,
            store?.MaxReservationCycles,
            "store");

        ReservationCyclePolicyOverride? category = null;
        if (categoryId is { } cid)
        {
            category = await _catalog.ReservationCyclePolicyOverrides.AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.ScopeKind == ReservationCyclePolicyOverride.CategoryScope && x.ScopeId == cid,
                    cancellationToken);
        }

        var afterCategory = Overlay(
            afterStore,
            category?.InitialReservationHoldMinutes,
            category?.RetryReservationHoldMinutes,
            category?.MaxReservationCycles,
            "category");

        ReservationCyclePolicyOverride? offer = null;
        if (offerId is { } oid)
        {
            offer = await _catalog.ReservationCyclePolicyOverrides.AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.ScopeKind == ReservationCyclePolicyOverride.OfferScope && x.ScopeId == oid,
                    cancellationToken);
        }

        var afterOffer = Overlay(
            afterCategory,
            offer?.InitialReservationHoldMinutes,
            offer?.RetryReservationHoldMinutes,
            offer?.MaxReservationCycles,
            "offer");

        return new ReservationPolicyPreview(
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
            offer?.MaxReservationCycles);
    }

    /// <summary>یک بار store/overrides را می‌خواند و پیش‌نمایش چند Offer را برمی‌گرداند.</summary>
    public async Task<IReadOnlyList<ReservationPolicyPreview>> PreviewManyAsync(
        IReadOnlyList<(Guid OfferId, Guid? CategoryId)> lines,
        CancellationToken cancellationToken)
    {
        var platform = PlatformLayer();
        var store = await _catalog.StoreHoldPolicySettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreHoldPolicySettings.SingletonId, cancellationToken);
        var afterStore = Overlay(
            platform,
            store?.InitialReservationHoldMinutes,
            store?.RetryReservationHoldMinutes,
            store?.MaxReservationCycles,
            "store");
        var offerIds = lines.Select(x => x.OfferId).Distinct().ToArray();
        var categoryIds = lines.Where(x => x.CategoryId is not null).Select(x => x.CategoryId!.Value).Distinct().ToArray();
        var overrides = await _catalog.ReservationCyclePolicyOverrides.AsNoTracking()
            .Where(x =>
                (x.ScopeKind == ReservationCyclePolicyOverride.OfferScope && offerIds.Contains(x.ScopeId))
                || (x.ScopeKind == ReservationCyclePolicyOverride.CategoryScope && categoryIds.Contains(x.ScopeId)))
            .ToListAsync(cancellationToken);

        var result = new List<ReservationPolicyPreview>(lines.Count);
        foreach (var line in lines)
        {
            var category = line.CategoryId is { } cid
                ? overrides.FirstOrDefault(x =>
                    x.ScopeKind == ReservationCyclePolicyOverride.CategoryScope && x.ScopeId == cid)
                : null;
            var offer = overrides.FirstOrDefault(x =>
                x.ScopeKind == ReservationCyclePolicyOverride.OfferScope && x.ScopeId == line.OfferId);
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
