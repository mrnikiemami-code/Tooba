using Tooba.Offer.Contracts.Dtos;
using Tooba.BuildingBlocks;
using Tooba.Cart.Domain.ValueObjects;

namespace Tooba.Cart.Domain.Events;

/// <summary>
/// Domain type.
/// </summary>
public sealed class CartCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد ایجاد را می‌سازد.
    /// </summary>
    public CartCreatedDomainEvent(Guid cartId, CartAccessKind accessKind)
    {
        CartId = cartId;
        AccessKind = accessKind;
        Metadata = EventMetadataFactory.ForDomain("cart.created.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// سبد ایجادشده.
    /// </summary>
    public Guid CartId { get; }

    /// <summary>
    /// گونهٔ دسترسی.
    /// </summary>
    public CartAccessKind AccessKind { get; }
}
