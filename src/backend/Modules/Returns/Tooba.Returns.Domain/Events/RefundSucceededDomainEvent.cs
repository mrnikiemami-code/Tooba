using Tooba.BuildingBlocks;

namespace Tooba.Returns.Domain.Events;


/// <summary>رویداد موفقیت refund.</summary>
public sealed class RefundSucceededDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public RefundSucceededDomainEvent(
        Guid returnRequestId,
        Guid sellerOrderId,
        Guid paymentId,
        decimal refundAmount,
        string currency)
    {
        ReturnRequestId = returnRequestId;
        SellerOrderId = sellerOrderId;
        PaymentId = paymentId;
        RefundAmount = refundAmount;
        Currency = currency;
        Metadata = EventMetadataFactory.ForDomain("refund.succeeded.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه درخواست.</summary>
    public Guid ReturnRequestId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }

    /// <summary>پرداخت مرجع.</summary>
    public Guid PaymentId { get; }

    /// <summary>مبلغ refund.</summary>
    public decimal RefundAmount { get; }

    /// <summary>ارز.</summary>
    public string Currency { get; }
}
