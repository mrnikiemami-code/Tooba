namespace Tooba.Host;

/// <summary>
/// تنظیمات reconciliation پرداخت: Tooba:PaymentReconciliation
/// </summary>
public sealed class PaymentReconciliationHostOptions
{
    /// <summary>
    /// اگر false باشد HostedService بدون poll خارج می‌شود.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// فاصلهٔ poll به ثانیه.
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// حداقل سن Pending قبل از Verify مجدد.
    /// </summary>
    public int PendingAgeMinutes { get; set; } = 5;

    /// <summary>
    /// حداکثر پرداخت در هر چرخه.
    /// </summary>
    public int BatchSize { get; set; } = 20;
}
