using Tooba.BuildingBlocks;

namespace Tooba.Payment.Domain.Events;

/// <summary>
/// شروع درگاه؛ موفقیت پرداخت نیست.
/// </summary>
public sealed class PaymentInitiatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PaymentInitiatedDomainEvent(Guid paymentId, Guid attemptId, string providerRequestReference)
    {
        PaymentId = paymentId;
        AttemptId = attemptId;
        ProviderRequestReference = providerRequestReference;
        Metadata = EventMetadataFactory.ForDomain("payment.initiated.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// پرداخت.
    /// </summary>
    public Guid PaymentId { get; }

    /// <summary>
    /// تلاش.
    /// </summary>
    public Guid AttemptId { get; }

    /// <summary>
    /// مرجع درخواست درگاه.
    /// </summary>
    public string ProviderRequestReference { get; }
}
