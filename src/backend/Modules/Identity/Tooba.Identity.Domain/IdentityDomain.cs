using Tooba.BuildingBlocks;

namespace Tooba.Identity.Domain;

/// <summary>
/// وضعیت چرخهٔ عمر حساب احراز هویت. مشتری، فروشنده یا Tenant نیست؛ فقط اجازهٔ ورود را محدود می‌کند.
/// </summary>
public enum UserAccountStatus
{
    /// <summary>
    /// حساب فعال است و در صورت اعتبار درست می‌تواند احراز شود.
    /// </summary>
    Active = 0,

    /// <summary>
    /// حساب به‌صورت اداری تعلیق شده و نباید احراز شود.
    /// </summary>
    Disabled = 1,

    /// <summary>
    /// حساب قفل شده (مثلاً پس از شکست‌های امنیتی) و نباید احراز شود.
    /// </summary>
    Locked = 2,
}

/// <summary>
/// گونهٔ شناسهٔ ورود. ستون ثابت Username/Email/Phone روی User نیست؛ نوع جدید با ثبت handler اضافه می‌شود نه بازنویسی هسته.
/// </summary>
public enum LoginIdentifierKind
{
    /// <summary>
    /// نام کاربری انتخابی کاربر داخل همان دامنهٔ هویت.
    /// </summary>
    Username = 1,

    /// <summary>
    /// نشانی رایانامه پس از نرمال‌سازی مخصوص ایمیل.
    /// </summary>
    Email = 2,

    /// <summary>
    /// شمارهٔ تلفن پس از نرمال‌سازی رقم‌محور بدون فرض ایران‌محور.
    /// </summary>
    Phone = 3,

    /// <summary>
    /// شناسهٔ ملی یا معادل آینده؛ در این تسک احراز نمی‌شود ولی نوع رزرو شده است.
    /// </summary>
    NationalId = 4,

    /// <summary>
    /// شناسهٔ پایدار ارائه‌دهندهٔ خارجی (issuer+subject جداگانه ذخیره می‌شود).
    /// </summary>
    ExternalProvider = 5,
}

/// <summary>
/// وضعیت اثبات مالکیت شناسه. وجود شناسه با تأیید مالکیت یکی نیست.
/// </summary>
public enum IdentifierVerificationState
{
    /// <summary>
    /// هنوز اثبات کنترل انجام نشده است.
    /// </summary>
    Unverified = 0,

    /// <summary>
    /// مالکیت شناسه اثبات شده است.
    /// </summary>
    Verified = 1,

    /// <summary>
    /// تأیید لغو شده و نباید برای ورود سیاست‌محور استفاده شود.
    /// </summary>
    Revoked = 2,
}

/// <summary>
/// گونهٔ عامل MFA آینده. سیاست MFA نقش کسب‌وکار نیست.
/// </summary>
public enum MfaFactorKind
{
    /// <summary>
    /// چالش یک‌بارمصرف پیامکی/ایمیلی به‌عنوان عامل دوم.
    /// </summary>
    Otp = 1,

    /// <summary>
    /// رمز زمان‌محور Authenticator.
    /// </summary>
    Totp = 2,

    /// <summary>
    /// Passkey / WebAuthn.
    /// </summary>
    WebAuthn = 3,

    /// <summary>
    /// ارتقای جلسه از طریق IdP خارجی.
    /// </summary>
    ExternalIdpStepUp = 4,
}

/// <summary>
/// هدف چالش OTP. یک هدف واحد hard-code نمی‌شود تا login/verify/reset/MFA جدا بمانند.
/// </summary>
public enum OtpPurpose
{
    /// <summary>
    /// ورود بدون رمز یا تکمیل ورود.
    /// </summary>
    Login = 1,

    /// <summary>
    /// اثبات مالکیت شناسه.
    /// </summary>
    IdentifierVerification = 2,

    /// <summary>
    /// بازیابی رمز.
    /// </summary>
    PasswordReset = 3,

    /// <summary>
    /// عامل دوم MFA.
    /// </summary>
    Mfa = 4,
}

/// <summary>
/// نرمال‌سازی نوع‌ویژه برای جستجوی پایدار شناسه. یک Lowercase عمومی روی همهٔ انواع اعمال نمی‌شود.
/// </summary>
public static class LoginIdentifierNormalizer
{
    /// <summary>
    /// مقدار نمایشی را trim می‌کند و مقدار نرمال را بر اساس گونه می‌سازد.
    /// </summary>
    /// <param name="kind">گونهٔ شناسه؛ قوانین جدا دارند.</param>
    /// <param name="rawValue">ورودی کاربر؛ نباید لاگ شود اگر محرمانه تلقی شود.</param>
    /// <returns>جفت نمایش و کلید نرمال.</returns>
    /// <exception cref="ArgumentException">وقتی مقدار پس از نرمال تهی است.</exception>
    public static (string Display, string Normalized) Normalize(LoginIdentifierKind kind, string rawValue)
    {
        ArgumentNullException.ThrowIfNull(rawValue);
        var display = rawValue.Trim();
        if (display.Length == 0)
        {
            throw new ArgumentException("شناسهٔ ورود پس از پیرایش تهی است.", nameof(rawValue));
        }

        var normalized = kind switch
        {
            LoginIdentifierKind.Email => NormalizeEmail(display),
            LoginIdentifierKind.Username => NormalizeUsername(display),
            LoginIdentifierKind.Phone => NormalizePhone(display),
            LoginIdentifierKind.NationalId => NormalizeNationalId(display),
            LoginIdentifierKind.ExternalProvider => display.Trim(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "گونهٔ شناسه پشتیبانی نمی‌شود."),
        };

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("شناسهٔ ورود پس از نرمال‌سازی تهی است.", nameof(rawValue));
        }

        return (display, normalized);
    }

    /// <summary>
    /// ایمیل: پیرایش و lowercase اینورینت؛ نقطه/plus collapsing انجام نمی‌شود تا رفتار ارائه‌دهنده حدس زده نشود.
    /// </summary>
    public static string NormalizeEmail(string display)
    {
        var trimmed = display.Trim();
        var at = trimmed.LastIndexOf('@');
        if (at <= 0 || at == trimmed.Length - 1)
        {
            throw new ArgumentException("قالب ایمیل برای هویت نامعتبر است.", nameof(display));
        }

        return trimmed.ToLowerInvariant();
    }

    /// <summary>
    /// نام کاربری: پیرایش و lowercase اینورینت برای یکتایی؛ فاصلهٔ داخلی حذف نمی‌شود تا قانون محصول جدا بماند.
    /// </summary>
    public static string NormalizeUsername(string display) => display.Trim().ToLowerInvariant();

    /// <summary>
    /// تلفن: فقط ارقام و حداکثر یک + ابتدایی؛ پیش‌فرض کشور ایران hard-code نمی‌شود.
    /// </summary>
    public static string NormalizePhone(string display)
    {
        var trimmed = display.Trim();
        var chars = new List<char>(trimmed.Length);
        var i = 0;
        if (trimmed.StartsWith('+'))
        {
            chars.Add('+');
            i = 1;
        }

        for (; i < trimmed.Length; i++)
        {
            if (char.IsAsciiDigit(trimmed[i]))
            {
                chars.Add(trimmed[i]);
            }
        }

        if (chars.Count == 0 || (chars.Count == 1 && chars[0] == '+'))
        {
            throw new ArgumentException("شمارهٔ تلفن پس از نرمال‌سازی رقم معتبری ندارد.", nameof(display));
        }

        return new string(chars.ToArray());
    }

    /// <summary>
    /// شناسهٔ ملی آینده: پیرایش و حذف جداکننده؛ قانون رقم ایران اینجا قفل نمی‌شود.
    /// </summary>
    public static string NormalizeNationalId(string display)
    {
        var chars = display.Trim().Where(char.IsLetterOrDigit).ToArray();
        if (chars.Length == 0)
        {
            throw new ArgumentException("شناسهٔ ملی پس از نرمال‌سازی تهی است.", nameof(display));
        }

        return new string(chars).ToUpperInvariant();
    }
}

/// <summary>
/// شناسهٔ ورود متعلق به یک User. ستون ثابت روی خود User نیست.
/// </summary>
public sealed class LoginIdentifier
{
    /// <summary>
    /// شناسهٔ پایدار ردیف شناسه.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// کلید User مالک؛ FK بین‌ماژولی به Party نیست.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// گونهٔ typed برای lookup.
    /// </summary>
    public LoginIdentifierKind Kind { get; init; }

    /// <summary>
    /// مقدار نمایش برای کاربر.
    /// </summary>
    public string DisplayValue { get; init; } = string.Empty;

    /// <summary>
    /// کلید یکتایی در دامنهٔ هویت همان پایگاه Tenant/Marketplace.
    /// </summary>
    public string NormalizedValue { get; init; } = string.Empty;

    /// <summary>
    /// وضعیت اثبات مالکیت.
    /// </summary>
    public IdentifierVerificationState VerificationState { get; set; }

    /// <summary>
    /// نشانهٔ ترجیح UX؛ جایگزین یکتایی نیست.
    /// </summary>
    public bool IsPreferred { get; set; }

    /// <summary>
    /// زمان ایجاد به‌وقت UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان تأیید در صورت Verified.
    /// </summary>
    public DateTimeOffset? VerifiedAt { get; set; }
}

/// <summary>
/// فرادادهٔ هش رمز. plaintext و خود هش نباید لاگ شوند.
/// </summary>
public sealed class PasswordCredential
{
    /// <summary>
    /// کلید User.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// خروجی hasher استاندارد ASP.NET؛ الگوریتم سفارشی نیست.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// نسخهٔ قالب هش برای ارتقای بعدی.
    /// </summary>
    public int HasherFormatVersion { get; set; }

    /// <summary>
    /// زمان آخرین تغییر اعتبار.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// اتصال آینده به IdP. User به یک Keycloak خاص وابسته نیست.
/// </summary>
public sealed class ExternalIdentityBinding
{
    /// <summary>
    /// کلید پایدار اتصال.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// User داخلی Tooba.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// صادرکنندهٔ پایدار (مثلاً issuer OIDC)؛ ایمیل کلید نیست.
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// subject پایدار در همان issuer.
    /// </summary>
    public string Subject { get; init; } = string.Empty;

    /// <summary>
    /// زمان ایجاد اتصال.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// ثبت نام‌نویسی عامل MFA بدون پیاده‌سازی UI.
/// </summary>
public sealed class MfaFactorEnrollment
{
    /// <summary>
    /// کلید ردیف نام‌نویسی.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// User مالک عامل.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// گونهٔ عامل آینده.
    /// </summary>
    public MfaFactorKind FactorKind { get; init; }

    /// <summary>
    /// آیا عامل فعال است.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// اصل احراز هویت. پروفایل مشتری، سازمان فروشنده یا Tenant نیست.
/// </summary>
public sealed class UserAccount : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// شناسهٔ پایدار User برای تحویل به Authorization بعدی.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// وضعیت ورود.
    /// </summary>
    public UserAccountStatus Status { get; set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان آخرین تغییر وضعیت یا اعتبار.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// مهر امنیتی حساب. با تغییر رمز/بازنشانی/قفل افزایش می‌یابد تا نشست‌های قبلی Refresh نشوند. ماتریس مجوز محصول نیست.
    /// </summary>
    public int SecurityStamp { get; set; }

    /// <summary>
    /// اعتبار رمز در صورت ثبت؛ نبودن یعنی ورود رمزی ممکن نیست.
    /// </summary>
    public PasswordCredential? Password { get; set; }

    /// <summary>
    /// شناسه‌های ورود متعلق به همین User. جدول جدا است نه ستون ثابت روی User.
    /// </summary>
    public List<LoginIdentifier> Identifiers { get; } = [];

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <summary>
    /// User جدید با یک شناسه می‌سازد. فیلد کسب‌وکار Party اضافه نمی‌شود.
    /// </summary>
    public static UserAccount Register(LoginIdentifierKind kind, string rawIdentifier, DateTimeOffset now)
    {
        var (display, normalized) = LoginIdentifierNormalizer.Normalize(kind, rawIdentifier);
        var user = new UserAccount
        {
            UserId = UuidV7.New(),
            Status = UserAccountStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };
        user.Identifiers.Add(new LoginIdentifier
        {
            Id = UuidV7.New(),
            UserId = user.UserId,
            Kind = kind,
            DisplayValue = display,
            NormalizedValue = normalized,
            VerificationState = IdentifierVerificationState.Unverified,
            IsPreferred = true,
            CreatedAt = now,
        });
        user.Raise(new UserRegisteredDomainEvent(user.UserId));
        return user;
    }

    /// <summary>
    /// مهر امنیتی را جلو می‌برد تا Refresh نشست‌های قبلی با همان نسخه نامعتبر شود.
    /// </summary>
    public void BumpSecurityStamp(DateTimeOffset now)
    {
        SecurityStamp++;
        UpdatedAt = now;
    }

    /// <summary>
    /// شناسهٔ ورود اضافی به همین User وصل می‌کند.
    /// </summary>
    public LoginIdentifier AddIdentifier(LoginIdentifierKind kind, string rawIdentifier, DateTimeOffset now)
    {
        var (display, normalized) = LoginIdentifierNormalizer.Normalize(kind, rawIdentifier);
        var identifier = new LoginIdentifier
        {
            Id = UuidV7.New(),
            UserId = UserId,
            Kind = kind,
            DisplayValue = display,
            NormalizedValue = normalized,
            VerificationState = IdentifierVerificationState.Unverified,
            CreatedAt = now,
        };
        Identifiers.Add(identifier);
        UpdatedAt = now;
        return identifier;
    }

    /// <summary>
    /// رویداد دامنه را صف می‌کند؛ انتشار Integration نیست.
    /// </summary>
    public void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// واقعیت داخلی ثبت User. هر Domain Event قرارداد خارجی نیست.
/// </summary>
public sealed class UserRegisteredDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد ثبت را با UserId پایدار می‌سازد.
    /// </summary>
    public UserRegisteredDomainEvent(Guid userId)
    {
        UserId = userId;
        Metadata = EventMetadataFactory.ForDomain("identity.user_registered.domain");
    }

    /// <summary>
    /// User تازه ایجادشده.
    /// </summary>
    public Guid UserId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// نشست احراز هویت. User نیست، بلیت مجوز محصول نیست، و راز Refresh را plaintext نگه نمی‌دارد.
/// </summary>
public sealed class AuthSession
{
    /// <summary>
    /// شناسهٔ پایدار نشست؛ همان SessionHandle بلیت است.
    /// </summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// اصل ورود مالک نشست. FK به Party نیست.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// زمان ایجاد نشست.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// پایان اعتبار Refresh؛ پس از آن چرخش مجاز نیست.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// آخرین استفادهٔ موفق (صدور یا چرخش).
    /// </summary>
    public DateTimeOffset LastUsedAt { get; set; }

    /// <summary>
    /// زمان لغو؛ تهی یعنی هنوز قابل Refresh است اگر مهر و انقضا درست باشند.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// علت لغو برای audit؛ راز نیست.
    /// </summary>
    public string? RevocationReason { get; set; }

    /// <summary>
    /// نسخهٔ مهر امنیتی در زمان صدور. اگر از User.SecurityStamp عقب بماند Refresh شکست می‌خورد.
    /// </summary>
    public int CredentialVersion { get; init; }

    /// <summary>
    /// Edition فرآیند در زمان صدور؛ از Host پارس نمی‌شود.
    /// </summary>
    public ToobaEdition Edition { get; init; }

    /// <summary>
    /// Tenant پایدار فقط در Single-Store؛ در Marketplace تهی است.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// برچسب اختیاری دستگاه/کلاینت؛ User-Agent خام نیست.
    /// </summary>
    public string? ClientLabel { get; init; }

    /// <summary>
    /// هش SHA-256 راز Refresh جاری. plaintext هرگز persist نمی‌شود.
    /// </summary>
    public string RefreshSecretHash { get; set; } = "";

    /// <summary>
    /// هش راز قبلی پس از چرخش برای تشخیص reuse. اگر ارائه شود خانواده لغو می‌شود.
    /// </summary>
    public string? PreviousRefreshSecretHash { get; set; }

    /// <summary>
    /// خانوادهٔ چرخش برای تشخیص replay.
    /// </summary>
    public Guid RefreshFamilyId { get; init; }

    /// <summary>
    /// نشست هنوز برای چرخش زنده است.
    /// </summary>
    public bool IsRefreshable(DateTimeOffset now, int userSecurityStamp) =>
        RevokedAt is null
        && now < ExpiresAt
        && CredentialVersion == userSecurityStamp;
}

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
