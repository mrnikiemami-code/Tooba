using Tooba.BuildingBlocks;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Events;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Offer.Infrastructure;

/// <summary>
/// ثبت Outbox ماژول Offer. ترجمه فقط رویدادهای صریح listing است.
/// </summary>
public sealed class OfferOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => OfferDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(OfferDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata)
    {
        return domainEvent switch
        {
            OfferCreatedDomainEvent created => new OfferCreatedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = OfferCreatedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                OfferId = created.OfferId,
                CatalogVariantId = created.CatalogVariantId,
                SellerPartyId = created.SellerPartyId,
            },
            OfferActivatedDomainEvent activated => new OfferActivatedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = OfferActivatedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                OfferId = activated.OfferId,
            },
            OfferSuspendedDomainEvent suspended => new OfferSuspendedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = OfferSuspendedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                OfferId = suspended.OfferId,
            },
            OfferArchivedDomainEvent archived => new OfferArchivedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = OfferArchivedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                OfferId = archived.OfferId,
            },
            _ => null,
        };
    }

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType)
    {
        if (integrationEventType == typeof(OfferCreatedIntegrationEvent))
        {
            return OfferCreatedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(OfferActivatedIntegrationEvent))
        {
            return OfferActivatedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(OfferSuspendedIntegrationEvent))
        {
            return OfferSuspendedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(OfferArchivedIntegrationEvent))
        {
            return OfferArchivedIntegrationEvent.EventTypeName;
        }

        throw new InvalidOperationException("Unmapped Offer integration event type.");
    }

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) =>
        eventTypeName switch
        {
            OfferCreatedIntegrationEvent.EventTypeName => typeof(OfferCreatedIntegrationEvent),
            OfferActivatedIntegrationEvent.EventTypeName => typeof(OfferActivatedIntegrationEvent),
            OfferSuspendedIntegrationEvent.EventTypeName => typeof(OfferSuspendedIntegrationEvent),
            OfferArchivedIntegrationEvent.EventTypeName => typeof(OfferArchivedIntegrationEvent),
            _ => null,
        };
}
