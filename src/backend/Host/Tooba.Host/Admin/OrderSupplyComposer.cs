#pragma warning disable CS1591
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application;
using Tooba.Inventory.Application;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Infrastructure;

namespace Tooba.Host.Admin;

/// <summary>
/// Facade تأمین سفارش: ساخت خط‌ها از Order/Fulfillment و فراخوانی Inventory EnsureOrderSupply.
/// </summary>
public sealed class OrderSupplyComposer
{
    private readonly OrderDbContext _orders;
    private readonly IInventoryDirectory _inventory;
    private readonly IFulfillmentDirectory _fulfillment;
    private readonly PaymentGatewayOptions _paymentGateway;

    public OrderSupplyComposer(
        OrderDbContext orders,
        IInventoryDirectory inventory,
        IFulfillmentDirectory fulfillment,
        IOptions<PaymentGatewayOptions> paymentGateway)
    {
        _orders = orders;
        _inventory = inventory;
        _fulfillment = fulfillment;
        _paymentGateway = paymentGateway.Value;
    }

    public DateTimeOffset ResolveManualReviewExpiresAt()
    {
        var hours = Math.Clamp(_paymentGateway.ManualPaymentReviewHoldHours, 1, 24 * 30);
        // Precedence: Payment:Gateway (method/store) overrides Inventory:OrderSupplyHolds platform defaults.
        if (_paymentGateway.OrderSupplyHoldOverrides is { } o && o.ManualPaymentReviewHoldHours is int ov)
        {
            hours = Math.Clamp(ov, 1, 24 * 30);
        }

        return DateTimeOffset.UtcNow.AddHours(hours);
    }

    public async Task<OrderSupplyStatus> GetStatusAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        var group = await LoadGroupAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(404, "سفارش پیدا نشد.", "order.operation.invalid");
        if (group.SellerOrders.All(x => x.Status == SellerOrderStatus.Cancelled))
        {
            return new OrderSupplyStatus(checkoutId, OrderSupplyStatusKind.NotApplicable, []);
        }

        var lines = await BuildLinesAsync(group, cancellationToken);
        return await _inventory.GetOrderSupplyStatusAsync(checkoutId, lines, cancellationToken);
    }

    public async Task<EnsureOrderSupplyResult> EnsureAsync(
        Guid checkoutId,
        OrderSupplyMode mode,
        bool allowReacquire,
        string reason,
        CancellationToken cancellationToken)
    {
        var group = await LoadGroupAsync(checkoutId, cancellationToken)
            ?? throw new PlatformHttpException(404, "سفارش پیدا نشد.", "order.operation.invalid");
        if (group.SellerOrders.All(x => x.Status == SellerOrderStatus.Cancelled))
        {
            return new EnsureOrderSupplyResult(
                OrderSupplyOutcome.NotApplicable,
                OrderSupplyStatusKind.NotApplicable,
                [],
                new Dictionary<Guid, Guid>());
        }

        var lines = await BuildLinesAsync(group, cancellationToken);
        DateTimeOffset? reviewExpires = mode == OrderSupplyMode.EnsureReviewHold
            ? ResolveManualReviewExpiresAt()
            : null;
        var result = await _inventory.EnsureOrderSupplyAsync(
            new EnsureOrderSupplyRequest(
                checkoutId,
                mode,
                allowReacquire,
                reason,
                CorrelationId: $"{mode}:{checkoutId:N}",
                reviewExpires,
                lines),
            cancellationToken);

        if (result.NewBindingsByOrderLineId.Count > 0
            && result.Outcome is OrderSupplyOutcome.AlreadyReserved or OrderSupplyOutcome.Reacquired
            && result.Status is not (OrderSupplyStatusKind.Unavailable or OrderSupplyStatusKind.PartiallyUnavailable))
        {
            foreach (var pair in result.NewBindingsByOrderLineId)
            {
                var orderLine = group.SellerOrders.SelectMany(o => o.Lines).Single(x => x.LineId == pair.Key);
                if (orderLine.ReservationId != pair.Value)
                {
                    orderLine.ReplaceReservation(pair.Value);
                }
            }

            await _orders.SaveChangesAsync(cancellationToken);
            await _fulfillment.RebindActiveReservationsFromOrderAsync(checkoutId, cancellationToken);
        }

        return result;
    }

    private async Task<IReadOnlyList<OrderSupplyLineInput>> BuildLinesAsync(
        CheckoutGroup group,
        CancellationToken cancellationToken)
    {
        var fulfillments = await _fulfillment.ListForCheckoutAsync(group.CheckoutId, cancellationToken);
        var lines = new List<OrderSupplyLineInput>();
        foreach (var order in group.SellerOrders.Where(x => x.Status != SellerOrderStatus.Cancelled))
        {
            var fulfillment = fulfillments.FirstOrDefault(x => x.SellerOrderId == order.SellerOrderId);
            foreach (var line in order.Lines)
            {
                var shipped = fulfillment?.Items.FirstOrDefault(i => i.OrderLineId == line.LineId)?.QuantityShipped ?? 0m;
                var remaining = line.Quantity - shipped;
                lines.Add(new OrderSupplyLineInput(
                    line.LineId,
                    line.OfferId,
                    line.ReservationId,
                    remaining,
                    line.UnitDisplaySnapshot,
                    line.UnitCodeSnapshot));
            }
        }

        return lines;
    }

    private async Task<CheckoutGroup?> LoadGroupAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        await _orders.Checkouts
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
}
