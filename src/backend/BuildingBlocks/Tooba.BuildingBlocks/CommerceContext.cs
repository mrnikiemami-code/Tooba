using System.Globalization;

namespace Tooba.BuildingBlocks;

/// <summary>
/// حالت استقرار فرآیند: یک فرآیند فقط یک Edition دارد و از پیکربندی می‌آید، نه از Host درخواست.
/// </summary>
public enum ToobaEdition
{
    /// <summary>
    /// Edition هنوز قفل نشده؛ مسیرهای غیر health باید ۵۰۳ با <c>platform.edition.unconfigured</c> بدهند.
    /// </summary>
    Unset = 0,

    /// <summary>
    /// یک پایگاه marketplace؛ Host نباید به دیتابیس فروشگاه جدا resolve شود.
    /// </summary>
    Marketplace = 1,

    /// <summary>
    /// یک دیتابیس به‌ازای هر فروشگاه؛ Host ورودی routing است نه هویت پایدار Tenant.
    /// </summary>
    SingleStore = 2,
}

/// <summary>
/// وضعیت عملیاتی Tenant در control plane پیکربندی‌شده. ناشناخته/غیرفعال باید مثل ناموجود fail-closed شود.
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// Tenant مجاز به سرویس است.
    /// </summary>
    Active = 0,

    /// <summary>
    /// غیرفعال؛ resolve نباید وجود را لو بدهد.
    /// </summary>
    Disabled = 1,

    /// <summary>
    /// معلق؛ همان سیاست fail-closed ۴۰۴.
    /// </summary>
    Suspended = 2,
}

/// <summary>
/// شناسهٔ پایدار Tenant. مستقل از hostname است و لایه‌های پایین نباید آن را دوباره از Request بسازند.
/// </summary>
/// <param name="Value">مقدار پایدار TenantId؛ برابر Host نیست.</param>
public readonly record struct TenantId(string Value)
{
    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// مرجع منطقی اتصال پایگاه‌داده. credential خام نیست و نباید در ProblemDetails یا لاگ متنی چاپ شود.
/// </summary>
/// <param name="Value">کلید lookup در پیکربندی Host (بدون نویسهٔ <c>:</c> در کلید dictionary ASP.NET).</param>
public readonly record struct ConnectionReference(string Value)
{
    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// زمینهٔ Edition فرآیند جاری پس از خواندن پیکربندی معتبر.
/// </summary>
/// <param name="Edition">Marketplace یا SingleStore یا Unset.</param>
/// <param name="DeploymentId">برچسب استقرار برای تله‌متری؛ هویت Tenant نیست.</param>
public sealed record EditionContext(
    ToobaEdition Edition,
    string DeploymentId);

/// <summary>
/// زمینهٔ تغییرناپذیر Tenant جاری پس از resolve امن Host در Single-Store.
/// TenantId هویت پایدار است؛ ResolvedHost فقط ورودی routing است.
/// </summary>
/// <param name="TenantId">شناسهٔ پایدار، نه hostname.</param>
/// <param name="Status">باید Active باشد وگرنه resolve fail-closed می‌شود.</param>
/// <param name="ConnectionReference">مرجع اتصال فروشگاه این Tenant.</param>
/// <param name="DisplayName">نام نمایشی اختیاری از control plane پیکربندی.</param>
/// <param name="ThemeReference">ارجاع تم؛ قرارداد UI تجاری اینجا پیاده نمی‌شود.</param>
/// <param name="DefaultMarketReference">ارجاع بازار پیش‌فرض؛ Locale/Currency/Tax جدا هستند.</param>
/// <param name="ResolvedHost">Host نرمال‌شدهٔ همین درخواست؛ هویت نیست.</param>
/// <param name="PrimaryDomain">دامنهٔ اصلی ثبت‌شده در صورت وجود.</param>
public sealed record TenantContext(
    TenantId TenantId,
    TenantStatus Status,
    ConnectionReference ConnectionReference,
    string? DisplayName,
    string? ThemeReference,
    string? DefaultMarketReference,
    string ResolvedHost,
    string? PrimaryDomain);

/// <summary>
/// زمینهٔ تجارت درخواست پس از middleware. در Marketplace مقدار Tenant تهی است و اتصال متعلق به marketplace است.
/// هدر/کوکی/query به‌عنوان مرجع Tenant پذیرفته نمی‌شوند.
/// </summary>
/// <param name="Edition">Edition فرآیند.</param>
/// <param name="Tenant">فقط Single-Store پس از allowlist و Active.</param>
/// <param name="DatabaseConnectionReference">مرجعی که DbContext باید resolve کند.</param>
/// <param name="TraceId">همبستگی تله‌متری؛ جایگزین Audit نیست.</param>
public sealed record CommerceContext(
    EditionContext Edition,
    TenantContext? Tenant,
    ConnectionReference DatabaseConnectionReference,
    string TraceId);

/// <summary>
/// دسترسی به <see cref="CommerceContext"/> درخواست جاری. بدون resolve موفق مقدار تهی است.
/// </summary>
public interface ICurrentCommerceContext
{
    /// <summary>
    /// زمینهٔ تثبیت‌شده برای همین درخواست، یا تهی اگر هنوز resolve نشده یا مسیر skip است.
    /// </summary>
    CommerceContext? Current { get; }
}

/// <summary>
/// دسترسی محدود به Edition فرآیند در درخواست جاری.
/// </summary>
public interface ICurrentEdition
{
    /// <summary>
    /// Edition تثبیت‌شده یا تهی.
    /// </summary>
    EditionContext? Current { get; }
}

/// <summary>
/// دسترسی محدود به Tenant درخواست. در Marketplace همیشه تهی است.
/// </summary>
public interface ICurrentTenant
{
    /// <summary>
    /// Tenant Single-Store یا تهی.
    /// </summary>
    TenantContext? Current { get; }
}

/// <summary>
/// نرمال‌سازی Host ورودی routing: حروف کوچک، حذف پورت، حذف نقطهٔ انتهایی، IDNA.
/// خروجی هویت Tenant نیست و نباید به‌عنوان TenantId ذخیره شود.
/// </summary>
public static class HostNormalizer
{
    private static readonly IdnMapping Idn = new();

    /// <summary>
    /// Host هدر را به شکل canonical ASCII تبدیل می‌کند.
    /// </summary>
    /// <param name="hostHeader">مقدار خام Host؛ ممکن است پورت یا IDN داشته باشد.</param>
    /// <param name="normalized">خروجی نرمال‌شده در صورت موفقیت.</param>
    /// <returns>false برای مقدار خالی، <c>*</c>، یا IDN نامعتبر — بدون تشخیص وجود Tenant.</returns>
    public static bool TryNormalize(string? hostHeader, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(hostHeader))
        {
            return false;
        }

        var host = hostHeader.Trim();
        if (host.StartsWith('[') && host.Contains(']'))
        {
            var end = host.IndexOf(']');
            host = host[..(end + 1)];
        }
        else
        {
            var colon = host.LastIndexOf(':');
            if (colon > 0 && host.AsSpan(colon + 1).ToString().All(char.IsDigit))
            {
                host = host[..colon];
            }
        }

        host = host.Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(host) || host == "*")
        {
            return false;
        }

        try
        {
            normalized = Idn.GetAscii(host).ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            return false;
        }

        return normalized.Length > 0;
    }
}
