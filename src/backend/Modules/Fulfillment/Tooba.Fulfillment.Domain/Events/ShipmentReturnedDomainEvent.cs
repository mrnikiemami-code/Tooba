using Tooba.BuildingBlocks;

namespace Tooba.Fulfillment.Domain.Events;


/// <summary>
/// TB-258: origin receipt confirmed for a returned parcel. This is the durable fact that makes the physically
/// received usable quantity eligible for controlled resend — never the non-delivery record alone.
/// </summary>
public sealed class ShipmentReturnedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public ShipmentReturnedDomainEvent(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid sellerOrderId,
        Guid receiptId,
        decimal usableQuantity)
    {
        FulfillmentId = fulfillmentId;
        ShipmentId = shipmentId;
        SellerOrderId = sellerOrderId;
        ReceiptId = receiptId;
        UsableQuantity = usableQuantity;
        Metadata = EventMetadataFactory.ForDomain("shipment.returned.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; }

    /// <summary>شناسه محموله.</summary>
    public Guid ShipmentId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }

    /// <summary>شناسه رسید دریافت در مبدأ.</summary>
    public Guid ReceiptId { get; }

    /// <summary>تعداد قابل استفادهٔ دریافت‌شده.</summary>
    public decimal UsableQuantity { get; }
}
