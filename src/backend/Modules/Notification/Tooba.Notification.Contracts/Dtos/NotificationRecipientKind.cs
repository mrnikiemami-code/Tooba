namespace Tooba.Notification.Contracts.Dtos;

/// <summary>
/// نوع گیرندهٔ اعلان تراکنشی. با نقش Identity یکی نیست.
/// </summary>
public enum NotificationRecipientKind
{
    /// <summary>خریدار / مشتری.</summary>
    Customer = 1,

    /// <summary>فروشندهٔ مالک سفارش یا رویداد.</summary>
    Seller = 2,
}
