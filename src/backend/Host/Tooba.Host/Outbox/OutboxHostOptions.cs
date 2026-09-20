using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// تنظیمات حلقهٔ dispatcher از بخش <c>Tooba:Outbox</c>. صفر بودن صف شرط /ready نیست.
/// </summary>
internal sealed class OutboxHostOptions
{
    /// <summary>
    /// اگر false باشد HostedService بلافاصله بدون poll خارج می‌شود.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// فاصلهٔ poll بر حسب ثانیه.
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 2;

    /// <summary>
    /// حداکثر ردیف claim در هر ماژول برای هر Tenant.
    /// </summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>
    /// تأخیر پایهٔ backoff نمایی پس از شکست.
    /// </summary>
    public int RetryBaseDelaySeconds { get; set; } = 2;

    /// <summary>
    /// سقف attempt_count قبل از dead-letter.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// مدت قفل نرم claim بر حسب ثانیه.
    /// </summary>
    public int LockSeconds { get; set; } = 30;
}
