using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure;

/// <summary>
/// snapshot گیرندگان اعلان از schema order بدون نشت EF به Notification.
/// </summary>
public sealed class OrderNotificationBridge : IOrderNotificationReader
{
    private readonly OrderDbContext _db;

    /// <summary>پل را به schema order وصل می‌کند.</summary>
    public OrderNotificationBridge(OrderDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<OrderNotificationRecipientSnapshot?> GetByCheckoutIdAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var checkout = await _db.Checkouts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (checkout is null)
        {
            return null;
        }

        var sellers = await _db.SellerOrders.AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId)
            .Select(x => new OrderNotificationSellerSnapshot(x.SellerOrderId, x.SellerPartyId))
            .ToListAsync(cancellationToken);
        return new OrderNotificationRecipientSnapshot(
            checkout.CheckoutId,
            checkout.BuyerPartyId,
            checkout.PlacedByUserId,
            sellers);
    }

    /// <inheritdoc />
    public async Task<OrderNotificationRecipientSnapshot?> GetBySellerOrderIdAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken)
    {
        var sellerOrder = await _db.SellerOrders.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SellerOrderId == sellerOrderId, cancellationToken);
        if (sellerOrder is null)
        {
            return null;
        }

        return await GetByCheckoutIdAsync(sellerOrder.CheckoutId, cancellationToken);
    }
}
