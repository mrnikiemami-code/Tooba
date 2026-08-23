using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Events;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>
/// ثبت Outbox ماژول Catalog. ترجمه فقط رویدادهای صریح تصویر Search آینده است نه هر تغییر داخلی.
/// </summary>
public sealed class CatalogOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => CatalogDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(CatalogDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata)
    {
        return domainEvent switch
        {
            CatalogProductCreatedDomainEvent created => new CatalogProductCreatedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = CatalogProductCreatedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                ProductId = created.ProductId,
            },
            CatalogProductPublishedDomainEvent published => new CatalogProductPublishedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = CatalogProductPublishedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                ProductId = published.ProductId,
            },
            CatalogProductUpdatedDomainEvent updated => new CatalogProductUpdatedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = CatalogProductUpdatedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                ProductId = updated.ProductId,
            },
            CatalogVariantCreatedDomainEvent variant => new CatalogVariantCreatedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = CatalogVariantCreatedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                VariantId = variant.VariantId,
                ProductId = variant.ProductId,
            },
            _ => null,
        };
    }

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType)
    {
        if (integrationEventType == typeof(CatalogProductCreatedIntegrationEvent))
        {
            return CatalogProductCreatedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(CatalogProductPublishedIntegrationEvent))
        {
            return CatalogProductPublishedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(CatalogProductUpdatedIntegrationEvent))
        {
            return CatalogProductUpdatedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(CatalogVariantCreatedIntegrationEvent))
        {
            return CatalogVariantCreatedIntegrationEvent.EventTypeName;
        }

        throw new InvalidOperationException("Unmapped Catalog integration event type.");
    }

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) =>
        eventTypeName switch
        {
            CatalogProductCreatedIntegrationEvent.EventTypeName => typeof(CatalogProductCreatedIntegrationEvent),
            CatalogProductPublishedIntegrationEvent.EventTypeName => typeof(CatalogProductPublishedIntegrationEvent),
            CatalogProductUpdatedIntegrationEvent.EventTypeName => typeof(CatalogProductUpdatedIntegrationEvent),
            CatalogVariantCreatedIntegrationEvent.EventTypeName => typeof(CatalogVariantCreatedIntegrationEvent),
            _ => null,
        };
}
