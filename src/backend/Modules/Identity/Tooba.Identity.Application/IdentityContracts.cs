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
/// نتیجهٔ داخلی احراز. برخی مقادیر نباید عیناً به سطح عمومی بروند.
/// </summary>
public enum AuthenticationOutcome
{
    /// <summary>
    /// احراز موفق.
    /// </summary>
    Succeeded = 0,

    /// <summary>
    /// شناسه یا رمز نادرست، یا کاربر یافت نشد.
    /// </summary>
    InvalidCredentials = 1,

    /// <summary>
    /// حساب Disabled است.
    /// </summary>
    Disabled = 2,

    /// <summary>
    /// حساب Locked است.
    /// </summary>
    Locked = 3,

    /// <summary>
    /// سیاست تأیید شناسه برقرار است و شناسه تأیید نشده.
    /// </summary>
    IdentifierNotVerified = 4,
}

/// <summary>
/// خطای عمومی احراز برای جلوگیری از enumeration آشکار.
/// </summary>
public enum PublicAuthenticationError
{
    /// <summary>
    /// شکست عمومی ورود؛ جزئیات حساب لو نمی‌رود.
    /// </summary>
    InvalidCredentials = 1,
}

/// <summary>
/// مرز نشست/توکن. JWT سفارشی در این تسک ساخته نمی‌شود.
/// </summary>
public sealed class AuthenticationTicket
{
    /// <summary>
    /// اصل پایدار برای تحویل به لایهٔ Authorization بعدی.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// دستهٔ نشست داخلی؛ cookie/access/refresh بعداً روی همین مرز سوار می‌شود.
    /// </summary>
    public required Guid SessionHandle { get; init; }

    /// <summary>
    /// زمان احراز.
    /// </summary>
    public required DateTimeOffset AuthenticatedAt { get; init; }
}

/// <summary>
/// نتیجهٔ عملیات احراز با تفکیک داخلی و عمومی.
/// </summary>
public sealed class AuthenticationResult
{
    /// <summary>
    /// آیا بلیت صادر شده است.
    /// </summary>
    public bool Succeeded => Ticket is not null;

    /// <summary>
    /// بلیت در صورت موفقیت.
    /// </summary>
    public AuthenticationTicket? Ticket { get; init; }

    /// <summary>
    /// علت داخلی برای audit و تست.
    /// </summary>
    public AuthenticationOutcome Outcome { get; init; }

    /// <summary>
    /// نمای عمومی جمع‌شده.
    /// </summary>
    public PublicAuthenticationError? PublicError { get; init; }

    /// <summary>
    /// موفقیت با بلیت.
    /// </summary>
    public static AuthenticationResult Success(AuthenticationTicket ticket) => new()
    {
        Ticket = ticket,
        Outcome = AuthenticationOutcome.Succeeded,
    };

    /// <summary>
    /// شکست با علت داخلی؛ سطح عمومی معمولاً InvalidCredentials است.
    /// </summary>
    public static AuthenticationResult Fail(AuthenticationOutcome outcome, bool collapsePublicly = true) => new()
    {
        Outcome = outcome,
        PublicError = collapsePublicly
            ? PublicAuthenticationError.InvalidCredentials
            : outcome switch
            {
                AuthenticationOutcome.Disabled => PublicAuthenticationError.InvalidCredentials,
                AuthenticationOutcome.Locked => PublicAuthenticationError.InvalidCredentials,
                _ => PublicAuthenticationError.InvalidCredentials,
            },
    };
}

/// <summary>
/// فرمان ثبت حداقلی User.
/// </summary>
public sealed class RegisterUserCommand
{
    /// <summary>
    /// گونهٔ شناسهٔ اول.
    /// </summary>
    public required LoginIdentifierKind IdentifierKind { get; init; }

    /// <summary>
    /// مقدار خام شناسه.
    /// </summary>
    public required string Identifier { get; init; }

    /// <summary>
    /// رمز plaintext فقط در حافظهٔ درخواست؛ persist نمی‌شود.
    /// </summary>
    public required string Password { get; init; }
}

/// <summary>
/// نتیجهٔ ثبت بدون افشای موجودیت EF.
/// </summary>
public sealed class RegisterUserResult
{
    /// <summary>
    /// User پایدار.
    /// </summary>
    public required Guid UserId { get; init; }
}

/// <summary>
/// موارد استفادهٔ احراز هویت. موجودیت EF را بیرون نمی‌دهد.
/// </summary>
public interface IIdentityAuthenticationService
{
    /// <summary>
    /// User و اعتبار رمز را در تراکنش ماژول Identity می‌سازد.
    /// </summary>
    Task<RegisterUserResult> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// ورود با شناسه + رمز. Host را parse نمی‌کند.
    /// </summary>
    Task<AuthenticationResult> AuthenticateWithPasswordAsync(LoginIdentifierKind kind, string identifier, string password, CancellationToken cancellationToken);

    /// <summary>
    /// جستجوی User با شناسهٔ نرمال در همین دامنهٔ پایگاه.
    /// </summary>
    Task<Guid?> FindUserIdByIdentifierAsync(LoginIdentifierKind kind, string identifier, CancellationToken cancellationToken);

    /// <summary>
    /// حساب را Disabled می‌کند تا ورود ممکن نباشد.
    /// </summary>
    Task DisableAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// حساب را Locked می‌کند تا ورود ممکن نباشد.
    /// </summary>
    Task LockAsync(Guid userId, CancellationToken cancellationToken);
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
/// دستهٔ چالش OTP بدون افشای راز.
/// </summary>
public sealed class OtpChallengeHandle
{
    /// <summary>
    /// شناسهٔ چالش.
    /// </summary>
    public required Guid ChallengeId { get; init; }

    /// <summary>
    /// هدف چالش.
    /// </summary>
    public required OtpPurpose Purpose { get; init; }
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
