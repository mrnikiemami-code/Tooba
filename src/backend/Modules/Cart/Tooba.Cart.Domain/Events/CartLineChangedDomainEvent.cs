using Tooba.Offer.Contracts.Dtos;
using Tooba.BuildingBlocks;

namespace Tooba.Cart.Domain.Events;

/// <summary>
/// Domain type.
/// </summary>
public sealed class CartLineChangedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد تغییر را می‌سازد.
    /// </summary>
    public CartLineChangedDomainEvent(Guid cartId, Guid lineId, Guid offerId, decimal quantity)
    {
        CartId = cartId;
        LineId = lineId;
        OfferId = offerId;
        Quantity = quantity;
        Metadata = EventMetadataFactory.ForDomain("cart.line_changed.v1");
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
    /// تعداد جدید.
    /// </summary>
    public decimal Quantity { get; }
}
