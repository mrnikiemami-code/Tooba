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

public sealed class OrderUnpaidRetrySupplyBridge(
    OrderDbContext orders,
    IOrderInventoryLifecyclePort inventory,
    IReservationCycleDirectory cycles,
    IFulfillmentShippedQuantityReader shippedReader,
    IFulfillmentDirectory fulfillment,
    IClock clock) : IOrderUnpaidRetrySupplyPort
{
    public async Task EnsureRetrySupplyAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        await cycles.CloseExpiredDueAsync(now, cancellationToken);
        var group = await orders.Checkouts.Include(x => x.SellerOrders).ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken)
            ?? throw new InvalidOperationException("payment.unpaid.supply_unavailable");
        var projection = await cycles.GetProjectionAsync(checkoutId, now, null, cancellationToken);
        var active = await cycles.GetActiveAsync(checkoutId, cancellationToken);
        if (active is null && projection.RetryCountRemaining <= 0 && projection.TotalCyclesCreated > 0)
        {
            await cycles.RecordRetryLimitReachedAsync(checkoutId, now, cancellationToken);
            throw new InvalidOperationException("inventory.reservation.retry_limit_reached");
        }

        var lines = await BuildLinesAsync(group, cancellationToken);
        if (group.SellerOrders.All(x => x.Status == SellerOrderStatus.Cancelled))
            throw new InvalidOperationException("payment.unpaid.supply_unavailable");
        if (active is null) await cycles.RecordReacquireRequestedAsync(checkoutId, now, cancellationToken);
        var result = await inventory.EnsureUnpaidRetryHoldAsync(
            new(checkoutId, active is null, active is null ? "unpaid-retry" : "active-cycle-retry", lines),
            cancellationToken);
        if (result.Status is "Unavailable" or "PartiallyUnavailable"
            || result.Outcome is "Unavailable" or "PartiallyUnavailable")
        {
            if (active is null)
                await cycles.RecordReacquireFailedAsync(checkoutId, now, result.Status, cancellationToken);
            throw new InvalidOperationException("payment.unpaid.supply_unavailable");
        }

        foreach (var pair in result.NewBindingsByOrderLineId)
        {
            var line = group.SellerOrders.SelectMany(x => x.Lines).Single(x => x.LineId == pair.Key);
            if (line.ReservationId != pair.Value) line.ReplaceReservation(pair.Value);
        }
        if (result.NewBindingsByOrderLineId.Count > 0)
        {
            await orders.SaveChangesAsync(cancellationToken);
            await fulfillment.RebindActiveReservationsFromOrderAsync(checkoutId, cancellationToken);
        }
        if (active is not null) return;

        var reservationIds = result.NewBindingsByOrderLineId.Values.Distinct().ToArray();
        if (reservationIds.Length == 0)
            reservationIds = group.SellerOrders.SelectMany(x => x.Lines)
                .Where(x => x.ReservationId.HasValue).Select(x => x.ReservationId!.Value).Distinct().ToArray();
        var previous = projection.History.LastOrDefault();
        var holdMinutes = previous?.EffectiveHoldMinutes ?? 15;
        var policy = new ReservationCyclePolicySnapshot(
            previous?.EffectiveHoldMinutes ?? holdMinutes,
            holdMinutes,
            projection.EffectiveMaxCycles > 0 ? projection.EffectiveMaxCycles : 2,
            previous?.PolicySource ?? "platform");
        await cycles.StartAsync(
            checkoutId, ReservationCycleReason.RetryAfterExpiry, now, now.AddMinutes(holdMinutes),
            policy, reservationIds, "unpaid-retry",
            $"cycle:{checkoutId:N}:{projection.TotalCyclesCreated + 1}", null, cancellationToken);
    }

    private async Task<IReadOnlyList<OrderInventorySupplyLine>> BuildLinesAsync(
        CheckoutGroup group, CancellationToken cancellationToken)
    {
        var shipped = await shippedReader.GetShippedByOrderLineIdsForCheckoutsAsync(
            [group.CheckoutId], cancellationToken);
        return group.SellerOrders.Where(x => x.Status != SellerOrderStatus.Cancelled)
            .SelectMany(x => x.Lines)
            .Select(x => new OrderInventorySupplyLine(
                x.LineId, x.OfferId, x.ReservationId,
                x.Quantity - shipped.GetValueOrDefault(x.LineId),
                x.UnitDisplaySnapshot, x.UnitCodeSnapshot))
            .ToArray();
    }
}
