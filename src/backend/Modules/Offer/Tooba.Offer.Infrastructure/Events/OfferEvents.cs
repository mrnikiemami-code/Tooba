using Tooba.BuildingBlocks;

namespace Tooba.Offer.Infrastructure.Events;

/// <summary>
/// Integration event emitted when a listing is created.
/// </summary>
public sealed class OfferCreatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Stable event type name.
    /// </summary>
    public const string EventTypeName = "offer.created.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// Created offer identifier.
    /// </summary>
    public Guid OfferId { get; set; }

    /// <summary>
    /// Target Catalog variant identifier.
    /// </summary>
    public Guid CatalogVariantId { get; set; }

    /// <summary>
    /// Seller Party identifier.
    /// </summary>
    public Guid SellerPartyId { get; set; }
}

/// <summary>
/// Integration event emitted when a listing is activated.
/// </summary>
public sealed class OfferActivatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Stable event type name.
    /// </summary>
    public const string EventTypeName = "offer.activated.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// Activated offer identifier.
    /// </summary>
    public Guid OfferId { get; set; }
}

/// <summary>
/// Integration event emitted when a listing is suspended.
/// </summary>
public sealed class OfferSuspendedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Stable event type name.
    /// </summary>
    public const string EventTypeName = "offer.suspended.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// Suspended offer identifier.
    /// </summary>
    public Guid OfferId { get; set; }
}

/// <summary>
/// Integration event emitted when a listing is archived.
/// </summary>
public sealed class OfferArchivedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Stable event type name.
    /// </summary>
    public const string EventTypeName = "offer.archived.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// Archived offer identifier.
    /// </summary>
    public Guid OfferId { get; set; }
}
