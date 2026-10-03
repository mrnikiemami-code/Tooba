using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Events;

/// <summary>
/// رویداد به‌روزرسانی توصیفی برای تصویر بعدی Search.
/// </summary>
public sealed class CatalogProductUpdatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد به‌روزرسانی.
    /// </summary>
    public CatalogProductUpdatedDomainEvent(CatalogProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);
        ProductId = product.ProductId;
        Metadata = EventMetadataFactory.ForDomain("catalog.product_updated.domain");
    }

    /// <summary>
    /// محصول تغییر یافته.
    /// </summary>
    public Guid ProductId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}
