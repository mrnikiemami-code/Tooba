namespace Tooba.Order.Domain.Enums;

/// <summary>
/// وضعیت سفارش یک فروشنده. پرداخت و ارسال حقیقت جدا هستند.
/// </summary>
public enum SellerOrderStatus
{
    /// <summary>
    /// ثبت شده.
    /// </summary>
    Submitted = 0,

    /// <summary>
    /// خرید آنلاین در انتظار پرداخت آینده؛ Paid نیست.
    /// </summary>
    PendingPayment = 1,

    /// <summary>
    /// درخواست رزرو ثبت شده؛ پذیرش فروشنده آینده است.
    /// </summary>
    ReservationRequested = 2,

    /// <summary>
    /// لغو شده.
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// پرداخت تأییدشده از ماژول Payment؛ شروع درگاه این وضعیت را نمی‌سازد.
    /// </summary>
    Paid = 4,
}
