using Tooba.Fulfillment.Application;
using Tooba.Inventory.Application;

namespace Tooba.Fulfillment.Infrastructure;

/// <summary>
/// مصرف رزرو موجودی از طریق قرارداد Inventory.
/// </summary>
public sealed class FulfillmentInventoryGateway : IFulfillmentInventoryGateway
{
    private readonly IInventoryDirectory _inventory;

    /// <summary>
    /// gateway را به قرارداد Inventory وصل می‌کند.
    /// </summary>
    public FulfillmentInventoryGateway(IInventoryDirectory inventory) => _inventory = inventory;

    /// <inheritdoc />
    public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
        _inventory.ConsumeAsync(reservationId, cancellationToken);
}
