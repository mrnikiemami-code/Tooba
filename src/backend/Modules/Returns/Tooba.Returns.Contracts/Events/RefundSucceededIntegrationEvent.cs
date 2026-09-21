using Tooba.BuildingBlocks;

namespace Tooba.Returns.Contracts.Events;

/// <summary>
/// رویداد Outbox refund.succeeded.v1
/// </summary>
public sealed class RefundSucceededIntegrationEvent : IIntegrationEvent
{
    /// <summary>نام قرارداد.</summary>
    public const string EventTypeName = "refund.succeeded.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>شناسه درخواست.</summary>
    public Guid ReturnRequestId { get; set; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; set; }

    /// <summary>پرداخت مرجع.</summary>
    public Guid PaymentId { get; set; }

    /// <summary>مبلغ refund.</summary>
    public decimal RefundAmount { get; set; }

    /// <summary>ارز.</summary>
    public string Currency { get; set; } = string.Empty;
}
