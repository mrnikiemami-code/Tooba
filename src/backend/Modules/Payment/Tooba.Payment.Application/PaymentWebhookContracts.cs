namespace Tooba.Payment.Application;

/// <summary>
/// بدنهٔ webhook/callback امضاشده از PSP.
/// </summary>
public sealed record PaymentWebhookNotification(
    string ProviderEventId,
    Guid PaymentId,
    Guid AttemptId,
    string ProviderRequestReference,
    decimal Amount,
    string Currency,
    string Status);

/// <summary>
/// نتیجهٔ پردازش webhook.
/// </summary>
public sealed record PaymentWebhookHandleResult(
    bool Accepted,
    bool Duplicate,
    string? ErrorCode);

/// <summary>
/// پردازش webhook امضاشده و Verify مستقل از متن success.
/// </summary>
public interface IPaymentWebhookHandler
{
    /// <summary>
    /// webhook را dedup، amount-match و Verify می‌کند.
    /// </summary>
    Task<PaymentWebhookHandleResult> HandleAsync(
        string providerCode,
        PaymentWebhookNotification notification,
        CancellationToken cancellationToken);
}
