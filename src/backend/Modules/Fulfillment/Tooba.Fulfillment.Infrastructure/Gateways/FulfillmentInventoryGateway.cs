using Tooba.Fulfillment.Application.Fulfillments.Ports;
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

    /// <summary>
    /// مصرف کلیددار رزرو: هویت پایدار مصرف‌کننده را به overload کلیددار Inventory می‌رساند تا
    /// replay پس از شکست commit به‌صورت اثبات‌شده و بدون کسر دوبارهٔ موجودی پذیرفته شود.
    /// </summary>
    /// <param name="reservationId">شناسهٔ رزرو Held.</param>
    /// <param name="consumerKey">هویت پایدار و غیرتهی مصرف‌کننده.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    public Task ConsumeReservationAsync(Guid reservationId, string consumerKey, CancellationToken cancellationToken) =>
        _inventory.ConsumeAsync(reservationId, consumerKey, cancellationToken);

    /// <inheritdoc />
    public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
        _inventory.CommitReservationForPaidOrderAsync(reservationId, cancellationToken);
}
