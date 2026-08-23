using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Infrastructure.Events;

/// <summary>
/// قرارداد Integration ایجاد محصول برای تصویر Search آینده. ایندکس اینجا ساخته نمی‌شود.
/// </summary>
public sealed class CatalogProductCreatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "catalog.product_created.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// محصول توصیفی ایجادشده.
    /// </summary>
    public Guid ProductId { get; set; }
}

/// <summary>
/// قرارداد انتشار Catalog. قابل‌خرید بودن Offer را اعلام نمی‌کند.
/// </summary>
public sealed class CatalogProductPublishedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "catalog.product_published.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// محصول منتشرشده در Catalog.
    /// </summary>
    public Guid ProductId { get; set; }
}

/// <summary>
/// قرارداد به‌روزرسانی توصیفی.
/// </summary>
public sealed class CatalogProductUpdatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "catalog.product_updated.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// محصول تغییر یافته.
    /// </summary>
    public Guid ProductId { get; set; }
}

/// <summary>
/// قرارداد ایجاد گونه. هویت Offer نیست.
/// </summary>
public sealed class CatalogVariantCreatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "catalog.variant_created.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// گونه.
    /// </summary>
    public Guid VariantId { get; set; }

    /// <summary>
    /// محصول والد.
    /// </summary>
    public Guid ProductId { get; set; }
}
