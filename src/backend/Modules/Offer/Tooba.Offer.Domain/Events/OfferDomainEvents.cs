using Tooba.BuildingBlocks;

namespace Tooba.Offer.Domain;

/// <summary>
/// رویداد ایجاد Offer.
/// </summary>
public sealed class OfferCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// از ریشه می‌سازد.
    /// </summary>
    public OfferCreatedDomainEvent(SellerOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        OfferId = offer.OfferId;
        CatalogVariantId = offer.CatalogVariantId;
        SellerPartyId = offer.SellerPartyId;
        Metadata = EventMetadataFactory.ForDomain("offer.created.domain");
    }

    /// <summary>
    /// Offer ایجادشده.
    /// </summary>
    public Guid OfferId { get; }

    /// <summary>
    /// Variant هدف.
    /// </summary>
    public Guid CatalogVariantId { get; }

    /// <summary>
    /// فروشندهٔ Party.
    /// </summary>
    public Guid SellerPartyId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// رویداد فعال‌سازی listing.
/// </summary>
public sealed class OfferActivatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// از ریشه می‌سازد.
    /// </summary>
    public OfferActivatedDomainEvent(SellerOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        OfferId = offer.OfferId;
        Metadata = EventMetadataFactory.ForDomain("offer.activated.domain");
    }

    /// <summary>
    /// Offer فعال‌شده.
    /// </summary>
    public Guid OfferId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// رویداد تعلیق listing.
/// </summary>
public sealed class OfferSuspendedDomainEvent : IDomainEvent
{
    /// <summary>
    /// از ریشه می‌سازد.
    /// </summary>
    public OfferSuspendedDomainEvent(SellerOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        OfferId = offer.OfferId;
        Metadata = EventMetadataFactory.ForDomain("offer.suspended.domain");
    }

    /// <summary>
    /// Offer معلق.
    /// </summary>
    public Guid OfferId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// رویداد بایگانی listing.
/// </summary>
public sealed class OfferArchivedDomainEvent : IDomainEvent
{
    /// <summary>
    /// از ریشه می‌سازد.
    /// </summary>
    public OfferArchivedDomainEvent(SellerOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        OfferId = offer.OfferId;
        Metadata = EventMetadataFactory.ForDomain("offer.archived.domain");
    }

    /// <summary>
    /// Offer بایگانی‌شده.
    /// </summary>
    public Guid OfferId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}
