using Tooba.BuildingBlocks;

namespace Tooba.Returns.Domain.Events;


/// <summary>رویداد تأیید مرجوعی.</summary>
public sealed class ReturnApprovedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public ReturnApprovedDomainEvent(
        Guid returnRequestId,
        Guid sellerOrderId,
        Guid checkoutId,
        decimal refundAmount,
        string currency)
    {
        ReturnRequestId = returnRequestId;
        SellerOrderId = sellerOrderId;
        CheckoutId = checkoutId;
        RefundAmount = refundAmount;
        Currency = currency;
        Metadata = EventMetadataFactory.ForDomain("return.approved.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه درخواست.</summary>
    public Guid ReturnRequestId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }

    /// <summary>checkout مرجع.</summary>
    public Guid CheckoutId { get; }

    /// <summary>مبلغ refund.</summary>
    public decimal RefundAmount { get; }

    /// <summary>ارز.</summary>
    public string Currency { get; }
}
