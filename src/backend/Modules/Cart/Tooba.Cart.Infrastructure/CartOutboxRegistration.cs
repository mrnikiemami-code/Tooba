using Tooba.BuildingBlocks;
using Tooba.Cart.Domain;
using Tooba.Cart.Infrastructure.Events;
using Tooba.Cart.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Cart.Infrastructure;

/// <summary>
/// ثبت Outbox ماژول Cart. ترجمه فقط رویدادهای صریح سبد است.
/// </summary>
public sealed class CartOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => CartDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(CartDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata)
    {
        return domainEvent switch
        {
            CartCreatedDomainEvent created => new CartCreatedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = CartCreatedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                CartId = created.CartId,
            },
            CartLineAddedDomainEvent added => new CartLineAddedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = CartLineAddedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                CartId = added.CartId,
                LineId = added.LineId,
                OfferId = added.OfferId,
                Quantity = added.Quantity,
            },
            CartLineChangedDomainEvent changed => new CartLineChangedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = CartLineChangedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                CartId = changed.CartId,
                LineId = changed.LineId,
                OfferId = changed.OfferId,
                Quantity = changed.Quantity,
            },
            CartLineRemovedDomainEvent removed => new CartLineRemovedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = CartLineRemovedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                CartId = removed.CartId,
                LineId = removed.LineId,
                OfferId = removed.OfferId,
            },
            CartExpiredDomainEvent expired => new CartExpiredIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = CartExpiredIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                CartId = expired.CartId,
            },
            CartConvertedDomainEvent converted => new CartConvertedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = CartConvertedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                CartId = converted.CartId,
            },
            _ => null,
        };
    }

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType)
    {
        if (integrationEventType == typeof(CartCreatedIntegrationEvent))
        {
            return CartCreatedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(CartLineAddedIntegrationEvent))
        {
            return CartLineAddedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(CartLineChangedIntegrationEvent))
        {
            return CartLineChangedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(CartLineRemovedIntegrationEvent))
        {
            return CartLineRemovedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(CartExpiredIntegrationEvent))
        {
            return CartExpiredIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(CartConvertedIntegrationEvent))
        {
            return CartConvertedIntegrationEvent.EventTypeName;
        }

        throw new InvalidOperationException("Unmapped Cart integration event type.");
    }

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) =>
        eventTypeName switch
        {
            CartCreatedIntegrationEvent.EventTypeName => typeof(CartCreatedIntegrationEvent),
            CartLineAddedIntegrationEvent.EventTypeName => typeof(CartLineAddedIntegrationEvent),
            CartLineChangedIntegrationEvent.EventTypeName => typeof(CartLineChangedIntegrationEvent),
            CartLineRemovedIntegrationEvent.EventTypeName => typeof(CartLineRemovedIntegrationEvent),
            CartExpiredIntegrationEvent.EventTypeName => typeof(CartExpiredIntegrationEvent),
            CartConvertedIntegrationEvent.EventTypeName => typeof(CartConvertedIntegrationEvent),
            _ => null,
        };
}
