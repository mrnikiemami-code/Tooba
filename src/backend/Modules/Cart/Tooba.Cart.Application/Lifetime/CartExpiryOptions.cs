namespace Tooba.Cart.Application.Lifetime;

/// <summary>
/// Cart-owned scheduling knobs for the Cart expiry worker, bound from <c>Tooba:CartExpiry</c>
/// for configuration compatibility. Cart owns the worker; Host only supplies the section values.
/// </summary>
public sealed class CartExpiryOptions
{
    /// <summary>نام بخش پیکربندی؛ نگه‌داشته‌شده برای سازگاری پیکربندی موجود.</summary>
    public const string SectionName = "Tooba:CartExpiry";

    /// <summary>
    /// اگر false باشد کارگر بدون poll خارج می‌شود.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// فاصلهٔ poll بر حسب ثانیه (حداقل ۵).
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// حداکثر سبد/رزرو پردازش‌شده در هر claim PostgreSQL.
    /// </summary>
    public int BatchSize { get; set; } = 20;
}
