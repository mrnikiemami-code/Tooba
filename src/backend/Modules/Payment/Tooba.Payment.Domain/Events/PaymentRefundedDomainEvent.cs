using Tooba.BuildingBlocks;

namespace Tooba.Payment.Domain.Events;

/// <summary>بازگشت وجه نزد درگاه تکمیل شد.</summary>
public sealed class PaymentRefundedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public PaymentRefundedDomainEvent(Guid paymentId, Guid checkoutId, decimal amount, string currency)
    {
        PaymentId = paymentId;
        CheckoutId = checkoutId;
        Amount = amount;
        Currency = currency;
        Metadata = EventMetadataFactory.ForDomain("payment.refunded.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>پرداخت.</summary>
    public Guid PaymentId { get; }

    /// <summary>checkout.</summary>
    public Guid CheckoutId { get; }

    /// <summary>مبلغ.</summary>
    public decimal Amount { get; }

    /// <summary>ارز.</summary>
    public string Currency { get; }
}
