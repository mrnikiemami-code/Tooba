using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Events;

/// <summary>
/// رویداد انتشار Catalog. خرید Offer را تضمین نمی‌کند.
/// </summary>
public sealed class CatalogProductPublishedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد انتشار.
    /// </summary>
    public CatalogProductPublishedDomainEvent(CatalogProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);
        ProductId = product.ProductId;
        Metadata = EventMetadataFactory.ForDomain("catalog.product_published.domain");
    }

    /// <summary>
    /// محصول منتشرشده در Catalog.
    /// </summary>
    public Guid ProductId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}
