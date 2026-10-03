using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Events;

/// <summary>
/// رویداد ایجاد گونه. هویت Offer فروشنده نیست.
/// </summary>
public sealed class CatalogVariantCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد ایجاد گونه.
    /// </summary>
    public CatalogVariantCreatedDomainEvent(CatalogVariant variant)
    {
        ArgumentNullException.ThrowIfNull(variant);
        VariantId = variant.VariantId;
        ProductId = variant.ProductId;
        Metadata = EventMetadataFactory.ForDomain("catalog.variant_created.domain");
    }

    /// <summary>
    /// گونهٔ ایجادشده.
    /// </summary>
    public Guid VariantId { get; }

    /// <summary>
    /// محصول والد.
    /// </summary>
    public Guid ProductId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}
