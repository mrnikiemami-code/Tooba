using Tooba.BuildingBlocks;

namespace Tooba.Order.Domain.Events;

/// <summary>
/// رویداد ارسال checkout.
/// </summary>
public sealed class CheckoutSubmittedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public CheckoutSubmittedDomainEvent(Guid checkoutId, Guid cartId, OrderMode mode)
    {
        CheckoutId = checkoutId;
        CartId = cartId;
        Mode = mode;
        Metadata = EventMetadataFactory.ForDomain("order.checkout_submitted.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// checkout.
    /// </summary>
    public Guid CheckoutId { get; }

    /// <summary>
    /// سبد مبدأ.
    /// </summary>
    public Guid CartId { get; }

    /// <summary>
    /// حالت.
    /// </summary>
    public OrderMode Mode { get; }
}

