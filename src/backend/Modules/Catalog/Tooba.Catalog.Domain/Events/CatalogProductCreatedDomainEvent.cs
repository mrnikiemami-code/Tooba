using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Events;

/// <summary>
/// رویداد دامنهٔ ایجاد محصول. ایندکس Search اینجا اجرا نمی‌شود.
/// </summary>
public sealed class CatalogProductCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را از ریشه می‌سازد.
    /// </summary>
    public CatalogProductCreatedDomainEvent(CatalogProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);
        ProductId = product.ProductId;
        Metadata = EventMetadataFactory.ForDomain("catalog.product_created.domain");
    }

    /// <summary>
    /// محصول ایجادشده.
    /// </summary>
    public Guid ProductId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}
