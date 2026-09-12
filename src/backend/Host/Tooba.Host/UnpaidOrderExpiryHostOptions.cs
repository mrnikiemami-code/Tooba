namespace Tooba.Host;

/// <summary>تنظیمات کارگر انقضای پرداخت‌نشده از <c>Tooba:UnpaidOrderExpiry</c>.</summary>
internal sealed class UnpaidOrderExpiryHostOptions
{
    /// <summary>اگر false باشد کارگر خارج می‌شود.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>فاصلهٔ poll بر حسب ثانیه.</summary>
    public int PollIntervalSeconds { get; set; } = 15;

    /// <summary>حداکثر پرداخت در هر claim.</summary>
    public int BatchSize { get; set; } = 20;
}
