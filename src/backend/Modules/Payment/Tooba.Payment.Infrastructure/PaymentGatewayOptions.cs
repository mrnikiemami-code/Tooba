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
    /// پایهٔ URL پرس‌وجوی وضعیت PSP برای Verify مستقل از متن callback.
    /// </summary>
    public string StatusQueryBaseUrl { get; set; } = "";

    /// <summary>
    /// Bearer/API key برای StatusQuery.
    /// </summary>
    public string StatusQueryApiKey { get; set; } = "";

    /// <summary>
    /// مهلت HTTP به ثانیه.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 15;
}
