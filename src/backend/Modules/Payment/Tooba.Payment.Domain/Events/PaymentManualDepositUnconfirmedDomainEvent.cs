using Tooba.BuildingBlocks;

namespace Tooba.Payment.Domain.Events;

/// <summary>
/// برگشت تأیید واریز دستی به انتظار تأیید. Paid را نگه نمی‌دارد.
/// </summary>
public sealed class PaymentManualDepositUnconfirmedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public PaymentManualDepositUnconfirmedDomainEvent(Guid paymentId, Guid checkoutId, Guid attemptId)
    {
        PaymentId = paymentId;
        CheckoutId = checkoutId;
        AttemptId = attemptId;
        Metadata = EventMetadataFactory.ForDomain("payment.manual_deposit.unconfirmed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>پرداخت.</summary>
    public Guid PaymentId { get; }

    /// <summary>checkout.</summary>
    public Guid CheckoutId { get; }

    /// <summary>تلاش جدید انتظار تأیید.</summary>
    public Guid AttemptId { get; }
}
