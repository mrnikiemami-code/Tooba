using Tooba.BuildingBlocks;

namespace Tooba.Payment.Contracts.Events;

/// <summary>
/// رویداد پایدار شکست Verify. سفارش را Paid نمی‌کند.
/// </summary>
public sealed class PaymentFailedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار قرارداد Outbox.
    /// </summary>
    public const string EventTypeName = "payment.failed.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// پرداخت.
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// checkout مرجع بدون FK.
    /// </summary>
    public Guid CheckoutId { get; set; }

    /// <summary>
    /// کد شکست درگاه در صورت وجود.
    /// </summary>
    public string? FailureCode { get; set; }
}
