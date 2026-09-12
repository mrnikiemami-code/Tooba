#pragma warning disable CS1591
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Infrastructure.Persistence;
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
    private readonly FulfillmentDbContext _fulfillmentDb;
    private readonly PaymentGatewayOptions _paymentGateway;

    public OrderSupplyComposer(
        OrderDbContext orders,
        IInventoryDirectory inventory,
        IFulfillmentDirectory fulfillment,
        FulfillmentDbContext fulfillmentDb,
        IOptions<PaymentGatewayOptions> paymentGateway)
    {
        _orders = orders;
        _inventory = inventory;
        _fulfillment = fulfillment;
        _fulfillmentDb = fulfillmentDb;
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

    /// <summary>وضعیت تأمین چند checkout در یک بارگذاری Order/Fulfillment — بدون N+1 HTTP.</summary>
    public async Task<IReadOnlyDictionary<Guid, OrderSupplyStatus>> GetStatusesAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, OrderSupplyStatus>();
        if (checkoutIds.Count == 0)
        {
            return result;
        }

        var ids = checkoutIds.Distinct().ToList();
        var groups = await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .Where(x => ids.Contains(x.CheckoutId))
            .ToListAsync(cancellationToken);
        var shipped = await LoadShippedByLineAsync(ids, cancellationToken);
        foreach (var group in groups)
        {
            if (group.SellerOrders.All(x => x.Status == SellerOrderStatus.Cancelled))
            {
                result[group.CheckoutId] = new OrderSupplyStatus(group.CheckoutId, OrderSupplyStatusKind.NotApplicable, []);
                continue;
            }

            var lines = BuildLines(group, shipped);
            result[group.CheckoutId] = await _inventory.GetOrderSupplyStatusAsync(
                group.CheckoutId,
                lines,
                cancellationToken);
        }

        foreach (var id in ids)
        {
            if (!result.ContainsKey(id))
            {
                result[id] = new OrderSupplyStatus(id, OrderSupplyStatusKind.NotApplicable, []);
            }
        }

        return result;
    }

    public static string MessageFa(OrderSupplyStatusKind status) =>
        status switch
        {
            OrderSupplyStatusKind.Reserved => "موجودی موردنیاز این سفارش رزرو شده است.",
            OrderSupplyStatusKind.AvailableForReacquire => "رزرو قبلی فعال نیست، اما موجودی لازم در حال حاضر قابل تأمین است.",
            OrderSupplyStatusKind.Unavailable or OrderSupplyStatusKind.PartiallyUnavailable =>
                "یک یا چند قلم این سفارش در حال حاضر قابل تأمین نیست.",
            OrderSupplyStatusKind.Fulfilled => "موجودی موردنیاز این سفارش تأمین و تکمیل شده است.",
            _ => "تأمین موجودی برای این سفارش موضوعیت ندارد.",
        };

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
        var shipped = await LoadShippedByLineAsync([group.CheckoutId], cancellationToken);
        return BuildLines(group, shipped);
    }

    private static IReadOnlyList<OrderSupplyLineInput> BuildLines(
        CheckoutGroup group,
        IReadOnlyDictionary<Guid, decimal> shippedByLine)
    {
        var lines = new List<OrderSupplyLineInput>();
        foreach (var order in group.SellerOrders.Where(x => x.Status != SellerOrderStatus.Cancelled))
        {
            foreach (var line in order.Lines)
            {
                shippedByLine.TryGetValue(line.LineId, out var shipped);
                lines.Add(new OrderSupplyLineInput(
                    line.LineId,
                    line.OfferId,
                    line.ReservationId,
                    line.Quantity - shipped,
                    line.UnitDisplaySnapshot,
                    line.UnitCodeSnapshot));
            }
        }

        return lines;
    }

    private async Task<Dictionary<Guid, decimal>> LoadShippedByLineAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken)
    {
        var fulfillmentIds = await _fulfillmentDb.Fulfillments.AsNoTracking()
            .Where(x => checkoutIds.Contains(x.CheckoutId))
            .Select(x => x.FulfillmentId)
            .ToListAsync(cancellationToken);
        if (fulfillmentIds.Count == 0)
        {
            return [];
        }

        var rows = await _fulfillmentDb.Items.AsNoTracking()
            .Where(x => fulfillmentIds.Contains(x.FulfillmentId))
            .Select(x => new { x.OrderLineId, x.QuantityShipped })
            .ToListAsync(cancellationToken);
        return rows
            .GroupBy(x => x.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityShipped));
    }

    private async Task<CheckoutGroup?> LoadGroupAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        await _orders.Checkouts
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
}
