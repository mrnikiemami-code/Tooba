using Tooba.BuildingBlocks;

namespace Tooba.Fulfillment.Domain.Events;


/// <summary>رویداد بسته‌بندی خط/تعداد.</summary>
public sealed class FulfillmentLinePackedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public FulfillmentLinePackedDomainEvent(Guid fulfillmentId, Guid sellerOrderId, Guid orderLineId, decimal quantity)
    {
        FulfillmentId = fulfillmentId;
        SellerOrderId = sellerOrderId;
        OrderLineId = orderLineId;
        Quantity = quantity;
        Metadata = EventMetadataFactory.ForDomain("fulfillment.line.packed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه fulfillment.</summary>
    public Guid FulfillmentId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }

    /// <summary>خط سفارش.</summary>
    public Guid OrderLineId { get; }

    /// <summary>تعداد بسته‌بندی‌شده.</summary>
    public decimal Quantity { get; }
}
