using Tooba.BuildingBlocks;

namespace Tooba.Fulfillment.Domain.Events;


/// <summary>TB-258: a dispatched parcel was reported as not delivered (internal operator fact, not a provider call).</summary>
public sealed class ShipmentNonDeliveredDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public ShipmentNonDeliveredDomainEvent(Guid fulfillmentId, Guid shipmentId, Guid sellerOrderId, string reason)
    {
        FulfillmentId = fulfillmentId;
        ShipmentId = shipmentId;
        SellerOrderId = sellerOrderId;
        Reason = reason;
        Metadata = EventMetadataFactory.ForDomain("shipment.nondelivered.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; }

    /// <summary>شناسه محموله.</summary>
    public Guid ShipmentId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }

    /// <summary>کد دلیل عدم تحویل.</summary>
    public string Reason { get; }
}
