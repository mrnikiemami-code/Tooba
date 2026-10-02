namespace Tooba.Identity.Application.Models;

/// <summary>
/// نتیجهٔ Verify برای ارتقای قالب هش.
/// </summary>
public enum PasswordVerificationOutcome
{
    /// <summary>
    /// شکست.
    /// </summary>
    Failed = 0,

    /// <summary>
    /// موفقیت بدون نیاز به بازنویسی.
    /// </summary>
    Success = 1,

    /// <summary>
    /// موفقیت با نیاز به هش مجدد با قالب جدیدتر.
    /// </summary>
    SuccessRehashNeeded = 2,
}
