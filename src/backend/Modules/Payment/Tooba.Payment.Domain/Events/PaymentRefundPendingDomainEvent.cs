using Tooba.BuildingBlocks;

namespace Tooba.Payment.Domain.Events;

/// <summary>شروع بازگشت وجه پس از لغو سفارش؛ موفقیت درگاه نیست.</summary>
public sealed class PaymentRefundPendingDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public PaymentRefundPendingDomainEvent(Guid paymentId, Guid checkoutId)
    {
        PaymentId = paymentId;
        CheckoutId = checkoutId;
        Metadata = EventMetadataFactory.ForDomain("payment.refund_pending.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>پرداخت.</summary>
    public Guid PaymentId { get; }

    /// <summary>checkout.</summary>
    public Guid CheckoutId { get; }
}
