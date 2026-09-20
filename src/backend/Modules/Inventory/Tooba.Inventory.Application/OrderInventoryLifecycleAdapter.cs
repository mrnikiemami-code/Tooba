using Tooba.Inventory.Contracts;
using Tooba.Inventory.Domain;

namespace Tooba.Inventory.Application;

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
    public async Task TryReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        try
        {
            await _inventory.ReleaseAsync(reservationId, cancellationToken);
        }
        catch (InvalidOperationException)
        {
        }
    }

    /// <inheritdoc />
    public async Task<Guid> ReacquireDurableHoldFromPreviousAsync(
        Guid previousReservationId,
        string externalReference,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var previous = await _inventory.FindReservationAsync(previousReservationId, cancellationToken)
            ?? throw new InvalidOperationException("order.restore.inventory_failed");
        var receipt = await _inventory.ReserveAsync(
            previous.StockItemId,
            previous.Quantity,
            externalReference,
            idempotencyKey,
            expiresAt: null,
            cancellationToken);
        return receipt.ReservationId;
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
            throw new InvalidOperationException("inventory.reservation.not_found");
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
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException("inventory.manual_review.unavailable");
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
}
