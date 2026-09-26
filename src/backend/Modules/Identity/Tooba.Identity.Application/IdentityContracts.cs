using Tooba.Identity.Contracts;
using Tooba.Identity.Domain;

namespace Tooba.Identity.Application;

/// <summary>
/// سیاست رمز قابل پیکربندی. قانون تجاری نهایی در دامنه hard-code نمی‌شود.
/// </summary>
public sealed class IdentityPasswordPolicyOptions
{
    /// <summary>
    /// حداقل طول فعلی برای ایمنی پایه؛ پیچیدگی محصول بعداً اضافه می‌شود.
    /// </summary>
    public int MinimumLength { get; set; } = 10;

    /// <summary>
    /// اگر true باشد باید حرف و رقم داشته باشد. پیش‌فرض خاموش است تا سیاست محصول جدا بماند.
    /// </summary>
    public bool RequireLetterAndDigit { get; set; }

    /// <summary>
    /// درز برای بررسی رمز افشاشده در آینده؛ این تسک اجرا نمی‌کند.
    /// </summary>
    public bool EnableBreachedPasswordCheckLater { get; init; }

    /// <summary>
    /// درز تاریخچهٔ رمز؛ این تسک ذخیرهٔ history ندارد.
    /// </summary>
    public bool EnablePasswordHistoryLater { get; init; }
}

/// <summary>
/// عمر نشست، چالش و حد تلاش. antifraud تجاری اینجا نیست.
/// </summary>
public sealed class IdentityLifecycleOptions
{
    /// <summary>
    /// عمر Refresh نشست به ساعت.
    /// </summary>
    public int SessionLifetimeHours { get; set; } = 336;

    /// <summary>
    /// عمر چالش OTP/بازنشانی به دقیقه.
    /// </summary>
    public int ChallengeLifetimeMinutes { get; set; } = 15;

    /// <summary>
    /// حداکثر تلاش نادرست روی یک چالش قبل از قفل همان چالش.
    /// </summary>
    public int MaxChallengeAttempts { get; set; } = 5;
}

/// <summary>
/// هش استاندارد رمز؛ الگوریتم سفارشی نیست.
/// </summary>
public interface IPasswordHashingService
{
    /// <summary>
    /// هش را می‌سازد. ورودی plaintext لاگ نمی‌شود.
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// صحت را می‌سنجد و در صورت نیاز نشانهٔ rehash می‌دهد.
    /// </summary>
    PasswordVerificationOutcome Verify(string hash, string password);
}

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

/// <summary>
/// فرستندهٔ OTP. ارائه‌دهندهٔ واقعی SMS/ایمیل اینجا وصل نمی‌شود.
/// </summary>
public interface IOtpSender
{
    /// <summary>
    /// چالش را به کانال مقصد می‌فرستد. کد OTP نباید لاگ شود.
    /// </summary>
    Task SendAsync(OtpPurpose purpose, string destination, string oneTimeCode, CancellationToken cancellationToken);
}

/// <summary>
/// سرویس چالش OTP مستقل از ارائه‌دهنده.
/// </summary>
public interface IOtpChallengeService
{
    /// <summary>
    /// چالش جدید برای هدف مشخص می‌سازد و از طریق <see cref="IOtpSender"/> می‌فرستد.
    /// </summary>
    Task<OtpChallengeHandle> IssueAsync(OtpPurpose purpose, string destination, CancellationToken cancellationToken);

    /// <summary>
    /// کد را می‌سنجد. plaintext کد persist بلندمدت نمی‌شود.
    /// </summary>
    Task<bool> VerifyAsync(OtpChallengeHandle handle, string oneTimeCode, CancellationToken cancellationToken);
}

/// <summary>
/// مرز access credential. JWT سفارشی اختراع نمی‌شود؛ cookie/BFF/IdP بعداً روی همین نشست سوار می‌شوند.
/// </summary>
public interface IAccessCredentialBoundary
{
    /// <summary>
    /// بلیت کوتاه‌عمر آزمایشی از نشست صادر می‌کند بدون JWT اختصاصی.
    /// </summary>
    AuthenticationTicket ToAccessTicket(AuthenticationTicket sessionTicket);
}

/// <summary>
/// نگاشت issuer+subject به User داخلی. به Keycloak گره نمی‌خورد.
/// </summary>
public interface IExternalIdentityDirectory
{
    /// <summary>
    /// اتصال را ذخیره می‌کند.
    /// </summary>
    Task BindAsync(Guid userId, string issuer, string subject, CancellationToken cancellationToken);

    /// <summary>
    /// User داخلی را از هویت خارجی پیدا می‌کند.
    /// </summary>
    Task<Guid?> FindUserIdAsync(string issuer, string subject, CancellationToken cancellationToken);
}

/// <summary>
/// درز ثبت عامل MFA بدون UI.
/// </summary>
public interface IMfaEnrollmentStore
{
    /// <summary>
    /// عامل را برای User علامت می‌زند.
    /// </summary>
    Task EnrollAsync(Guid userId, MfaFactorKind factorKind, CancellationToken cancellationToken);

    /// <summary>
    /// عوامل فعال را می‌خواند.
    /// </summary>
    Task<IReadOnlyList<MfaFactorKind>> ListEnabledAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>
/// درز Security Audit آینده. لاگ فنی جایگزین این درز نیست.
/// </summary>
public interface IIdentitySecurityEventSink
{
    /// <summary>
    /// رخداد امنیتی را بدون راز به مصرف‌کنندهٔ بعدی می‌دهد.
    /// </summary>
    Task RecordAsync(IdentitySecurityEvent securityEvent, CancellationToken cancellationToken);
}

/// <summary>
/// رخداد امنیتی بدون payload محرمانه.
/// </summary>
public sealed class IdentitySecurityEvent
{
    /// <summary>
    /// نام رخداد مانند login_success.
    /// </summary>
    public required string EventName { get; init; }

    /// <summary>
    /// User در صورت شناخته بودن.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// زمان رخداد.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }
}
