namespace Tooba.Notification.Domain.ValueObjects;

/// <summary>
/// نوع گیرندهٔ اعلان تراکنشی در Domain. با نقش Identity یکی نیست.
/// مقادیر عددی پایدار: Customer=1, Seller=2.
/// </summary>
public enum NotificationRecipientKind
{
    /// <summary>خریدار / مشتری.</summary>
    Customer = 1,

    /// <summary>فروشندهٔ مالک سفارش یا رویداد.</summary>
    Seller = 2,
}
