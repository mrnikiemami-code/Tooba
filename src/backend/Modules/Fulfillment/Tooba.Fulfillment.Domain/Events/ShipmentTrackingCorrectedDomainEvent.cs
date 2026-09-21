using Tooba.BuildingBlocks;

namespace Tooba.Fulfillment.Domain.Events;


/// <summary>رویداد اصلاح کد رهگیری پیش از ارسال.</summary>
public sealed class ShipmentTrackingCorrectedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public ShipmentTrackingCorrectedDomainEvent(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid sellerOrderId,
        string? previousTrackingReference,
        string? trackingReference)
    {
        FulfillmentId = fulfillmentId;
        ShipmentId = shipmentId;
        SellerOrderId = sellerOrderId;
        PreviousTrackingReference = previousTrackingReference;
        TrackingReference = trackingReference;
        Metadata = EventMetadataFactory.ForDomain("shipment.tracking.corrected.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; }

    /// <summary>شناسه محموله.</summary>
    public Guid ShipmentId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }

    /// <summary>کد رهگیری قبلی.</summary>
    public string? PreviousTrackingReference { get; }

    /// <summary>کد رهگیری جدید.</summary>
    public string? TrackingReference { get; }
}
