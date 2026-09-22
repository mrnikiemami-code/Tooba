using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Order.Application.Admin.Supply.Models;
using Tooba.Order.Application.Admin.Supply.Ports;
using Tooba.Order.Domain;
using Tooba.Payment.Contracts.Hold;

namespace Tooba.Order.Application.Admin.Supply.Services;

/// <summary>
/// Facade تأمین سفارش: ساخت خط‌ها از Order/Fulfillment و فراخوانی Inventory Ensure.
/// </summary>
public sealed class OrderSupplyService
{
    private readonly IOrderSupplyCheckoutStore _orders;
    private readonly IOrderInventoryLifecyclePort _inventory;
    private readonly IFulfillmentAdminOperations _fulfillment;
    private readonly IClock _clock;
    private readonly ICommerceHoldPolicySource? _holdPolicy;
    private readonly IReservationCyclePolicyResolver? _cyclePolicy;

    /// <summary>سرویس تأمین را به درزهای Order/Inventory/Fulfillment وصل می‌کند.</summary>
    public OrderSupplyService(
        IOrderSupplyCheckoutStore orders,
        IOrderInventoryLifecyclePort inventory,
        IFulfillmentAdminOperations fulfillment,
        IClock clock,
        ICommerceHoldPolicySource? holdPolicy = null,
        IReservationCyclePolicyResolver? cyclePolicy = null)
    {
        _orders = orders;
        _inventory = inventory;
        _fulfillment = fulfillment;
        _clock = clock;
        _holdPolicy = holdPolicy;
        _cyclePolicy = cyclePolicy;
    }

    /// <summary>مهلت بررسی دستی پرداخت.</summary>
    public DateTimeOffset ResolveManualReviewExpiresAt()
    {
        if (_holdPolicy is not null)
        {
            return _holdPolicy.ResolveManualReviewExpiresAt(_clock.UtcNow);
        }

        return _clock.UtcNow.AddHours(48);
    }

    /// <summary>مهلت retry unpaid.</summary>
    public DateTimeOffset ResolveUnpaidRetryExpiresAt()
    {
        if (_holdPolicy is not null)
        {
            return _holdPolicy.ResolveInitialExpiresAt(_clock.UtcNow);
        }

        return ResolveManualReviewExpiresAt();
    }

    private async Task<DateTimeOffset> ResolveRetryExpiresAtAsync(
        OrderSupplyCheckoutSnapshot group,
        CancellationToken cancellationToken)
    {
        if (_cyclePolicy is not null)
        {
            var policy = await _cyclePolicy.ResolveAsync(
                group.SellerOrders
                    .Where(x => x.Status != SellerOrderStatus.Cancelled)
                    .SelectMany(x => x.Lines)
                    .Select(x => new ReservationCyclePolicyLine(x.OfferId, x.CategoryIdSnapshot))
                    .ToArray(),
                cancellationToken);
            return _clock.UtcNow.AddMinutes(policy.RetryHoldMinutes);
        }

        return ResolveUnpaidRetryExpiresAt();
    }

    /// <summary>وضعیت تأمین یک checkout.</summary>
    public async Task<Result<OrderSupplyStatus>> GetStatusAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        var group = await _orders.GetAsync(checkoutId, cancellationToken);
        if (group is null)
        {
            return Result.Failure<OrderSupplyStatus>(new SemanticError("order.operation.invalid"));
        }

        if (group.SellerOrders.All(x => x.Status == SellerOrderStatus.Cancelled))
        {
            return Result.Success(new OrderSupplyStatus(checkoutId, OrderSupplyStatusKind.NotApplicable, []));
        }

        var lines = await BuildLinesAsync(group, cancellationToken);
        var detail = await _inventory.GetSupplyStatusDetailAsync(checkoutId, lines, cancellationToken);
        return Result.Success(MapStatus(detail));
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
        var groups = await _orders.GetManyAsync(ids, cancellationToken);
        var shipped = await _fulfillment.GetShippedByOrderLineIdsForCheckoutsAsync(ids, cancellationToken);
        foreach (var group in groups)
        {
            if (group.SellerOrders.All(x => x.Status == SellerOrderStatus.Cancelled))
            {
                result[group.CheckoutId] = new OrderSupplyStatus(group.CheckoutId, OrderSupplyStatusKind.NotApplicable, []);
                continue;
            }

            var lines = BuildLines(group, shipped);
            var detail = await _inventory.GetSupplyStatusDetailAsync(group.CheckoutId, lines, cancellationToken);
            result[group.CheckoutId] = MapStatus(detail);
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

    /// <summary>Ensure تأمین با mode مشخص.</summary>
    public async Task<Result<EnsureOrderSupplyResult>> EnsureAsync(
        Guid checkoutId,
        OrderSupplyMode mode,
        bool allowReacquire,
        string reason,
        CancellationToken cancellationToken)
    {
        var group = await _orders.GetAsync(checkoutId, cancellationToken);
        if (group is null)
        {
            return Result.Failure<EnsureOrderSupplyResult>(new SemanticError("order.operation.invalid"));
        }

        if (group.SellerOrders.All(x => x.Status == SellerOrderStatus.Cancelled))
        {
            return Result.Success(new EnsureOrderSupplyResult(
                OrderSupplyOutcome.NotApplicable,
                OrderSupplyStatusKind.NotApplicable,
                [],
                new Dictionary<Guid, Guid>()));
        }

        var lines = await BuildLinesAsync(group, cancellationToken);
        DateTimeOffset? reviewExpires = mode switch
        {
            OrderSupplyMode.EnsureReviewHold => ResolveManualReviewExpiresAt(),
            OrderSupplyMode.EnsureUnpaidRetryHold when allowReacquire =>
                await ResolveRetryExpiresAtAsync(group, cancellationToken),
            _ => null,
        };
        var detail = await _inventory.EnsureSupplyDetailAsync(
            new OrderInventoryEnsureDetailRequest(
                checkoutId,
                mode.ToString(),
                allowReacquire,
                reason,
                CorrelationId: $"{mode}:{checkoutId:N}",
                reviewExpires,
                lines),
            cancellationToken);
        var result = MapEnsure(detail);

        if (result.NewBindingsByOrderLineId.Count > 0
            && result.Outcome is OrderSupplyOutcome.AlreadyReserved or OrderSupplyOutcome.Reacquired
            && result.Status is not (OrderSupplyStatusKind.Unavailable or OrderSupplyStatusKind.PartiallyUnavailable))
        {
            await _orders.ReplaceReservationsAsync(checkoutId, result.NewBindingsByOrderLineId, cancellationToken);
            await _fulfillment.RebindActiveReservationsFromOrderAsync(checkoutId, cancellationToken);
        }

        return Result.Success(result);
    }

    private async Task<IReadOnlyList<OrderInventorySupplyLine>> BuildLinesAsync(
        OrderSupplyCheckoutSnapshot group,
        CancellationToken cancellationToken)
    {
        var shipped = await _fulfillment.GetShippedByOrderLineIdsForCheckoutsAsync(
            [group.CheckoutId], cancellationToken);
        return BuildLines(group, shipped);
    }

    private static IReadOnlyList<OrderInventorySupplyLine> BuildLines(
        OrderSupplyCheckoutSnapshot group,
        IReadOnlyDictionary<Guid, decimal> shippedByLine)
    {
        var lines = new List<OrderInventorySupplyLine>();
        foreach (var order in group.SellerOrders.Where(x => x.Status != SellerOrderStatus.Cancelled))
        {
            foreach (var line in order.Lines)
            {
                shippedByLine.TryGetValue(line.LineId, out var shipped);
                lines.Add(new OrderInventorySupplyLine(
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

    private static OrderSupplyStatus MapStatus(OrderInventorySupplyStatusDetail detail) =>
        new(
            detail.CheckoutId,
            ParseStatus(detail.Status),
            detail.Lines.Select(MapLine).ToArray());

    private static EnsureOrderSupplyResult MapEnsure(OrderInventoryEnsureDetailResult detail) =>
        new(
            ParseOutcome(detail.Outcome),
            ParseStatus(detail.Status),
            detail.Lines.Select(MapLine).ToArray(),
            detail.NewBindingsByOrderLineId);

    private static OrderSupplyLineShortage MapLine(OrderInventorySupplyLineDetail line) =>
        new(
            line.OrderLineId,
            line.ItemTitle,
            line.UnitCode,
            line.Required,
            line.Available,
            line.Shortage,
            ParseStatus(line.LineStatus),
            line.BoundReservationId);

    private static OrderSupplyStatusKind ParseStatus(string value) =>
        Enum.TryParse<OrderSupplyStatusKind>(value, true, out var kind) ? kind : OrderSupplyStatusKind.NotApplicable;

    private static OrderSupplyOutcome ParseOutcome(string value) =>
        Enum.TryParse<OrderSupplyOutcome>(value, true, out var outcome) ? outcome : OrderSupplyOutcome.NotApplicable;
}
