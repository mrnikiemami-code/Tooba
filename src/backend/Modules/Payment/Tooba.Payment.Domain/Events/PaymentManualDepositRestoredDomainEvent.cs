using Tooba.BuildingBlocks;

namespace Tooba.Payment.Domain.Events;

/// <summary>
/// بازگرداندن رد واریز دستی به انتظار تأیید. Paid نمی‌کند.
/// </summary>
public sealed class PaymentManualDepositRestoredDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PaymentManualDepositRestoredDomainEvent(Guid paymentId, Guid checkoutId, Guid attemptId)
    {
        PaymentId = paymentId;
        CheckoutId = checkoutId;
        AttemptId = attemptId;
        Metadata = EventMetadataFactory.ForDomain("payment.manual_deposit.restored.v1");
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
