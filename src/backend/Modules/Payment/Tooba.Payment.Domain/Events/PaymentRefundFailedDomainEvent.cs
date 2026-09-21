using Tooba.BuildingBlocks;

namespace Tooba.Payment.Domain.Events;

/// <summary>شکست بازگشت وجه؛ سفارش Cancelled می‌ماند.</summary>
public sealed class PaymentRefundFailedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public PaymentRefundFailedDomainEvent(Guid paymentId, Guid checkoutId, string? failureCode)
    {
        PaymentId = paymentId;
        CheckoutId = checkoutId;
        FailureCode = failureCode;
        Metadata = EventMetadataFactory.ForDomain("payment.refund_failed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>پرداخت.</summary>
    public Guid PaymentId { get; }

    /// <summary>checkout.</summary>
    public Guid CheckoutId { get; }

    /// <summary>کد شکست.</summary>
    public string? FailureCode { get; }
}
