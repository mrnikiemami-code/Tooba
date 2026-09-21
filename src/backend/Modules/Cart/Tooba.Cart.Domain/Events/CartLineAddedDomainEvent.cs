using Tooba.Offer.Contracts.Dtos;
using Tooba.BuildingBlocks;

namespace Tooba.Cart.Domain.Events;

/// <summary>
/// Domain type.
/// </summary>
public sealed class CartLineAddedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد افزودن را می‌سازد.
    /// </summary>
    public CartLineAddedDomainEvent(Guid cartId, Guid lineId, Guid offerId, decimal quantity)
    {
        CartId = cartId;
        LineId = lineId;
        OfferId = offerId;
        Quantity = quantity;
        Metadata = EventMetadataFactory.ForDomain("cart.line_added.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// سبد.
    /// </summary>
    public Guid CartId { get; }

    /// <summary>
    /// خط.
    /// </summary>
    public Guid LineId { get; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; }

    /// <summary>
    /// تعداد.
    /// </summary>
    public decimal Quantity { get; }
}
