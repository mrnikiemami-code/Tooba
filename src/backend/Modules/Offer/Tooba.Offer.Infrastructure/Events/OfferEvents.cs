using Tooba.BuildingBlocks;

namespace Tooba.Offer.Infrastructure.Events;

/// <summary>
/// قرارداد Integration ایجاد listing. قیمت اینجا نیست.
/// </summary>
public sealed class OfferCreatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "offer.created.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// Offer ایجادشده.
    /// </summary>
    public Guid OfferId { get; set; }

    /// <summary>
    /// Variant Catalog هدف.
    /// </summary>
    public Guid CatalogVariantId { get; set; }

    /// <summary>
    /// Party فروشنده.
    /// </summary>
    public Guid SellerPartyId { get; set; }
}

/// <summary>
/// قرارداد فعال‌سازی listing. اعتبار Price/Stock را اعلام نمی‌کند.
/// </summary>
public sealed class OfferActivatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "offer.activated.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// Offer فعال‌شده.
    /// </summary>
    public Guid OfferId { get; set; }
}

/// <summary>
/// قرارداد تعلیق listing.
/// </summary>
public sealed class OfferSuspendedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "offer.suspended.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// Offer معلق.
    /// </summary>
    public Guid OfferId { get; set; }
}

/// <summary>
/// قرارداد بایگانی listing.
/// </summary>
public sealed class OfferArchivedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "offer.archived.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// Offer بایگانی‌شده.
    /// </summary>
    public Guid OfferId { get; set; }
}
