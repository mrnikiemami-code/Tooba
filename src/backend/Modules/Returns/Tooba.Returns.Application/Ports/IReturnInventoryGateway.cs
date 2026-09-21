using Tooba.Returns.Application.Models;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Ports;


/// <summary>
/// درز موجودی برای restock؛ Returns مستقیم Inventory DbContext باز نمی‌کند.
/// </summary>
public interface IReturnInventoryGateway
{
    /// <summary>
    /// رزرو مصرف‌شده را پس از refund موفق restock می‌کند. پیاده‌سازی فعلی می‌تواند no-op log باشد.
    /// </summary>
    Task RestockConsumedReservationAsync(
        Guid reservationId,
        decimal quantity,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
