using Tooba.Offer.Domain.Aggregates;
using Tooba.BuildingBlocks;

namespace Tooba.Offer.Domain.Events;

/// <summary>
/// Raised when an offer is created.
/// </summary>
public sealed class OfferCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// Creates the event from an aggregate.
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
    /// Created offer identifier.
    /// </summary>
    public Guid OfferId { get; }

    /// <summary>
    /// Target Catalog variant identifier.
    /// </summary>
    public Guid CatalogVariantId { get; }

    /// <summary>
    /// Seller Party identifier.
    /// </summary>
    public Guid SellerPartyId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// Raised when an offer is activated.
/// </summary>
public sealed class OfferActivatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// Creates the event from an aggregate.
    /// </summary>
    public OfferActivatedDomainEvent(SellerOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        OfferId = offer.OfferId;
        Metadata = EventMetadataFactory.ForDomain("offer.activated.domain");
    }

    /// <summary>
    /// Activated offer identifier.
    /// </summary>
    public Guid OfferId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// Raised when an offer is suspended.
/// </summary>
public sealed class OfferSuspendedDomainEvent : IDomainEvent
{
    /// <summary>
    /// Creates the event from an aggregate.
    /// </summary>
    public OfferSuspendedDomainEvent(SellerOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        OfferId = offer.OfferId;
        Metadata = EventMetadataFactory.ForDomain("offer.suspended.domain");
    }

    /// <summary>
    /// Suspended offer identifier.
    /// </summary>
    public Guid OfferId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// Raised when an offer is archived.
/// </summary>
public sealed class OfferArchivedDomainEvent : IDomainEvent
{
    /// <summary>
    /// Creates the event from an aggregate.
    /// </summary>
    public OfferArchivedDomainEvent(SellerOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        OfferId = offer.OfferId;
        Metadata = EventMetadataFactory.ForDomain("offer.archived.domain");
    }

    /// <summary>
    /// Archived offer identifier.
    /// </summary>
    public Guid OfferId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}
