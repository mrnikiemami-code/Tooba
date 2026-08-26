namespace Tooba.Fulfillment.Infrastructure.Persistence;

/// <summary>
/// dedup رویداد payment.succeeded برای ایجاد fulfillment.
/// </summary>
public sealed class FulfillmentPaymentInboxRecord
{
    /// <summary>
    /// شناسهٔ یکتا رویداد integration.
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// پرداخت مرجع.
    /// </summary>
    public Guid PaymentId { get; init; }

    /// <summary>
    /// زمان پردازش inbox.
    /// </summary>
    public DateTimeOffset ProcessedAt { get; init; }
}
