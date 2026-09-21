namespace Tooba.Inventory.Contracts.Fulfillment;

/// <summary>
/// درز پایدار Inventory برای مصرف/commit رزرو پس از پرداخت — بدون افشای Directory داخلی.
/// </summary>
public interface IFulfillmentInventoryLifecyclePort
{
    /// <summary>
    /// رزرو را پس از dispatch مصرف می‌کند.
    /// </summary>
    Task ConsumeAsync(Guid reservationId, CancellationToken cancellationToken);

    /// <summary>
    /// رزرو را برای سفارش پرداخت‌شده commit می‌کند.
    /// </summary>
    Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken);
}
