namespace Tooba.BuildingBlocks;

/// <summary>
/// درز نگاشت HTTP فنی برای Host؛ طبقه‌بندی استثنای دامنه نیست و جایگزین Audit نمی‌شود.
/// </summary>
/// <remarks>
/// جزئیات پیاده‌سازی و connection string نباید از طریق این نوع به کلاینت نشت کند.
/// کد پایدار خطا (در صورت وجود) برای ProblemDetails است، نه پیام داخلی.
/// </remarks>
public sealed class PlatformHttpException : Exception
{
    /// <summary>
    /// استثنای قابل‌نگاشت به پاسخ HTTP کنترل‌شده می‌سازد.
    /// </summary>
    /// <param name="statusCode">وضعیت HTTP هدف (مثلاً ۴۰۴ fail-closed یا ۵۰۳ پیکربندی ناقص).</param>
    /// <param name="title">عنوان عمومی قابل‌نمایش؛ نباید مسیر فایل یا SQL باشد.</param>
    /// <param name="errorCode">کد پایدار اختیاری برای کلاینت‌های پلتفرم.</param>
    public PlatformHttpException(int statusCode, string title, string? errorCode = null)
        : base(title)
    {
        StatusCode = statusCode;
        Title = title;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// وضعیت HTTP که Host باید بنویسد.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// عنوان ProblemDetails؛ پیام فنی داخلی نیست.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// شناسهٔ پایدار خطا (مثلاً <c>platform.resolution.failed</c>) یا تهی برای خطای عمومی.
    /// </summary>
    public string? ErrorCode { get; }
}
