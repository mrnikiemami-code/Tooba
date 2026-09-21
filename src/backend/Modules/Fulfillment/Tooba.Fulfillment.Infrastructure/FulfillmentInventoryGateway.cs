using Tooba.Fulfillment.Application;
using Tooba.Inventory.Application.Ports;
using Tooba.Inventory.Application.Checkout;
using Tooba.Inventory.Application.Orders;
using Tooba.Inventory.Application.Returns;

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

    /// <inheritdoc />
    public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
        _inventory.CommitReservationForPaidOrderAsync(reservationId, cancellationToken);
}
