using Tooba.BuildingBlocks;

namespace Tooba.Returns.Contracts.Events;

/// <summary>
/// رویداد Outbox return.approved.v1
/// </summary>
public sealed class ReturnApprovedIntegrationEvent : IIntegrationEvent
{
    /// <summary>نام قرارداد.</summary>
    public const string EventTypeName = "return.approved.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>شناسه درخواست.</summary>
    public Guid ReturnRequestId { get; set; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; set; }

    /// <summary>checkout مرجع.</summary>
    public Guid CheckoutId { get; set; }

    /// <summary>مبلغ refund.</summary>
    public decimal RefundAmount { get; set; }

    /// <summary>ارز.</summary>
    public string Currency { get; set; } = string.Empty;
}
