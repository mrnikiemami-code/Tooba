namespace Tooba.Inventory.Contracts.Returns;

/// <summary>
/// درز Inventory برای restock پس از مرجوعی. Returns DbContext Inventory را باز نمی‌کند.
/// </summary>
public interface IInventoryReturnGateway
{
    /// <summary>
    /// پس از refund موفق، موجودی مصرف‌شده را با idempotency برمی‌گرداند.
    /// </summary>
    Task RestockFromReturnAsync(
        Guid reservationId,
        decimal quantity,
        string idempotencyKey,
        CancellationToken cancellationToken);
}

/// <summary>TB-265: result of releasing the unconsumed held quantity that backs one closed order-line obligation.</summary>
public sealed record InventoryHeldReleaseResult(
    Guid ReservationId,
    decimal RequestedQuantity,
    decimal ReleasedQuantity,
    string Status);

/// <summary>
/// TB-265: narrow, quantity-safe seam used when an approved courier-origin return closes the corresponding order-line
/// delivery obligation. It releases ONLY the unconsumed Held quantity of that exact reservation: a consumed original
/// sale is never released or resurrected, and a sibling quantity is never touched.
/// </summary>
public interface IInventoryReturnObligationRelease
{
    /// <summary>
    /// Releases up to <paramref name="quantity"/> of the still-Held portion of the reservation, at most once per
    /// idempotency key. Returns the quantity actually released (0 when nothing was held).
    /// </summary>
    Task<InventoryHeldReleaseResult> ReleaseUnconsumedHeldAsync(
        Guid reservationId,
        decimal quantity,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
