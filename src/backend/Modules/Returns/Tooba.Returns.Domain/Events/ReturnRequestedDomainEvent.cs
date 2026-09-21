using Tooba.BuildingBlocks;

namespace Tooba.Returns.Domain.Events;


/// <summary>رویداد درخواست مرجوعی.</summary>
public sealed class ReturnRequestedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public ReturnRequestedDomainEvent(Guid returnRequestId, Guid sellerOrderId, Guid checkoutId)
    {
        ReturnRequestId = returnRequestId;
        SellerOrderId = sellerOrderId;
        CheckoutId = checkoutId;
        Metadata = EventMetadataFactory.ForDomain("return.requested.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه درخواست.</summary>
    public Guid ReturnRequestId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }

    /// <summary>checkout مرجع.</summary>
    public Guid CheckoutId { get; }
}
