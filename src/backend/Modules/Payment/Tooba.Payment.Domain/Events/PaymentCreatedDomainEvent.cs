using Tooba.BuildingBlocks;

namespace Tooba.Payment.Domain.Events;

/// <summary>
/// ایجاد پرداخت. سفارش را Paid نمی‌کند.
/// </summary>
public sealed class PaymentCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PaymentCreatedDomainEvent(Guid paymentId, Guid checkoutId)
    {
        PaymentId = paymentId;
        CheckoutId = checkoutId;
        Metadata = EventMetadataFactory.ForDomain("payment.created.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// پرداخت.
    /// </summary>
    public Guid PaymentId { get; }

    /// <summary>
    /// checkout مرجع.
    /// </summary>
    public Guid CheckoutId { get; }
}
