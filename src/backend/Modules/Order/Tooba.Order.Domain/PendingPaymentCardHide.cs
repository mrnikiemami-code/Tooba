namespace Tooba.Order.Domain;

/// <summary>
/// ترجیح نمایش کارت در انتظار پرداخت. وضعیت سفارش/پرداخت/رزرو را عوض نمی‌کند.
/// </summary>
public sealed class PendingPaymentCardHide
{
    /// <summary>شناسه ردیف.</summary>
    public Guid HideId { get; private set; }

    /// <summary>سفارش متعهد.</summary>
    public Guid CheckoutId { get; private set; }

    /// <summary>مشتری احرازشده؛ برای مهمان خالی است.</summary>
    public Guid? OwnerUserId { get; private set; }

    /// <summary>سبد متعهد مهمان؛ برای مشتری احرازشده خالی است.</summary>
    public Guid? GuestCartId { get; private set; }

    /// <summary>زمان ثبت ترجیح.</summary>
    public DateTimeOffset HiddenAt { get; private set; }

    /// <summary>ردیف ترجیح را می‌سازد.</summary>
    public static PendingPaymentCardHide Create(
        Guid checkoutId,
        Guid? ownerUserId,
        Guid? guestCartId,
        DateTimeOffset hiddenAt) =>
        new()
        {
            HideId = Guid.NewGuid(),
            CheckoutId = checkoutId,
            OwnerUserId = ownerUserId,
            GuestCartId = guestCartId,
            HiddenAt = hiddenAt,
        };
}
