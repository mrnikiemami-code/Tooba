namespace Tooba.Story.Domain.Enums;

/// <summary>وضعیت بازبینی استوری‌های فروشنده.</summary>
public enum StoryReviewStatus
{
    /// <summary>بدون بازبینی فعال (ادمین یا پیش‌نویس فروشنده).</summary>
    None = 0,
    /// <summary>ارسال‌شده برای بازبینی ادمین.</summary>
    Submitted = 1,
    /// <summary>تأییدشده؛ آمادهٔ زمان‌بندی/فعال‌سازی.</summary>
    Approved = 2,
    /// <summary>ردشده با دلیل.</summary>
    Rejected = 3,
}
