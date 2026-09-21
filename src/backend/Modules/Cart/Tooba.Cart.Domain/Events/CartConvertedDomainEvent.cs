using Tooba.Offer.Contracts.Dtos;
using Tooba.BuildingBlocks;
using Tooba.Cart.Domain.ValueObjects;

namespace Tooba.Cart.Domain.Events;

/// <summary>
/// Domain type.
/// </summary>
public sealed class CartConvertedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد تبدیل را می‌سازد.
    /// </summary>
    public CartConvertedDomainEvent(Guid cartId, CartConversionIntent intent)
    {
        CartId = cartId;
        Intent = intent;
        Metadata = EventMetadataFactory.ForDomain("cart.converted.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// سبد تبدیل‌شده.
    /// </summary>
    public Guid CartId { get; }

    /// <summary>
    /// مسیر سفارش آینده.
    /// </summary>
    public CartConversionIntent Intent { get; }
}
