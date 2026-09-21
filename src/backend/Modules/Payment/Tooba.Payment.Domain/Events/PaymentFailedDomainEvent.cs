using Tooba.BuildingBlocks;

namespace Tooba.Payment.Domain.Events;

/// <summary>
/// شکست پس از Verify. سفارش را Paid نمی‌کند.
/// </summary>
public sealed class PaymentFailedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PaymentFailedDomainEvent(Guid paymentId, Guid checkoutId, string? failureCode)
    {
        PaymentId = paymentId;
        CheckoutId = checkoutId;
        FailureCode = failureCode;
        Metadata = EventMetadataFactory.ForDomain("payment.failed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// پرداخت.
    /// </summary>
    public Guid PaymentId { get; }

    /// <summary>
    /// checkout.
    /// </summary>
    public Guid CheckoutId { get; }

    /// <summary>
    /// کد شکست درگاه.
    /// </summary>
    public string? FailureCode { get; }
}
