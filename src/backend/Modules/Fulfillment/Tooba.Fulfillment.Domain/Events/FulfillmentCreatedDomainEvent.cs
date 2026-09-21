using Tooba.BuildingBlocks;

namespace Tooba.Fulfillment.Domain.Events;


/// <summary>رویداد ایجاد fulfillment.</summary>
public sealed class FulfillmentCreatedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public FulfillmentCreatedDomainEvent(Guid fulfillmentId, Guid sellerOrderId, Guid checkoutId)
    {
        FulfillmentId = fulfillmentId;
        SellerOrderId = sellerOrderId;
        CheckoutId = checkoutId;
        Metadata = EventMetadataFactory.ForDomain("fulfillment.created.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }

    /// <summary>checkout مرجع.</summary>
    public Guid CheckoutId { get; }
}
