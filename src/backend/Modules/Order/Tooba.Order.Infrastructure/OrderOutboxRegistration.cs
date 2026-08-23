using Tooba.BuildingBlocks;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Events;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Order.Infrastructure;

/// <summary>
/// ثبت Outbox ماژول Order. رویداد پرداخت موفق ترجمه نمی‌شود.
/// </summary>
public sealed class OrderOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => OrderDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(OrderDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata)
    {
        return domainEvent switch
        {
            CheckoutSubmittedDomainEvent submitted => new CheckoutSubmittedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = CheckoutSubmittedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                CheckoutId = submitted.CheckoutId,
                CartId = submitted.CartId,
            },
            SellerOrderCreatedDomainEvent created => new SellerOrderCreatedIntegrationEvent
            {
                Metadata = metadata with
                {
                    EventType = SellerOrderCreatedIntegrationEvent.EventTypeName,
                    Version = 1,
                },
                CheckoutId = created.CheckoutId,
                SellerOrderId = created.SellerOrderId,
                SellerPartyId = created.SellerPartyId,
            },
            _ => null,
        };
    }

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType)
    {
        if (integrationEventType == typeof(CheckoutSubmittedIntegrationEvent))
        {
            return CheckoutSubmittedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(SellerOrderCreatedIntegrationEvent))
        {
            return SellerOrderCreatedIntegrationEvent.EventTypeName;
        }

        throw new InvalidOperationException($"رویداد Order ناشناخته: {integrationEventType.FullName}");
    }

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => eventTypeName switch
    {
        CheckoutSubmittedIntegrationEvent.EventTypeName => typeof(CheckoutSubmittedIntegrationEvent),
        SellerOrderCreatedIntegrationEvent.EventTypeName => typeof(SellerOrderCreatedIntegrationEvent),
        _ => null,
    };
}
