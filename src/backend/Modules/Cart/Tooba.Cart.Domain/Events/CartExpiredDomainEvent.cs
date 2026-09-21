using Tooba.Offer.Contracts.Dtos;
using Tooba.BuildingBlocks;

namespace Tooba.Cart.Domain.Events;

/// <summary>
/// Domain type.
/// </summary>
public sealed class CartExpiredDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد انقضا را می‌سازد.
    /// </summary>
    public CartExpiredDomainEvent(Guid cartId)
    {
        CartId = cartId;
        Metadata = EventMetadataFactory.ForDomain("cart.expired.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// سبد منقضی یا رهاشده.
    /// </summary>
    public Guid CartId { get; }
}
