namespace Tooba.Identity.Infrastructure;

/// <summary>
/// پیکربندی تحویل OTP: Tooba:Identity:OtpDelivery
/// </summary>
public sealed class OtpDeliveryOptions
{
    /// <summary>
    /// Capturing (dev/test), Webhook (production provider), Disabled.
    /// </summary>
    public string Mode { get; set; } = "Capturing";

    /// <summary>
    /// Webhook endpoint for provider-backed delivery.
    /// </summary>
    public string WebhookUrl { get; set; } = "";

    /// <summary>
    /// Bearer/API key header value (env-injected in Production).
    /// </summary>
    public string WebhookApiKey { get; set; } = "";

    /// <summary>
    /// Per-request timeout seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;
}
