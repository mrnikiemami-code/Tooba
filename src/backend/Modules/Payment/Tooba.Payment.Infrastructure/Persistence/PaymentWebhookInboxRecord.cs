namespace Tooba.Payment.Infrastructure.Persistence;

/// <summary>
/// dedup رویداد webhook/callback ارائه‌دهنده. callback امضاشده حقیقت Paid نیست.
/// </summary>
public sealed class PaymentWebhookInboxRecord
{
    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private PaymentWebhookInboxRecord()
    {
    }

    /// <summary>
    /// شناسهٔ داخلی inbox.
    /// </summary>
    public Guid InboxId { get; init; }

    /// <summary>
    /// کد درگاه.
    /// </summary>
    public string ProviderCode { get; init; } = string.Empty;

    /// <summary>
    /// شناسهٔ یکتا رویداد از PSP.
    /// </summary>
    public string ProviderEventId { get; init; } = string.Empty;

    /// <summary>
    /// پرداخت هدف.
    /// </summary>
    public Guid PaymentId { get; init; }

    /// <summary>
    /// زمان دریافت.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; init; }

    /// <summary>
    /// ردیف inbox را می‌سازد.
    /// </summary>
    public static PaymentWebhookInboxRecord Create(
        string providerCode,
        string providerEventId,
        Guid paymentId,
        DateTimeOffset receivedAt) =>
        new()
        {
            InboxId = Guid.NewGuid(),
            ProviderCode = providerCode.Trim(),
            ProviderEventId = providerEventId.Trim(),
            PaymentId = paymentId,
            ReceivedAt = receivedAt,
        };
}
