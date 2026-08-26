using Tooba.Inventory.Application;
using Tooba.Returns.Application;

namespace Tooba.Returns.Infrastructure;

/// <summary>
/// restock از طریق قرارداد Inventory؛ بدون DbContext مشترک.
/// </summary>
public sealed class ReturnInventoryGateway : IReturnInventoryGateway
{
    private readonly IInventoryReturnGateway _inventory;

    /// <summary>
    /// gateway را به قرارداد Inventory وصل می‌کند.
    /// </summary>
    public ReturnInventoryGateway(IInventoryReturnGateway inventory) => _inventory = inventory;

    /// <inheritdoc />
    public Task RestockConsumedReservationAsync(
        Guid reservationId,
        int quantity,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        _inventory.RestockFromReturnAsync(reservationId, quantity, idempotencyKey, cancellationToken);
}
