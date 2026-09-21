using Tooba.Offer.Contracts.Dtos;
using Tooba.BuildingBlocks;

namespace Tooba.Cart.Domain.Events;

/// <summary>
/// Domain type.
/// </summary>
public sealed class CartLineRemovedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد حذف را می‌سازد.
    /// </summary>
    public CartLineRemovedDomainEvent(Guid cartId, Guid lineId, Guid offerId)
    {
        CartId = cartId;
        LineId = lineId;
        OfferId = offerId;
        Metadata = EventMetadataFactory.ForDomain("cart.line_removed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// سبد.
    /// </summary>
    public Guid CartId { get; }

    /// <summary>
    /// خط حذف‌شده.
    /// </summary>
    public Guid LineId { get; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; }
}
