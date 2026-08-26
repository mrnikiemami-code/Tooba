namespace Tooba.Inventory.Application;

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
        int quantity,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
