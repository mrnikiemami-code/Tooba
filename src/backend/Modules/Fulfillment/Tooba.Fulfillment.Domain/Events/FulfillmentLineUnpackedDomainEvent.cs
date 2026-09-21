using Tooba.BuildingBlocks;

namespace Tooba.Fulfillment.Domain.Events;


/// <summary>رویداد بازگشت از بسته‌بندی.</summary>
public sealed class FulfillmentLineUnpackedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public FulfillmentLineUnpackedDomainEvent(Guid fulfillmentId, Guid sellerOrderId, Guid orderLineId, decimal quantity)
    {
        FulfillmentId = fulfillmentId;
        SellerOrderId = sellerOrderId;
        OrderLineId = orderLineId;
        Quantity = quantity;
        Metadata = EventMetadataFactory.ForDomain("fulfillment.line.unpacked.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }

    /// <summary>خط سفارش.</summary>
    public Guid OrderLineId { get; }

    /// <summary>تعداد بازگشتی از بسته‌بندی.</summary>
    public decimal Quantity { get; }
}
