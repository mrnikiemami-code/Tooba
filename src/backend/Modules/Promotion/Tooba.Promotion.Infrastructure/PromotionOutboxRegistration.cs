using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.Promotion.Domain;
using Tooba.Promotion.Infrastructure.Events;
using Tooba.Promotion.Infrastructure.Persistence;

namespace Tooba.Promotion.Infrastructure;

/// <summary>
/// ثبت Outbox ماژول Promotion.
/// </summary>
public sealed class PromotionOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => PromotionDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(PromotionDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata)
    {
        return domainEvent switch
        {
            PromotionCreatedDomainEvent created => new PromotionCreatedIntegrationEvent
            {
                Metadata = metadata with { EventType = PromotionCreatedIntegrationEvent.EventTypeName, Version = 1 },
                PromotionId = created.PromotionId,
            },
            PromotionActivatedDomainEvent activated => new PromotionActivatedIntegrationEvent
            {
                Metadata = metadata with { EventType = PromotionActivatedIntegrationEvent.EventTypeName, Version = 1 },
                PromotionId = activated.PromotionId,
            },
            PromotionChangedDomainEvent changed => new PromotionChangedIntegrationEvent
            {
                Metadata = metadata with { EventType = PromotionChangedIntegrationEvent.EventTypeName, Version = 1 },
                PromotionId = changed.PromotionId,
            },
            PromotionExpiredDomainEvent expired => new PromotionExpiredIntegrationEvent
            {
                Metadata = metadata with { EventType = PromotionExpiredIntegrationEvent.EventTypeName, Version = 1 },
                PromotionId = expired.PromotionId,
            },
            _ => null,
        };
    }

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType)
    {
        if (integrationEventType == typeof(PromotionCreatedIntegrationEvent))
        {
            return PromotionCreatedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(PromotionActivatedIntegrationEvent))
        {
            return PromotionActivatedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(PromotionChangedIntegrationEvent))
        {
            return PromotionChangedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(PromotionExpiredIntegrationEvent))
        {
            return PromotionExpiredIntegrationEvent.EventTypeName;
        }

        throw new InvalidOperationException("Unmapped Promotion integration event type.");
    }

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) =>
        eventTypeName switch
        {
            PromotionCreatedIntegrationEvent.EventTypeName => typeof(PromotionCreatedIntegrationEvent),
            PromotionActivatedIntegrationEvent.EventTypeName => typeof(PromotionActivatedIntegrationEvent),
            PromotionChangedIntegrationEvent.EventTypeName => typeof(PromotionChangedIntegrationEvent),
            PromotionExpiredIntegrationEvent.EventTypeName => typeof(PromotionExpiredIntegrationEvent),
            _ => null,
        };
}
