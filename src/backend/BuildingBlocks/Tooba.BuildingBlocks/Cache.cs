using System.Security.Cryptography;
using System.Text;

namespace Tooba.BuildingBlocks;

/// <summary>
/// کلید پایدار کش پس از نرمال‌سازی بخش‌ها. مقدار خام برای ارائه‌دهنده است؛ TenantId یا Host در خود ارائه‌دهنده ساخته نمی‌شود.
/// </summary>
/// <param name="Value">رشتهٔ قطعی پس از escape و محدودیت طول؛ راز یا PII ندارد.</param>
/// <param name="Namespace">فضای منطقی برای متریک با کاردینالیتی محدود و ابطال namespace.</param>
/// <param name="EditionLabel">برچسب Edition برای متریک؛ هویت Tenant نیست.</param>
public readonly record struct CacheKey(string Value, string Namespace, string EditionLabel);

/// <summary>
/// سیاست انقضا و برچسب ابطال برای یک ورود. TTL سراسری اجباری نیست؛ دادهٔ تغییرپذیر نباید بی‌انقضا ذخیره شود.
/// </summary>
/// <param name="AbsoluteExpiration">انقضای مطلق از لحظهٔ Set؛ برای دادهٔ تغییرپذیر الزامی است مگر Sliding مشخص باشد.</param>
/// <param name="SlidingExpiration">تمدید انقضا با هر خواندن موفق؛ جایگزین منبع حقیقت نیست.</param>
/// <param name="Tags">برچسب‌های ابطال گروهی؛ نام کسب‌وکار خاص اینجا تعریف نمی‌شود.</param>
/// <param name="CacheNull">اگر true باشد فقط با <see cref="NullAbsoluteExpiration"/> کوتاه، نبودن مقدار به‌صورت صریح کش می‌شود.</param>
/// <param name="NullAbsoluteExpiration">عمر ورود منفی؛ بدون این مقدار، null ذخیره نمی‌شود.</param>
public sealed record CachePolicy(
    TimeSpan? AbsoluteExpiration,
    TimeSpan? SlidingExpiration,
    IReadOnlyList<string> Tags,
    bool CacheNull,
    TimeSpan? NullAbsoluteExpiration)
{
    /// <summary>
    /// سیاست بدون ورود منفی و بدون برچسب.
    /// </summary>
    /// <param name="absoluteExpiration">انقضای مطلق اجباری برای دادهٔ تغییرپذیر.</param>
    /// <param name="slidingExpiration">انقضای لغزان اختیاری.</param>
    /// <param name="tags">برچسب‌های ابطال.</param>
    public static CachePolicy Expiring(
        TimeSpan absoluteExpiration,
        TimeSpan? slidingExpiration = null,
        IReadOnlyList<string>? tags = null) =>
        new(absoluteExpiration, slidingExpiration, tags ?? Array.Empty<string>(), false, null);

    /// <summary>
    /// بررسی می‌کند انقضا کران‌دار است و ورود منفی بدون TTL کوتاه مجاز نیست.
    /// </summary>
    public void EnsureBounded()
    {
        if (AbsoluteExpiration is null && SlidingExpiration is null)
        {
            throw new InvalidOperationException(
                "CachePolicy requires AbsoluteExpiration or SlidingExpiration; unbounded mutable cache is not allowed.");
        }

        if (CacheNull && NullAbsoluteExpiration is null)
        {
            throw new InvalidOperationException(
                "Negative caching requires an explicit short NullAbsoluteExpiration; not-found is not cached by default.");
        }
    }
}

/// <summary>
/// بخش‌های صریح کلید. فقط ابعادی که روی مقدار اثر دارند باید پر شوند. Locale از Market و Currency و حوزهٔ مالیات جدا است.
/// </summary>
public sealed record CacheKeyParts
{
    /// <summary>
    /// فضای منطقی مثل catalog یا theme؛ برچسب متریک و ابطال namespace است نه TenantId.
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    /// نوع منبع منطقی مثل product؛ نام جدول EF نیست.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    /// شناسهٔ منبع در مالک ماژول؛ بین Tenantها به‌تنهایی یکتا فرض نمی‌شود.
    /// </summary>
    public required string ResourceId { get; init; }

    /// <summary>
    /// Edition فرآیند استقرار تا Marketplace با SingleStore تصادم نکند.
    /// </summary>
    public required ToobaEdition Edition { get; init; }

    /// <summary>
    /// برچسب استقرار/محیط؛ نام ماشین نیست.
    /// </summary>
    public required string DeploymentId { get; init; }

    /// <summary>
    /// هویت پایدار Tenant فقط وقتی مقدار مختص فروشگاه است. در Marketplace باید تهی بماند.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// بازار تجاری وقتی مقدار به Market وابسته است؛ Locale یا Currency نیست.
    /// </summary>
    public string? Market { get; init; }

    /// <summary>
    /// زبان/فرهنگ وقتی مقدار به Locale وابسته است؛ Market نیست.
    /// </summary>
    public string? Locale { get; init; }

    /// <summary>
    /// ارز وقتی مقدار به Currency وابسته است؛ حوزهٔ مالیات نیست.
    /// </summary>
    public string? Currency { get; init; }

    /// <summary>
    /// تم وقتی خروجی به Theme وابسته است.
    /// </summary>
    public string? Theme { get; init; }

    /// <summary>
    /// دامنهٔ مجوز وقتی خروجی حساس به authorization است؛ مدل کاربر عمومی ساخته نمی‌شود.
    /// </summary>
    public string? AuthorizationScope { get; init; }

    /// <summary>
    /// نسخه/بازنگری منبع وقتی کلید نسخه‌بندی می‌شود؛ اجباری برای همهٔ کلیدها نیست.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// اگر true باشد <see cref="TenantId"/> الزامی است تا مقدار فروشگاه A با B مخلوط نشود.
    /// </summary>
    public bool TenantScoped { get; init; }
}

/// <summary>
/// خواندن/نوشتن/GetOrCreate پشت ارائه‌دهنده. ماژول کسب‌وکار <c>IMemoryCache</c> یا Redis را نمی‌بیند.
/// موجودیت tracked مربوط به EF و DbContext و HttpContext نباید ذخیره شوند؛ فقط DTO/projection تغییرناپذیر.
/// </summary>
public interface ICache
{
    /// <summary>
    /// مقدار typed را اگر موجود و منقضی‌نشده باشد برمی‌گرداند. miss یعنی باید از منبع حقیقت خواند.
    /// </summary>
    /// <typeparam name="T">قرارداد سریالایزشدنی؛ موجودیت EF نیست.</typeparam>
    /// <param name="key">کلید ساخته‌شده با <see cref="ICacheKeyBuilder"/>.</param>
    /// <param name="cancellationToken">لغو انتظار؛ برای حافظه معمولاً فوری است.</param>
    Task<T?> GetAsync<T>(CacheKey key, CancellationToken cancellationToken)
        where T : class;

    /// <summary>
    /// مقدار را با سیاست انقضا ذخیره می‌کند. شکست کارخانه اینجا مطرح نیست؛ null فقط با سیاست منفی صریح ذخیره می‌شود.
    /// </summary>
    /// <typeparam name="T">قرارداد سریالایزشدنی.</typeparam>
    /// <param name="key">کلید canonical.</param>
    /// <param name="value">مقدار؛ null بدون سیاست منفی ذخیره نمی‌شود.</param>
    /// <param name="policy">انقضا و برچسب‌ها.</param>
    /// <param name="cancellationToken">لغو.</param>
    Task SetAsync<T>(CacheKey key, T? value, CachePolicy policy, CancellationToken cancellationToken)
        where T : class;

    /// <summary>
    /// miss را با کارخانه پر می‌کند. برای یک کلید، اجرای همزمان کارخانه در فرآیند واحد single-flight است. نتیجهٔ شکست کش نمی‌شود.
    /// </summary>
    /// <typeparam name="T">قرارداد سریالایزشدنی.</typeparam>
    /// <param name="key">کلید canonical.</param>
    /// <param name="factory">منبع حقیقت؛ نباید DbContext را برگرداند.</param>
    /// <param name="policy">انقضا پس از موفقیت.</param>
    /// <param name="cancellationToken">لغو انتظار قفل per-key و کارخانه.</param>
    Task<T?> GetOrCreateAsync<T>(
        CacheKey key,
        Func<CancellationToken, Task<T?>> factory,
        CachePolicy policy,
        CancellationToken cancellationToken)
        where T : class;

    /// <summary>
    /// یک کلید را حذف می‌کند و فهرست برچسب را پاک می‌کند.
    /// </summary>
    /// <param name="key">کلید canonical.</param>
    /// <param name="cancellationToken">لغو.</param>
    Task RemoveAsync(CacheKey key, CancellationToken cancellationToken);
}

/// <summary>
/// ابطال گروهی بدون وابستگی به تکنیک Redis. ابطال باید صریح باشد؛ TTL به‌تنهایی برای دادهٔ حساس به صحت کافی نیست.
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>
    /// همهٔ کلیدهای دارای برچسب را حذف می‌کند؛ برچسب‌های نامرتبط دست نمی‌خورند.
    /// </summary>
    /// <param name="tag">برچسب منطقی مثل آیندهٔ catalog:product:{id}؛ اینجا نمونهٔ کسب‌وکار ثبت نمی‌شود.</param>
    /// <param name="cancellationToken">لغو.</param>
    Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken);

    /// <summary>
    /// همهٔ کلیدهای یک فضای منطقی را حذف می‌کند. معادل ابطال برچسب داخلی namespace است.
    /// </summary>
    /// <param name="ns">همان Namespace کلید.</param>
    /// <param name="cancellationToken">لغو.</param>
    Task InvalidateByNamespaceAsync(string ns, CancellationToken cancellationToken);
}

/// <summary>
/// ساخت کلید قطعی از بخش‌های typed. Host یا hostname داخل کلید نمی‌رود.
/// </summary>
public interface ICacheKeyBuilder
{
    /// <summary>
    /// بخش‌ها را نرمال و escape می‌کند. Marketplace نباید TenantId جعلی بگیرد؛ مقدار Tenant-scoped بدون TenantId رد می‌شود.
    /// </summary>
    /// <param name="parts">ابعاد مؤثر بر مقدار.</param>
    CacheKey Build(CacheKeyParts parts);
}

/// <summary>
/// پیاده‌سازی canonical بخش‌ها با جداکنندهٔ پایدار و محدودیت طول. راز در کلید قرار نمی‌گیرد.
/// </summary>
public sealed class CanonicalCacheKeyBuilder : ICacheKeyBuilder
{
    private const int MaxKeyLength = 512;
    private const char Separator = '|';

    /// <inheritdoc />
    public CacheKey Build(CacheKeyParts parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        if (string.IsNullOrWhiteSpace(parts.Namespace)
            || string.IsNullOrWhiteSpace(parts.ResourceType)
            || string.IsNullOrWhiteSpace(parts.ResourceId)
            || string.IsNullOrWhiteSpace(parts.DeploymentId))
        {
            throw new InvalidOperationException(
                "Cache key requires Namespace, ResourceType, ResourceId, and DeploymentId.");
        }

        if (parts.Edition == ToobaEdition.Unset)
        {
            throw new InvalidOperationException("Cache key cannot use Unset edition.");
        }

        if (parts.Edition == ToobaEdition.Marketplace && !string.IsNullOrWhiteSpace(parts.TenantId))
        {
            throw new InvalidOperationException(
                "Marketplace cache keys must not carry a SingleStore TenantId.");
        }

        if (parts.TenantScoped)
        {
            if (parts.Edition != ToobaEdition.SingleStore)
            {
                throw new InvalidOperationException("Tenant-scoped cache requires SingleStore edition.");
            }

            if (string.IsNullOrWhiteSpace(parts.TenantId))
            {
                throw new InvalidOperationException(
                    "Tenant-scoped cache requires durable TenantId; Host is not a TenantId.");
            }
        }

        var ns = NormalizeToken(parts.Namespace, lowercase: true);
        var edition = parts.Edition.ToString().ToLowerInvariant();
        var segments = new List<string>
        {
            ns,
            edition,
            NormalizeToken(parts.DeploymentId, lowercase: true),
            NormalizeToken(parts.ResourceType, lowercase: true),
            Escape(parts.ResourceId.Trim()),
        };

        AppendOptional(segments, "t", parts.TenantId, lowercase: false);
        AppendOptional(segments, "mkt", parts.Market, lowercase: true);
        AppendOptional(segments, "loc", parts.Locale, lowercase: true);
        AppendOptional(segments, "cur", parts.Currency, lowercase: false, forceUpper: true);
        AppendOptional(segments, "thm", parts.Theme, lowercase: true);
        AppendOptional(segments, "az", parts.AuthorizationScope, lowercase: true);
        AppendOptional(segments, "v", parts.Version, lowercase: false);

        var raw = string.Join(Separator, segments);
        if (raw.Length > MaxKeyLength)
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
            raw = string.Join(Separator, ns, edition, "h", hash);
        }

        return new CacheKey(raw, ns, edition);
    }

    private static void AppendOptional(
        List<string> segments,
        string label,
        string? value,
        bool lowercase,
        bool forceUpper = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var token = forceUpper
            ? value.Trim().ToUpperInvariant()
            : NormalizeToken(value, lowercase);
        segments.Add(label);
        segments.Add(token);
    }

    private static string NormalizeToken(string value, bool lowercase)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new InvalidOperationException("Cache key segment cannot be empty.");
        }

        var normalized = lowercase ? trimmed.ToLowerInvariant() : trimmed;
        return Escape(normalized);
    }

    private static string Escape(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (ch == '\\' || ch == Separator)
            {
                builder.Append('\\');
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }
}
