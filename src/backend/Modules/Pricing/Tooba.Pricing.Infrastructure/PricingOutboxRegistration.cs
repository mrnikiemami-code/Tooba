using Tooba.BuildingBlocks;
using Tooba.Pricing.Domain;
using Tooba.Pricing.Infrastructure.Events;
using Tooba.Pricing.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Pricing.Infrastructure;

/// <summary>
/// ثبت Outbox ماژول Pricing. ترجمه فقط رویدادهای صریح قیمت نوشته‌شده است.
/// </summary>
public sealed class PricingOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => PricingDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(PricingDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata)
    {
        return domainEvent switch
        {
            PriceCreatedDomainEvent created => new PriceCreatedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = PriceCreatedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                PriceId = created.PriceId,
                OfferId = created.OfferId,
            },
            PriceActivatedDomainEvent activated => new PriceActivatedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = PriceActivatedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                PriceId = activated.PriceId,
            },
            PriceChangedDomainEvent changed => new PriceChangedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = PriceChangedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                PriceId = changed.PriceId,
            },
            PriceExpiredDomainEvent expired => new PriceExpiredIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = PriceExpiredIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                PriceId = expired.PriceId,
            },
            _ => null,
        };
    }

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType)
    {
        if (integrationEventType == typeof(PriceCreatedIntegrationEvent))
        {
            return PriceCreatedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(PriceActivatedIntegrationEvent))
        {
            return PriceActivatedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(PriceChangedIntegrationEvent))
        {
            return PriceChangedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(PriceExpiredIntegrationEvent))
        {
            return PriceExpiredIntegrationEvent.EventTypeName;
        }

        throw new InvalidOperationException("Unmapped Pricing integration event type.");
    }

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) =>
        eventTypeName switch
        {
            PriceCreatedIntegrationEvent.EventTypeName => typeof(PriceCreatedIntegrationEvent),
            PriceActivatedIntegrationEvent.EventTypeName => typeof(PriceActivatedIntegrationEvent),
            PriceChangedIntegrationEvent.EventTypeName => typeof(PriceChangedIntegrationEvent),
            PriceExpiredIntegrationEvent.EventTypeName => typeof(PriceExpiredIntegrationEvent),
            _ => null,
        };
}
