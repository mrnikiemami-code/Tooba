namespace Tooba.Host;

/// <summary>
/// تنظیمات کارگر انقضای سبد از بخش <c>Tooba:CartExpiry</c>. مستقل از Outbox است.
/// </summary>
internal sealed class CartExpiryHostOptions
{
    /// <summary>
    /// اگر false باشد HostedService بدون poll خارج می‌شود.
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
