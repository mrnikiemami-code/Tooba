namespace Tooba.Order.Domain;

/// <summary>
/// پیش‌بینی کانونی سفارش بازِ پرداخت‌نشده. شمارش از کارت پنهان یا تلاش پرداخت ساخته نمی‌شود.
/// </summary>
public static class OpenUnpaidOrderPredicate
{
    /// <summary>وضعیت‌های سفارش که در شمارش بازِ پرداخت‌نشده می‌آیند.</summary>
    public static readonly SellerOrderStatus[] OpenUnpaidStatuses =
    [
        SellerOrderStatus.PendingPayment,
        SellerOrderStatus.Submitted,
    ];

    /// <summary>
    /// سفارش هنوز از نظر تجاری باز و پرداخت‌نشده است؛ Paid/Cancelled و وضعیت پایانی غیرقابل‌پرداخت را حساب نمی‌کند.
    /// </summary>
    public static bool IsOpenUnpaid(SellerOrderStatus status) =>
        OpenUnpaidStatuses.Contains(status);

    /// <summary>
    /// وضعیت‌هایی که دیگر ظرفیت سفارش باز را اشغال نمی‌کنند.
    /// </summary>
    public static bool IsClosedForOpenUnpaid(SellerOrderStatus status) => !IsOpenUnpaid(status);
}
