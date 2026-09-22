#pragma warning disable CS1591
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Host.Grid;
using Tooba.Host.Storefront;
using Tooba.Order.Application;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Application.Ports;

namespace Tooba.Host.Admin;

/// <summary>Host adapter: order enrichment for Payment admin grid (OrderDbContext stays in Host).</summary>
public sealed class HostPaymentAdminOrderEnrichmentAdapter : IPaymentAdminOrderEnrichmentPort
{
    private readonly OrderDbContext _orders;
    private readonly OrderSupplyComposer _supply;
    private readonly IReservationCycleDirectory _cycles;
    private readonly IClock _clock;

    public HostPaymentAdminOrderEnrichmentAdapter(
        OrderDbContext orders,
        OrderSupplyComposer supply,
        IReservationCycleDirectory cycles,
        IClock clock)
    {
        _orders = orders;
        _supply = supply;
        _cycles = cycles;
        _clock = clock;
    }

    public async Task<IReadOnlyList<Guid>> ResolveSearchCheckoutIdsAsync(
        string search,
        CancellationToken cancellationToken)
    {
        var term = search.Trim().ToLower();
        return await _orders.Checkouts.AsNoTracking()
            .Where(c =>
                c.RecipientName.ToLower().Contains(term)
                || c.CheckoutId.ToString().ToLower().Contains(term)
                || c.SellerOrders.Any(o => o.OrderNumber.ToLower().Contains(term)))
            .Select(c => c.CheckoutId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> ResolveSupplyFilterCheckoutIdsAsync(
        IReadOnlyList<string> wantedValues,
        CancellationToken cancellationToken)
    {
        var wanted = wantedValues.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (wanted.Count == 0)
            return [];

        var checkoutIds = await _orders.Checkouts.AsNoTracking()
            .Select(x => x.CheckoutId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var statuses = await _supply.GetStatusesAsync(checkoutIds, cancellationToken).ConfigureAwait(false);
        return checkoutIds.Where(id =>
                statuses.TryGetValue(id, out var st) && wanted.Contains(st.Status.ToString()))
            .ToList();
    }

    public async Task<IReadOnlyList<Guid>> ResolveReservationFilterCheckoutIdsAsync(
        IReadOnlyList<string> wantedValues,
        CancellationToken cancellationToken)
    {
        var wanted = wantedValues.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (wanted.Count == 0)
            return [];

        var checkoutIds = await _orders.Checkouts.AsNoTracking()
            .Select(x => x.CheckoutId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var supply = await _supply.GetStatusesAsync(checkoutIds, cancellationToken).ConfigureAwait(false);
        var supplyByCheckout = supply.ToDictionary(x => x.Key, x => (string?)x.Value.Status.ToString());
        var cycles = await _cycles.GetProjectionsAsync(
            checkoutIds,
            _clock.UtcNow,
            supplyByCheckout,
            cancellationToken).ConfigureAwait(false);
        return checkoutIds.Where(id =>
        {
            var summary = cycles.TryGetValue(id, out var projection)
                ? AdminReservationCycleMapper.ToSummary(projection)
                : AdminReservationCycleMapper.EmptySummary();
            return wanted.Contains(summary.State) || wanted.Contains(summary.CompactLabelFa);
        }).ToList();
    }

    public async Task<IReadOnlyList<AdminPaymentOrderEnrichmentDto>> EnrichAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken)
    {
        if (checkoutIds.Count == 0)
            return [];

        var checkouts = await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .Where(x => checkoutIds.Contains(x.CheckoutId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var checkoutMap = checkouts.ToDictionary(x => x.CheckoutId);
        var supply = await _supply.GetStatusesAsync(checkoutIds, cancellationToken).ConfigureAwait(false);
        var supplyByCheckout = supply.ToDictionary(x => x.Key, x => (string?)x.Value.Status.ToString());
        var cycles = await _cycles.GetProjectionsAsync(
            checkoutIds,
            _clock.UtcNow,
            supplyByCheckout,
            cancellationToken).ConfigureAwait(false);

        return checkoutIds.Select(checkoutId =>
        {
            checkoutMap.TryGetValue(checkoutId, out var checkout);
            var references = checkout?.SellerOrders
                .Select(x => x.OrderNumber)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList() ?? [];
            var reference = references.Count == 0
                ? checkoutId.ToString("N")[..12]
                : string.Join(" / ", references);
            var customer = checkout is null
                ? "مشتری توبا"
                : StorefrontRecipientNames.DisplayOrFallback(
                    checkout.RecipientFirstName, checkout.RecipientLastName, checkout.RecipientName);
            var supplyStatus = checkout is null
                ? "NotApplicable"
                : supply.TryGetValue(checkoutId, out var st) ? st.Status.ToString() : "NotApplicable";
            var reservation = cycles.TryGetValue(checkoutId, out var projection)
                ? AdminReservationCycleMapper.ToSummary(projection)
                : AdminReservationCycleMapper.EmptySummary();
            return new AdminPaymentOrderEnrichmentDto(
                checkoutId,
                reference,
                customer,
                supplyStatus,
                reservation.CompactLabelFa,
                reservation.CompactLabelEn,
                reservation.State,
                reservation.CycleNumber,
                reservation.RetryPossible,
                reservation.NeedsReacquire,
                reservation.RetryLimitReached);
        }).ToList();
    }
}
