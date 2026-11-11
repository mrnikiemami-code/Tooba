using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Inventory.Contracts.Fulfillment;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Replacement;

namespace Tooba.Fulfillment.Infrastructure.Gateways;

/// <summary>
/// مصرف رزرو موجودی از طریق قرارداد Inventory.
/// </summary>
public sealed class FulfillmentInventoryGateway : IFulfillmentInventoryGateway
{
    private readonly IFulfillmentInventoryLifecyclePort _inventory;
    private readonly IOrderInventoryLifecyclePort _orderInventory;
    private readonly IDamagedReplacementInventoryPort _replacementInventory;

    /// <summary>
    /// gateway را به قرارداد Inventory وصل می‌کند.
    /// </summary>
    public FulfillmentInventoryGateway(
        IFulfillmentInventoryLifecyclePort inventory,
        IOrderInventoryLifecyclePort orderInventory,
        IDamagedReplacementInventoryPort replacementInventory)
    {
        _inventory = inventory;
        _orderInventory = orderInventory;
        _replacementInventory = replacementInventory;
    }

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

    /// <inheritdoc />
    public Task ConsumeReplacementReservationAsync(
        Guid reservationId,
        string consumerKey,
        CancellationToken cancellationToken) =>
        _inventory.ConsumeAsync(reservationId, consumerKey, cancellationToken);

    /// <inheritdoc />
    public Task ReleaseReplacementReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
        // ReleaseIfHeldAsync is deliberately idempotent: a consumed reservation is never released and a released
        // one is never released twice, so a retried cancellation cannot disturb stock.
        _orderInventory.ReleaseIfHeldAsync(reservationId, cancellationToken);

    /// <inheritdoc />
    public async Task<FulfillmentReplacementReservation> ReserveReplacementReservationAsync(
        Guid originalReservationId,
        decimal quantity,
        string reservationKey,
        string externalReference,
        CancellationToken cancellationToken)
    {
        var reserved = await _replacementInventory.ReserveForDamagedReplacementAsync(
            new DamagedReplacementReservationRequest(
                originalReservationId,
                quantity,
                reservationKey,
                externalReference),
            cancellationToken);
        return new FulfillmentReplacementReservation(
            reserved.ReservationId,
            reserved.StockItemId,
            reserved.OfferId,
            reserved.Quantity,
            reserved.Status);
    }

    /// <inheritdoc />
    public async Task<FulfillmentReplacementReservation?> FindReplacementReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        var found = await _replacementInventory.FindDamagedReplacementReservationAsync(
            reservationId,
            cancellationToken);
        return found is null
            ? null
            : new FulfillmentReplacementReservation(
                found.ReservationId,
                found.StockItemId,
                found.OfferId,
                found.Quantity,
                found.Status);
    }
}
