using Tooba.BuildingBlocks;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Application.Ports;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.Events;

namespace Tooba.Inventory.Application.Orders;

/// <summary>آداپتر در-فرآیند چرخهٔ عمر رزرو سفارش؛ بعداً می‌تواند HTTP/gRPC/message شود.</summary>
public sealed class OrderInventoryLifecycleAdapter : IOrderInventoryLifecyclePort
{
    private readonly IInventoryDirectory _inventory;

    /// <summary>آداپتر را به directory موجودی وصل می‌کند.</summary>
    public OrderInventoryLifecycleAdapter(IInventoryDirectory inventory) => _inventory = inventory;

    /// <inheritdoc />
    public Task ReleaseHeldReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        => _inventory.ReleaseAsync(reservationId, cancellationToken);

    /// <inheritdoc />
    public Task TryReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        => _inventory.ReleaseAsync(reservationId, cancellationToken);

    /// <inheritdoc />
    public async Task<Guid> ReacquireDurableHoldFromPreviousAsync(
        Guid previousReservationId,
        string externalReference,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var previous = await _inventory.FindReservationAsync(previousReservationId, cancellationToken)
                ?? throw new ContractOperationException("order.restore.inventory_failed");
            var receipt = await _inventory.ReserveAsync(
                previous.StockItemId,
                previous.Quantity,
                externalReference,
                idempotencyKey,
                expiresAt: null,
                cancellationToken);
            return receipt.ReservationId;
        }
        catch (ContractOperationException ex) when (
            ex.Code.StartsWith("inventory.", StringComparison.Ordinal))
        {
            throw new ContractOperationException("order.restore.inventory_failed", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Guid> PromoteOrReacquireForManualPaymentReviewAsync(
        Guid reservationId,
        DateTimeOffset reviewExpiresAt,
        string externalReference,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var existing = await _inventory.FindReservationAsync(reservationId, cancellationToken);
        if (existing is { Status: StockReservationStatus.Held })
        {
            await _inventory.PromoteReservationForManualPaymentReviewAsync(
                reservationId,
                reviewExpiresAt,
                cancellationToken);
            return reservationId;
        }

        if (existing is null)
        {
            throw new ContractOperationException("inventory.reservation.not_found");
        }

        try
        {
            var receipt = await _inventory.ReserveAsync(
                existing.StockItemId,
                existing.Quantity,
                externalReference,
                idempotencyKey,
                reviewExpiresAt,
                cancellationToken);
            return receipt.ReservationId;
        }
        catch (ContractOperationException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw new ContractOperationException("inventory.manual_review.unavailable");
        }
    }

    /// <inheritdoc />
    public async Task ReleaseIfHeldAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        var existing = await _inventory.FindReservationAsync(reservationId, cancellationToken);
        if (existing is not { Status: StockReservationStatus.Held })
        {
            return;
        }

        await _inventory.ReleaseAsync(reservationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OrderInventoryPaidSupplyResult> EnsurePaidDurableSupplyAsync(
        OrderInventoryPaidSupplyRequest request,
        CancellationToken cancellationToken)
    {
        var lines = request.Lines
            .Select(line => new OrderSupplyLineInput(
                line.OrderLineId,
                line.OfferId,
                line.CurrentReservationId,
                line.RemainingQuantity,
                line.ItemTitle,
                line.UnitCode))
            .ToArray();
        var result = await _inventory.EnsureOrderSupplyAsync(
            new EnsureOrderSupplyRequest(
                request.CheckoutId,
                OrderSupplyMode.EnsurePaidDurable,
                AllowReacquire: true,
                Reason: request.Reason,
                CorrelationId: request.CorrelationId,
                ReviewExpiresAt: null,
                lines),
            cancellationToken);
        return new OrderInventoryPaidSupplyResult(result.NewBindingsByOrderLineId);
    }

    /// <inheritdoc />
    public async Task<OrderInventorySupplyResult> EnsureUnpaidRetryHoldAsync(
        OrderInventoryUnpaidRetryRequest request,
        CancellationToken cancellationToken)
    {
        var lines = request.Lines.Select(Map).ToArray();
        var result = await _inventory.EnsureOrderSupplyAsync(
            new EnsureOrderSupplyRequest(
                request.CheckoutId,
                OrderSupplyMode.EnsureUnpaidRetryHold,
                request.AllowReacquire,
                request.Reason,
                null,
                null,
                lines),
            cancellationToken);
        return new(result.Status.ToString(), result.Outcome.ToString(), result.NewBindingsByOrderLineId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, OrderInventorySupplyStatusSnapshot>> GetSupplyStatusesAsync(
        IReadOnlyDictionary<Guid, IReadOnlyList<OrderInventorySupplyLine>> linesByCheckoutId,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, OrderInventorySupplyStatusSnapshot>();
        foreach (var pair in linesByCheckoutId)
        {
            var status = await _inventory.GetOrderSupplyStatusAsync(
                pair.Key, pair.Value.Select(Map).ToArray(), cancellationToken);
            result[pair.Key] = new(pair.Key, status.Status.ToString());
        }
        return result;
    }

    private static OrderSupplyLineInput Map(OrderInventorySupplyLine line) =>
        new(line.OrderLineId, line.OfferId, line.CurrentReservationId, line.RemainingQuantity, line.ItemTitle, line.UnitCode);
}
