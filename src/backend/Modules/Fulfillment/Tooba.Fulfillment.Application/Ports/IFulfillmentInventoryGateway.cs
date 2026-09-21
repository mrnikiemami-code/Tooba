namespace Tooba.Fulfillment.Application.Ports;


/// <summary>
/// درز موجودی برای dispatch؛ Fulfillment مستقیم Inventory DbContext باز نمی‌کند.
/// </summary>
public interface IFulfillmentInventoryGateway
{
    /// <summary>
    /// رزرو را پس از dispatch کامل خط مصرف می‌کند.
    /// </summary>
    Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken);

    /// <summary>
    /// رزرو Held سفارش پرداخت‌شده را از TTL سبد خارج می‌کند (ExpiresAt=null). Idempotent.
    /// </summary>
    Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken);
}
