using Tooba.Identity.Contracts;

namespace Tooba.Identity.Domain.Aggregates;

/// <summary>
/// چالش یک‌بارمصرف هویت (ورود OTP، تأیید شناسه، بازنشانی رمز، MFA). راز plaintext ذخیره نمی‌شود.
/// </summary>
public sealed class AuthChallenge
{
    /// <summary>
    /// شناسهٔ پایدار چالش.
    /// </summary>
    public Guid ChallengeId { get; init; }

    /// <summary>
    /// User در صورت شناخته بودن. برای enumeration عمومی ممکن است تهی نماند فقط وقتی حساب پیدا شده.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// هش شناسه/مقصد؛ مقدار خام ایمیل/تلفن persist نمی‌شود.
    /// </summary>
    public string IdentifierHash { get; init; } = "";

    /// <summary>
    /// هدف کنترل‌شده. کلاینت نمی‌تواند رشتهٔ آزاد بفرستد.
    /// </summary>
    public OtpPurpose Purpose { get; init; }

    /// <summary>
    /// هش راز یک‌بارمصرف.
    /// </summary>
    public string SecretHash { get; init; } = "";

    /// <summary>
    /// زمان صدور.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// انقضا؛ پس از آن Verify شکست می‌خورد.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// زمان مصرف موفق؛ پس از آن چالش single-use است.
    /// </summary>
    public DateTimeOffset? ConsumedAt { get; set; }

    /// <summary>
    /// زمان قفل پس از حد تلاش.
    /// </summary>
    public DateTimeOffset? LockedAt { get; set; }

    /// <summary>
    /// شمار تلاش ناموفق. برای محدود کردن brute-force روی همین چالش است نه antifraud تجاری.
    /// </summary>
    public int AttemptCount { get; set; }
}
