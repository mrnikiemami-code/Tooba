using Microsoft.EntityFrameworkCore;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Integrations.Payment;

/// <summary>
/// خواندن فقط‌خواندنی مجموعهٔ رزرو خطوط یک checkout. مالکیت اینجا سنجیده نمی‌شود و هیچ چیزی نوشته
/// نمی‌شود؛ فقط خطوط پایدار سفارش با ترتیب قطعی برگردانده می‌شوند تا ارزیاب قابل‌پرداخت‌بودن بتواند
/// غیبت/تکرار شناسهٔ رزرو را خودش تشخیص دهد.
/// </summary>
public sealed class OrderCheckoutReservationSetReader : IOrderCheckoutReservationSetReader
{
    private readonly OrderDbContext _db;

    /// <summary>خواننده را به schema سفارش وصل می‌کند.</summary>
    public OrderCheckoutReservationSetReader(OrderDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<OrderCheckoutReservationSet?> GetReservationSetAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var group = await _db.Checkouts
            .AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken)
            .ConfigureAwait(false);
        if (group is null)
        {
            return null;
        }

        var reservationIds = group.SellerOrders
            .OrderBy(x => x.SellerOrderId)
            .SelectMany(order => order.Lines.OrderBy(line => line.LineId))
            .Select(line => line.ReservationId)
            .ToArray();
        return new OrderCheckoutReservationSet(group.CheckoutId, reservationIds);
    }
}
