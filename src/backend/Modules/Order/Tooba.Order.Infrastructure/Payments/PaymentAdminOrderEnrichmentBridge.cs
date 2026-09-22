#pragma warning disable CS1591
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Order.Application;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Payments;

public sealed class PaymentAdminOrderEnrichmentBridge(
    OrderDbContext orders,
    IOrderInventoryLifecyclePort inventory,
    IReservationCycleDirectory cycles,
    IFulfillmentShippedQuantityReader shippedReader,
    IClock clock) : IPaymentAdminOrderEnrichmentReader
{
    public async Task<IReadOnlyList<Guid>> ResolveSearchCheckoutIdsAsync(string search, CancellationToken cancellationToken)
    {
        var term = search.Trim().ToLower();
        return await orders.Checkouts.AsNoTracking()
            .Where(c => c.RecipientName.ToLower().Contains(term)
                || c.CheckoutId.ToString().ToLower().Contains(term)
                || c.SellerOrders.Any(o => o.OrderNumber.ToLower().Contains(term)))
            .Select(c => c.CheckoutId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ResolveSupplyFilterCheckoutIdsAsync(
        IReadOnlyList<string> wantedValues, CancellationToken cancellationToken)
    {
        var wanted = wantedValues.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (wanted.Count == 0) return [];
        var ids = await orders.Checkouts.AsNoTracking().Select(x => x.CheckoutId).ToListAsync(cancellationToken);
        var statuses = await SupplyAsync(ids, cancellationToken);
        return ids.Where(id => statuses.TryGetValue(id, out var status) && wanted.Contains(status)).ToArray();
    }

    public async Task<IReadOnlyList<Guid>> ResolveReservationFilterCheckoutIdsAsync(
        IReadOnlyList<string> wantedValues, CancellationToken cancellationToken)
    {
        var wanted = wantedValues.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (wanted.Count == 0) return [];
        var ids = await orders.Checkouts.AsNoTracking().Select(x => x.CheckoutId).ToListAsync(cancellationToken);
        var supply = await SupplyAsync(ids, cancellationToken);
        var projections = await cycles.GetProjectionsAsync(
            ids, clock.UtcNow, supply.ToDictionary(x => x.Key, x => (string?)x.Value), cancellationToken);
        return ids.Where(id =>
        {
            var summary = Summary(projections.GetValueOrDefault(id));
            return wanted.Contains(summary.State) || wanted.Contains(summary.Fa);
        }).ToArray();
    }

    public async Task<IReadOnlyList<AdminPaymentOrderEnrichmentSnapshot>> EnrichAsync(
        IReadOnlyList<Guid> checkoutIds, CancellationToken cancellationToken)
    {
        if (checkoutIds.Count == 0) return [];
        var groups = await orders.Checkouts.AsNoTracking().Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines).Where(x => checkoutIds.Contains(x.CheckoutId))
            .ToListAsync(cancellationToken);
        var map = groups.ToDictionary(x => x.CheckoutId);
        var supply = await SupplyAsync(checkoutIds, cancellationToken);
        var projections = await cycles.GetProjectionsAsync(
            checkoutIds, clock.UtcNow, supply.ToDictionary(x => x.Key, x => (string?)x.Value), cancellationToken);
        return checkoutIds.Select(id =>
        {
            map.TryGetValue(id, out var group);
            var references = group?.SellerOrders.Select(x => x.OrderNumber)
                .Where(x => !string.IsNullOrWhiteSpace(x)).ToArray() ?? [];
            var summary = Summary(projections.GetValueOrDefault(id));
            return new AdminPaymentOrderEnrichmentSnapshot(
                id, references.Length == 0 ? id.ToString("N")[..12] : string.Join(" / ", references),
                group is null ? "مشتری توبا" : DisplayName(
                    group.RecipientFirstName, group.RecipientLastName, group.RecipientName),
                supply.GetValueOrDefault(id, "NotApplicable"),
                summary.Fa, summary.En, summary.State, summary.Cycle,
                summary.Retry, summary.Reacquire, summary.Limit);
        }).ToArray();
    }

    private async Task<Dictionary<Guid, string>> SupplyAsync(
        IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        var groups = await orders.Checkouts.AsNoTracking().Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines).Where(x => ids.Contains(x.CheckoutId)).ToListAsync(cancellationToken);
        var shipped = await shippedReader.GetShippedByOrderLineIdsForCheckoutsAsync(ids, cancellationToken);
        var input = groups.ToDictionary(x => x.CheckoutId, x => (IReadOnlyList<OrderInventorySupplyLine>)
            x.SellerOrders.Where(o => o.Status != SellerOrderStatus.Cancelled).SelectMany(o => o.Lines)
            .Select(line => new OrderInventorySupplyLine(
                line.LineId, line.OfferId, line.ReservationId,
                line.Quantity - shipped.GetValueOrDefault(line.LineId),
                line.UnitDisplaySnapshot, line.UnitCodeSnapshot)).ToArray());
        var statuses = await inventory.GetSupplyStatusesAsync(input, cancellationToken);
        var result = ids.ToDictionary(x => x, _ => "NotApplicable");
        foreach (var pair in statuses) result[pair.Key] = pair.Value.Status;
        return result;
    }

    private static string DisplayName(string? first, string? last, string? legacy)
    {
        var canonical = string.Join(" ", new[] { first, last }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        return !string.IsNullOrWhiteSpace(canonical) ? canonical
            : !string.IsNullOrWhiteSpace(legacy) ? legacy.Trim() : "مشتری توبا";
    }

    private static (string Fa, string En, string State, int? Cycle, bool Retry, bool Reacquire, bool Limit)
        Summary(ReservationCycleProjection? p)
    {
        if (p is null || (p.CurrentStatus is null && p.TotalCyclesCreated == 0))
            return ("—", "—", "none", null, false, false, false);
        var status = p.CurrentStatus;
        var cycle = p.CurrentCycleNumber;
        var fa = status switch {
            ReservationCycleStatus.Active => cycle is int n ? $"فعال #{n}" : "فعال",
            ReservationCycleStatus.Expired => cycle is int n ? $"پایان‌یافته #{n}" : "پایان‌یافته",
            ReservationCycleStatus.CommittedPaid => "نهایی‌شده",
            ReservationCycleStatus.ReleasedByCancel or ReservationCycleStatus.ReleasedByPolicy => cycle is int n ? $"آزادشده #{n}" : "آزادشده",
            ReservationCycleStatus.ReacquireFailed => "رزرو مجدد ناموفق", _ => "—" };
        var en = status switch {
            ReservationCycleStatus.Active => cycle is int n ? $"Active #{n}" : "Active",
            ReservationCycleStatus.Expired => cycle is int n ? $"Expired #{n}" : "Expired",
            ReservationCycleStatus.CommittedPaid => "Committed",
            ReservationCycleStatus.ReleasedByCancel or ReservationCycleStatus.ReleasedByPolicy => cycle is int n ? $"Released #{n}" : "Released",
            ReservationCycleStatus.ReacquireFailed => "Reacquire failed", _ => "—" };
        var state = status switch {
            ReservationCycleStatus.Active => "active", ReservationCycleStatus.Expired => "expired",
            ReservationCycleStatus.CommittedPaid => "committed",
            ReservationCycleStatus.ReleasedByCancel or ReservationCycleStatus.ReleasedByPolicy => "released",
            ReservationCycleStatus.ReacquireFailed => "reacquireFailed", _ => "none" };
        var limit = p.RetryCountRemaining <= 0 && status is not ReservationCycleStatus.Active
            && status is not ReservationCycleStatus.CommittedPaid && p.TotalCyclesCreated > 0;
        var retry = p.RetryCountRemaining > 0
            && status is ReservationCycleStatus.Expired or ReservationCycleStatus.ReleasedByPolicy;
        var reacquire = status is not ReservationCycleStatus.Active and not ReservationCycleStatus.CommittedPaid
            && p.SupplyStatus is "AvailableForReacquire" or "Unavailable" or "PartiallyUnavailable";
        return (fa, en, state, cycle, retry, reacquire, limit);
    }
}
