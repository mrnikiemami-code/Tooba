using Tooba.BuildingBlocks;

namespace Tooba.Payment.Contracts.Events;

/// <summary>
/// رویداد پایدار موفقیت Verify. تصویر Paid سفارش فقط از مصرف این قرارداد ساخته می‌شود نه از تراکنش همزمان Payment.
/// </summary>
public sealed class PaymentSucceededIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار قرارداد Outbox.
    /// </summary>
    public const string EventTypeName = "payment.succeeded.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// پرداخت تأییدشده.
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// checkout مرجع بدون FK.
    /// </summary>
    public Guid CheckoutId { get; set; }

    /// <summary>
    /// مبلغ تصویر سفارش.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// ارز تصویر سفارش.
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// مرجع تراکنش تأییدشدهٔ درگاه.
    /// </summary>
    public string ProviderTransactionReference { get; set; } = string.Empty;

    /// <summary>
    /// سفارش‌های فروشندهٔ هدف تخصیص.
    /// </summary>
    public Guid[] SellerOrderIds { get; set; } = [];
}
