namespace Tooba.Order.Contracts.Notifications;

/// <summary>
/// snapshot گیرندگان اعلان از checkout بدون افشای DbContext.
/// </summary>
public sealed record OrderNotificationRecipientSnapshot(
    Guid CheckoutId,
    Guid? BuyerPartyId,
    Guid PlacedByUserId,
    IReadOnlyList<OrderNotificationSellerSnapshot> Sellers);

/// <summary>
/// فروشندهٔ یک سفارش برای اعلان.
/// </summary>
public sealed record OrderNotificationSellerSnapshot(Guid SellerOrderId, Guid SellerPartyId);

/// <summary>
/// خواندن گیرندگان اعلان از Order بدون cross-DbContext.
/// </summary>
public interface IOrderNotificationReader
{
    /// <summary>گیرندگان را از CheckoutId می‌خواند.</summary>
    Task<OrderNotificationRecipientSnapshot?> GetByCheckoutIdAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>گیرندگان را از SellerOrderId می‌خواند.</summary>
    Task<OrderNotificationRecipientSnapshot?> GetBySellerOrderIdAsync(Guid sellerOrderId, CancellationToken cancellationToken);
}
