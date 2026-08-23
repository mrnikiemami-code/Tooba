using Tooba.BuildingBlocks;
using Tooba.Inventory.Domain;
using Tooba.Inventory.Infrastructure.Events;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Inventory.Infrastructure;

/// <summary>
/// ثبت Outbox ماژول Inventory. ترجمه فقط رویدادهای صریح موجودی است.
/// </summary>
public sealed class InventoryOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => InventoryDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(InventoryDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata)
    {
        return domainEvent switch
        {
            StockAdjustedDomainEvent adjusted => new InventoryAdjustedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = InventoryAdjustedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                StockItemId = adjusted.StockItemId,
                OfferId = adjusted.OfferId,
                Delta = adjusted.Delta,
            },
            StockReservedDomainEvent reserved => new InventoryReservedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = InventoryReservedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                ReservationId = reserved.ReservationId,
                OfferId = reserved.OfferId,
                Quantity = reserved.Quantity,
            },
            StockReleasedDomainEvent released => new InventoryReleasedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = InventoryReleasedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                ReservationId = released.ReservationId,
                OfferId = released.OfferId,
                Quantity = released.Quantity,
            },
            StockReservationConsumedDomainEvent consumed => new InventoryReservationConsumedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = InventoryReservationConsumedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                ReservationId = consumed.ReservationId,
                OfferId = consumed.OfferId,
                Quantity = consumed.Quantity,
            },
            StockAvailabilityChangedDomainEvent changed => new InventoryAvailabilityChangedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = InventoryAvailabilityChangedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                StockItemId = changed.StockItemId,
                OfferId = changed.OfferId,
                Available = changed.Available,
            },
            _ => null,
        };
    }

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType)
    {
        if (integrationEventType == typeof(InventoryAdjustedIntegrationEvent))
        {
            return InventoryAdjustedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(InventoryReservedIntegrationEvent))
        {
            return InventoryReservedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(InventoryReleasedIntegrationEvent))
        {
            return InventoryReleasedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(InventoryReservationConsumedIntegrationEvent))
        {
            return InventoryReservationConsumedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(InventoryAvailabilityChangedIntegrationEvent))
        {
            return InventoryAvailabilityChangedIntegrationEvent.EventTypeName;
        }

        throw new InvalidOperationException("Unmapped Inventory integration event type.");
    }

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) =>
        eventTypeName switch
        {
            InventoryAdjustedIntegrationEvent.EventTypeName => typeof(InventoryAdjustedIntegrationEvent),
            InventoryReservedIntegrationEvent.EventTypeName => typeof(InventoryReservedIntegrationEvent),
            InventoryReleasedIntegrationEvent.EventTypeName => typeof(InventoryReleasedIntegrationEvent),
            InventoryReservationConsumedIntegrationEvent.EventTypeName => typeof(InventoryReservationConsumedIntegrationEvent),
            InventoryAvailabilityChangedIntegrationEvent.EventTypeName => typeof(InventoryAvailabilityChangedIntegrationEvent),
            _ => null,
        };
}
