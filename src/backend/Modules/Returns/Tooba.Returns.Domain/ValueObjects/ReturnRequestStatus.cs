

namespace Tooba.Returns.Domain.ValueObjects;


/// <summary>
/// وضعیت درخواست مرجوعی. با وضعیت Order یا Payment یکی نیست.
/// </summary>
public enum ReturnRequestStatus
{
    /// <summary>درخواست ثبت‌شده و در انتظار بررسی.</summary>
    Requested = 0,

    /// <summary>تأیید شده و آمادهٔ refund.</summary>
    Approved = 1,

    /// <summary>رد شده.</summary>
    Rejected = 2,

    /// <summary>refund در حال پردازش.</summary>
    RefundProcessing = 3,

    /// <summary>مرجوعی و refund تکمیل شده.</summary>
    Completed = 4,

    /// <summary>refund شکست خورده.</summary>
    RefundFailed = 5,

    /// <summary>لغو شده توسط مشتری.</summary>
    Cancelled = 6,
}
