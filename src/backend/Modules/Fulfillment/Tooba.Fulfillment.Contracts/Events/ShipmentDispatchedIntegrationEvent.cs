using Tooba.BuildingBlocks;

namespace Tooba.Fulfillment.Contracts.Events;

/// <summary>
/// رویداد Outbox shipment.dispatched.v1
/// </summary>
public sealed class ShipmentDispatchedIntegrationEvent : IIntegrationEvent
{
    /// <summary>نام قرارداد.</summary>
    public const string EventTypeName = "shipment.dispatched.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; set; }

    /// <summary>شناسه محموله.</summary>
    public Guid ShipmentId { get; set; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; set; }
}
