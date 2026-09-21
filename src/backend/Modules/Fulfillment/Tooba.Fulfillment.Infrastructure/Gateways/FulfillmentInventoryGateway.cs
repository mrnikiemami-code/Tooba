using Tooba.Fulfillment.Application.Ports;
using Tooba.Inventory.Contracts.Fulfillment;

namespace Tooba.Fulfillment.Infrastructure.Gateways;

/// <summary>
/// مصرف رزرو موجودی از طریق قرارداد Inventory.
/// </summary>
public sealed class FulfillmentInventoryGateway : IFulfillmentInventoryGateway
{
    private readonly IFulfillmentInventoryLifecyclePort _inventory;

    /// <summary>
    /// gateway را به قرارداد Inventory وصل می‌کند.
    /// </summary>
    public FulfillmentInventoryGateway(IFulfillmentInventoryLifecyclePort inventory) => _inventory = inventory;

    /// <inheritdoc />
    public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
        _inventory.ConsumeAsync(reservationId, cancellationToken);

    /// <inheritdoc />
    public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
        _inventory.CommitReservationForPaidOrderAsync(reservationId, cancellationToken);
}
