namespace Tooba.Payment.Domain.ValueObjects;

/// <summary>
/// وضعیت یک تلاش درگاه. تاریخچهٔ تلاش بازنویسی نمی‌شود.
/// </summary>
public enum PaymentAttemptStatus
{
    /// <summary>
    /// شروع شده.
    /// </summary>
    Initiated = 0,

    /// <summary>
    /// درگاه Verify را تأیید کرد.
    /// </summary>
    VerifiedSucceeded = 1,

    /// <summary>
    /// درگاه Verify را رد کرد یا شکست اعلام کرد.
    /// </summary>
    VerifiedFailed = 2,

    /// <summary>
    /// تلاش لغو شد.
    /// </summary>
    Cancelled = 3,
}
