using Tooba.BuildingBlocks;

namespace Tooba.Fulfillment.Contracts.Events;

/// <summary>
/// رویداد Outbox fulfillment.created.v1
/// </summary>
public sealed class FulfillmentCreatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>نام قرارداد.</summary>
    public const string EventTypeName = "fulfillment.created.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; set; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; set; }

    /// <summary>checkout مرجع.</summary>
    public Guid CheckoutId { get; set; }
}
