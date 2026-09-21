using Tooba.BuildingBlocks;

namespace Tooba.Returns.Contracts.Events;

/// <summary>
/// رویداد Outbox return.requested.v1
/// </summary>
public sealed class ReturnRequestedIntegrationEvent : IIntegrationEvent
{
    /// <summary>نام قرارداد.</summary>
    public const string EventTypeName = "return.requested.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>شناسه درخواست.</summary>
    public Guid ReturnRequestId { get; set; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; set; }

    /// <summary>checkout مرجع.</summary>
    public Guid CheckoutId { get; set; }
}
