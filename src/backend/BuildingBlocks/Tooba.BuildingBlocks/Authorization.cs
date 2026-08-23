using System.Text.RegularExpressions;

namespace Tooba.BuildingBlocks;

/// <summary>
/// نتیجهٔ صریح بررسی مجوز. DENY عادی استثنا نیست؛ شکست زیرساخت هرگز ALLOW نیست.
/// </summary>
public enum AuthorizationDecisionKind
{
    /// <summary>
    /// رابطه اجازه می‌دهد.
    /// </summary>
    Allow = 1,

    /// <summary>
    /// رابطه وجود ندارد یا کافی نیست.
    /// </summary>
    Deny = 2,

    /// <summary>
    /// SpiceDB/پیکربندی در دسترس نیست؛ عملیات محافظت‌شده نباید ادامه یابد.
    /// </summary>
    Unavailable = 3,
}

/// <summary>
/// تصمیم مجوز به‌همراه علت داخلی غیرقابل افشا در ProblemDetails عمومی.
/// </summary>
public sealed class AuthorizationDecision
{
    /// <summary>
    /// گونهٔ تصمیم.
    /// </summary>
    public required AuthorizationDecisionKind Kind { get; init; }

    /// <summary>
    /// کد پایدار داخلی؛ متن خطای زیرساخت SpiceDB نیست.
    /// </summary>
    public string? ReasonCode { get; init; }

    /// <summary>
    /// موفقیت مجوز.
    /// </summary>
    public bool IsAllow => Kind == AuthorizationDecisionKind.Allow;

    /// <summary>
    /// اجازه.
    /// </summary>
    public static AuthorizationDecision Allow() => new() { Kind = AuthorizationDecisionKind.Allow, ReasonCode = "allow" };

    /// <summary>
    /// رد.
    /// </summary>
    public static AuthorizationDecision Deny() => new() { Kind = AuthorizationDecisionKind.Deny, ReasonCode = "deny" };

    /// <summary>
    /// زیرساخت مجوز در دسترس نیست.
    /// </summary>
    public static AuthorizationDecision Unavailable(string reasonCode) =>
        new() { Kind = AuthorizationDecisionKind.Unavailable, ReasonCode = reasonCode };
}

/// <summary>
/// اصل مجوز. فعلاً فقط user؛ سازمان/گروه بعداً اضافه می‌شود نه با ستون نقش روی User.
/// </summary>
public sealed class AuthorizationSubject
{
    private static readonly Regex UserIdPattern = new("^[0-9a-fA-F-]{36}$", RegexOptions.Compiled);

    /// <summary>
    /// گونهٔ پایدار قرارداد؛ از namespace CLR ساخته نمی‌شود.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// شناسهٔ پایدار اصل؛ hostname نیست.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// User احرازشده را به subject قرارداد Tooba تبدیل می‌کند.
    /// </summary>
    public static AuthorizationSubject ForUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId تهی برای subject مجوز مجاز نیست.", nameof(userId));
        }

        var id = userId.ToString("D");
        if (!UserIdPattern.IsMatch(id))
        {
            throw new ArgumentException("قالب UserId برای subject نامعتبر است.", nameof(userId));
        }

        return new AuthorizationSubject { Type = AuthorizationObjectTypes.User, Id = id };
    }
}

/// <summary>
/// منبع مجوز. شناسه نباید راز یا hostname باشد.
/// </summary>
public sealed class AuthorizationResource
{
    /// <summary>
    /// نوع منبع snake_case پایدار.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// شناسهٔ منبع داخل همان نوع.
    /// </summary>
    public required string Id { get; init; }
}

/// <summary>
/// زمینهٔ بررسی. Tenant از Host ساخته نمی‌شود.
/// </summary>
public sealed record AuthorizationCallContext
{
    /// <summary>
    /// Tenant پایدار Single-Store یا تهی در Marketplace.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Edition فرآیند.
    /// </summary>
    public ToobaEdition Edition { get; init; }

    /// <summary>
    /// همبستگی تله‌متری؛ بعد متریک با کاردینالیتی بالا نیست.
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// نشانهٔ سازگاری آینده (ZedToken). در این foundation اجباری نیست.
    /// </summary>
    public string? ConsistencyToken { get; init; }
}

/// <summary>
/// درخواست بررسی مجوز در مرز Application.
/// </summary>
public sealed class AuthorizationCheck
{
    /// <summary>
    /// اصل.
    /// </summary>
    public required AuthorizationSubject Subject { get; init; }

    /// <summary>
    /// منبع.
    /// </summary>
    public required AuthorizationResource Resource { get; init; }

    /// <summary>
    /// نام permission پایدار snake_case.
    /// </summary>
    public required string Permission { get; init; }

    /// <summary>
    /// زمینهٔ Tenant/Edition.
    /// </summary>
    public required AuthorizationCallContext CallContext { get; init; }
}

/// <summary>
/// درخواست typed نوشتن رابطه؛ رشتهٔ خام SpiceDB از ماژول‌ها پذیرفته نمی‌شود.
/// </summary>
public sealed class AuthorizationRelationshipWrite
{
    /// <summary>
    /// اصل.
    /// </summary>
    public required AuthorizationSubject Subject { get; init; }

    /// <summary>
    /// منبع.
    /// </summary>
    public required AuthorizationResource Resource { get; init; }

    /// <summary>
    /// رابطهٔ snake_case.
    /// </summary>
    public required string Relation { get; init; }
}

/// <summary>
/// نام‌های پایدار نوع شیء. از CLR تولید نمی‌شوند.
/// </summary>
public static class AuthorizationObjectTypes
{
    /// <summary>
    /// اصل کاربر.
    /// </summary>
    public const string User = "user";

    /// <summary>
    /// منبع خنثی Tenant برای اثبات isolation؛ مدل Catalog نیست.
    /// </summary>
    public const string Tenant = "tenant";
}

/// <summary>
/// نام‌های پایدار رابطه/مجوز foundation.
/// </summary>
public static class AuthorizationRelations
{
    /// <summary>
    /// عضویت خنثی در Tenant.
    /// </summary>
    public const string Member = "member";

    /// <summary>
    /// مجوز مشاهدهٔ منبع Tenant.
    /// </summary>
    public const string View = "view";
}

/// <summary>
/// قرارداد بررسی مجوز در مرز use-case. نوع SDK SpiceDB اینجا نمی‌آید.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// بررسی Can. DENY استثنا نیست. شکست زیرساخت ALLOW برنمی‌گرداند.
    /// </summary>
    Task<AuthorizationDecision> CanAsync(AuthorizationCheck check, CancellationToken cancellationToken);
}

/// <summary>
/// نوشتن رابطهٔ typed.
/// </summary>
public interface IAuthorizationTupleWriter
{
    /// <summary>
    /// رابطه را پس از اعتبارسنجی نام‌ها می‌نویسد.
    /// </summary>
    Task WriteAsync(AuthorizationRelationshipWrite write, CancellationToken cancellationToken);
}

/// <summary>
/// schema نسخه‌بندی‌شدهٔ foundation.
/// </summary>
public interface IAuthorizationSchemaProvider
{
    /// <summary>
    /// نسخهٔ صریح schema؛ با هر استارت تولید بازنویسی کور نمی‌شود.
    /// </summary>
    int SchemaVersion { get; }

    /// <summary>
    /// متن schema سازگار با SpiceDB برای منبع خنثی.
    /// </summary>
    string SchemaText { get; }
}

/// <summary>
/// اعمال اختیاری schema در dev/test وقتی صریحاً پیکربندی شده باشد.
/// </summary>
public interface IAuthorizationSchemaBootstrapper
{
    /// <summary>
    /// اگر ApplySchemaOnStartup روشن باشد schema را اعمال می‌کند؛ در غیر این صورت no-op.
    /// </summary>
    Task BootstrapIfConfiguredAsync(CancellationToken cancellationToken);
}

/// <summary>
/// الگوی مرز use-case: ابتدا مجوز، سپس منطق کسب‌وکار. SpiceDB داخل Domain صدا نمی‌شود.
/// </summary>
public interface IAuthorizationGuard
{
    /// <summary>
    /// تصمیم را برمی‌گرداند تا caller DENY را بدون استثنا هندل کند و Unavailable را fail-closed کند.
    /// </summary>
    Task<AuthorizationDecision> AuthorizeUseCaseAsync(AuthorizationCheck check, CancellationToken cancellationToken);
}

/// <summary>
/// درز Security Audit برای رد حساس و تغییر رابطه. فروشگاه کامل Audit نیست.
/// </summary>
public interface IAuthorizationSecurityEventSink
{
    /// <summary>
    /// رخداد را بدون توکن و بدون راز ثبت می‌کند.
    /// </summary>
    Task RecordAsync(string eventName, string? resourceType, string? permission, CancellationToken cancellationToken);
}

/// <summary>
/// اعتبارسنجی نام قرارداد مجوز قبل از رسیدن به adapter.
/// </summary>
public static class AuthorizationContractValidator
{
    private static readonly Regex Token = new("^[a-z][a-z0-9_]*$", RegexOptions.Compiled);

    /// <summary>
    /// نام نوع/رابطه/مجوز را می‌سنجد.
    /// </summary>
    public static void ValidateToken(string value, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        if (!Token.IsMatch(value))
        {
            throw new ArgumentException("نام مجوز/رابطه/نوع باید snake_case پایدار باشد.", paramName);
        }
    }

    /// <summary>
    /// شناسهٔ منبع نباید خالی یا شبیه hostname خام باشد.
    /// </summary>
    public static void ValidateResourceId(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
        if (id.Contains("://", StringComparison.Ordinal) || id.Contains('/', StringComparison.Ordinal))
        {
            throw new ArgumentException("شناسهٔ منبع نباید URL/hostname باشد.", nameof(id));
        }
    }

    /// <summary>
    /// درخواست بررسی را کامل می‌سنجد.
    /// </summary>
    public static void Validate(AuthorizationCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        ValidateToken(check.Subject.Type, nameof(check.Subject.Type));
        ValidateToken(check.Resource.Type, nameof(check.Resource.Type));
        ValidateToken(check.Permission, nameof(check.Permission));
        ValidateResourceId(check.Resource.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(check.Subject.Id, nameof(check.Subject.Id));
    }

    /// <summary>
    /// درخواست نوشتن را می‌سنجد.
    /// </summary>
    public static void Validate(AuthorizationRelationshipWrite write)
    {
        ArgumentNullException.ThrowIfNull(write);
        ValidateToken(write.Subject.Type, nameof(write.Subject.Type));
        ValidateToken(write.Resource.Type, nameof(write.Resource.Type));
        ValidateToken(write.Relation, nameof(write.Relation));
        ValidateResourceId(write.Resource.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(write.Subject.Id, nameof(write.Subject.Id));
    }
}
