namespace Tooba.Payment.Infrastructure.Workers;

/// <summary>
/// Payment-owned scheduling knobs for <see cref="PaymentReconciliationWorker"/>, bound from
/// <c>Tooba:PaymentReconciliation</c> for configuration compatibility. Payment owns the worker;
/// Host only supplies the section values.
/// </summary>
public sealed class PaymentReconciliationOptions
{
    /// <summary>نام بخش پیکربندی؛ نگه‌داشته‌شده برای سازگاری پیکربندی موجود.</summary>
    public const string SectionName = "Tooba:PaymentReconciliation";

    /// <summary>
    /// اگر false باشد کارگر بدون poll خارج می‌شود.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// فاصلهٔ poll بر حسب ثانیه (حداقل مؤثر ۱۵؛ پیش‌فرض ۶۰). مقادیر نامعتبر به کف ۱۵ ثانیه normalize می‌شوند.
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// حداقل سن Pending قبل از Verify مجدد (دقیقه، حداقل ۱).
    /// </summary>
    public int PendingAgeMinutes { get; set; } = 5;

    /// <summary>
    /// حداکثر پرداخت در هر چرخه (حداقل ۱).
    /// </summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>
    /// فاصلهٔ poll نرمال‌شده (ثانیه). تنها نقطهٔ clamp سیاست زمان‌بندی Payment است؛
    /// حداقل مؤثر ۱۵ ثانیه است (parity با رفتار Host پیش از انتقال مالکیت) و مقادیر کمتر به ۱۵ ثانیه ارتقا می‌یابند.
    /// </summary>
    public TimeSpan NormalizedPollInterval =>
        TimeSpan.FromSeconds(PollIntervalSeconds < 15 ? 15 : PollIntervalSeconds);

    /// <summary>
    /// حداقل سن Pending نرمال‌شده؛ هر مقدار کمتر از ۱ دقیقه به ۱ دقیقه ارتقا می‌یابد.
    /// </summary>
    public TimeSpan NormalizedPendingAge =>
        TimeSpan.FromMinutes(PendingAgeMinutes < 1 ? 1 : PendingAgeMinutes);

    /// <summary>
    /// اندازهٔ batch نرمال‌شده؛ هر مقدار غیرمثبت به پیش‌فرض ۲۰ ارتقا می‌یابد.
    /// </summary>
    public int NormalizedBatchSize => BatchSize <= 0 ? 20 : BatchSize;
}
