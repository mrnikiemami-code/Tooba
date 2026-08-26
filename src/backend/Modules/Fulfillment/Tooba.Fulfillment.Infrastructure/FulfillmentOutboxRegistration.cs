using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Fulfillment.Infrastructure;

/// <summary>
/// ثبت Outbox ماژول Fulfillment. سفارش را مستقیم به‌روز نمی‌کند؛ ترجمه فقط Integration پایدار است.
/// </summary>
public sealed class FulfillmentOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => FulfillmentDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(FulfillmentDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) =>
        domainEvent switch
        {
            FulfillmentCreatedDomainEvent created => new FulfillmentCreatedIntegrationEvent
            {
                Metadata = metadata with { EventType = FulfillmentCreatedIntegrationEvent.EventTypeName, Version = 1 },
                FulfillmentId = created.FulfillmentId,
                SellerOrderId = created.SellerOrderId,
                CheckoutId = created.CheckoutId,
            },
            ShipmentDispatchedDomainEvent dispatched => new ShipmentDispatchedIntegrationEvent
            {
                Metadata = metadata with { EventType = ShipmentDispatchedIntegrationEvent.EventTypeName, Version = 1 },
                FulfillmentId = dispatched.FulfillmentId,
                ShipmentId = dispatched.ShipmentId,
                SellerOrderId = dispatched.SellerOrderId,
            },
            _ => null,
        };

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        integrationEventType switch
        {
            _ when integrationEventType == typeof(FulfillmentCreatedIntegrationEvent) => FulfillmentCreatedIntegrationEvent.EventTypeName,
            _ when integrationEventType == typeof(ShipmentDispatchedIntegrationEvent) => ShipmentDispatchedIntegrationEvent.EventTypeName,
            _ => throw new InvalidOperationException("Unmapped Fulfillment integration event type."),
        };

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) =>
        eventTypeName switch
        {
            FulfillmentCreatedIntegrationEvent.EventTypeName => typeof(FulfillmentCreatedIntegrationEvent),
            ShipmentDispatchedIntegrationEvent.EventTypeName => typeof(ShipmentDispatchedIntegrationEvent),
            _ => null,
        };
}
