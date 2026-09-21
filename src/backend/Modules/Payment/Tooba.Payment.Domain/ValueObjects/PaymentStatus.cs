namespace Tooba.Payment.Domain.ValueObjects;

/// <summary>
/// وضعیت پرداخت. با وضعیت سفارش یکی نیست؛ شروع درگاه به‌معنای Succeeded نیست.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// رکورد ساخته شده و هنوز به درگاه نرفته.
    /// </summary>
    Created = 0,

    /// <summary>
    /// شروع درگاه انجام شده؛ تا تأیید مستقل Succeeded نیست.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// فقط پس از Verify موفق درگاه.
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// تأیید یا تلاش شکست خورد.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// لغو شده؛ سفارش را خودکار Paid نمی‌کند.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// مهلت تلاش تمام شده.
    /// </summary>
    Expired = 5,

    /// <summary>
    /// بازگشت وجه شروع شده؛ هنوز موفق فرض نمی‌شود.
    /// </summary>
    RefundPending = 6,

    /// <summary>
    /// بازگشت وجه نزد درگاه تکمیل شده.
    /// </summary>
    Refunded = 7,

    /// <summary>
    /// بازگشت وجه شکست خورده؛ اقدام ادمین لازم است.
    /// </summary>
    RefundFailed = 8,
}
