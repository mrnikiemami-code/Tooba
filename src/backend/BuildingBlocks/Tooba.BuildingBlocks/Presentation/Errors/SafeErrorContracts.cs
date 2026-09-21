namespace Tooba.BuildingBlocks.Presentation.Errors;

/// <summary>شدت لاگ/طبقه‌بندی خطای نگاشت‌شده.</summary>
public enum ErrorSeverity
{
    /// <summary>خطای کسب‌وکار قابل انتظار.</summary>
    Warning = 0,

    /// <summary>خطای غیرمنتظرهٔ زیرساخت/سیستم.</summary>
    Error = 1,
}

/// <summary>طبقه‌بندی پایدار خطا برای نگاشت HTTP.</summary>
public enum ErrorClassification
{
    /// <summary>اعتبارسنجی ورودی.</summary>
    Validation = 0,

    /// <summary>منبع پیدا نشد.</summary>
    NotFound = 1,

    /// <summary>تعارض وضعیت.</summary>
    Conflict = 2,

    /// <summary>ممنوع.</summary>
    Forbidden = 3,

    /// <summary>خطای معنایی کسب‌وکار عمومی.</summary>
    Business = 4,

    /// <summary>خطای پلتفرم با وضعیت HTTP صریح.</summary>
    Platform = 5,

    /// <summary>خطای ناشناخته.</summary>
    Unexpected = 6,
}

/// <summary>
/// خروجی امن نگاشت خطا برای کلاینت — بدون exception.Message خام.
/// </summary>
/// <param name="StatusCode">کد HTTP.</param>
/// <param name="ErrorCode">کد پایدار.</param>
/// <param name="LocalizationKey">کلید محلی‌سازی (معمولاً همان کد).</param>
/// <param name="Arguments">آرگومان‌های ساخت‌یافته.</param>
/// <param name="Classification">طبقه‌بندی.</param>
/// <param name="Severity">شدت لاگ پیشنهادی.</param>
/// <param name="SafeTitleFallback">عنوان امن انگلیسی اگر catalog موجود نباشد.</param>
/// <param name="ValidationErrors">خطاهای فیلد اعتبارسنجی.</param>
public sealed record MappedSafeError(
    int StatusCode,
    string ErrorCode,
    string LocalizationKey,
    IReadOnlyDictionary<string, string?> Arguments,
    ErrorClassification Classification,
    ErrorSeverity Severity,
    string SafeTitleFallback,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

/// <summary>قرارداد نگاشت استثنا به payload امن کلاینت.</summary>
public interface ISafeErrorMapper
{
    /// <summary>استثنا را بدون نشت Message خام طبقه‌بندی می‌کند.</summary>
    MappedSafeError Map(Exception exception);
}
