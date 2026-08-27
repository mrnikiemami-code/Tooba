namespace Tooba.Payment.Infrastructure;

/// <summary>
/// پیکربندی درگاه پرداخت: Payment:Gateway
/// </summary>
public sealed class PaymentGatewayOptions
{
    /// <summary>
    /// نام بخش پیکربندی.
    /// </summary>
    public const string SectionName = "Payment:Gateway";

    /// <summary>
    /// Sandbox (dev/test)، Webhook (production provider)، Disabled.
    /// </summary>
    public string Mode { get; set; } = "Sandbox";

    /// <summary>
    /// کد درگاه پیش‌فرض برای شروع پرداخت.
    /// </summary>
    public string DefaultProvider { get; set; } = "fake";

    /// <summary>
    /// راز امضای webhook/callback. در Production از env تزریق می‌شود.
    /// </summary>
    public string WebhookSigningSecret { get; set; } = "";

    /// <summary>
    /// پایهٔ URL شروع پرداخت نزد PSP (بدون انتخاب تجاری در کد).
    /// </summary>
    public string InitiateBaseUrl { get; set; } = "";

    /// <summary>
    /// پایهٔ URL پرس‌وجوی وضعیت PSP برای Verify مستقل از متن callback.
    /// </summary>
    public string StatusQueryBaseUrl { get; set; } = "";

    /// <summary>
    /// Bearer/API key برای StatusQuery.
    /// </summary>
    public string StatusQueryApiKey { get; set; } = "";

    /// <summary>
    /// میزبان‌های مجاز برای StatusQuery (SSRF fail-closed). خالی = فقط https و رد localhost/private.
    /// </summary>
    public string[] AllowedStatusQueryHosts { get; set; } = [];

    /// <summary>
    /// مهلت HTTP به ثانیه.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// حداکثر تلاش Verify برای خطاهای موقت (timeout/rate-limit/unavailable).
    /// </summary>
    public int VerifyMaxAttempts { get; set; } = 3;
}
