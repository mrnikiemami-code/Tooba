using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.Returns.Application;
using Tooba.Returns.Domain;
using Tooba.Returns.Infrastructure.Persistence;

namespace Tooba.Returns.Infrastructure;

/// <summary>
/// ثبت Outbox ماژول Returns. سفارش/پرداخت را مستقیم به‌روز نمی‌کند؛ ترجمه فقط Integration پایدار است.
/// </summary>
public sealed class ReturnsOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => ReturnsDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(ReturnsDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) =>
        domainEvent switch
        {
            ReturnRequestedDomainEvent requested => new ReturnRequestedIntegrationEvent
            {
                Metadata = metadata with { EventType = ReturnRequestedIntegrationEvent.EventTypeName, Version = 1 },
                ReturnRequestId = requested.ReturnRequestId,
                SellerOrderId = requested.SellerOrderId,
                CheckoutId = requested.CheckoutId,
            },
            ReturnApprovedDomainEvent approved => new ReturnApprovedIntegrationEvent
            {
                Metadata = metadata with { EventType = ReturnApprovedIntegrationEvent.EventTypeName, Version = 1 },
                ReturnRequestId = approved.ReturnRequestId,
                SellerOrderId = approved.SellerOrderId,
                CheckoutId = approved.CheckoutId,
                RefundAmount = approved.RefundAmount,
                Currency = approved.Currency,
            },
            RefundSucceededDomainEvent succeeded => new RefundSucceededIntegrationEvent
            {
                Metadata = metadata with { EventType = RefundSucceededIntegrationEvent.EventTypeName, Version = 1 },
                ReturnRequestId = succeeded.ReturnRequestId,
                SellerOrderId = succeeded.SellerOrderId,
                PaymentId = succeeded.PaymentId,
                RefundAmount = succeeded.RefundAmount,
                Currency = succeeded.Currency,
            },
            _ => null,
        };

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        integrationEventType switch
        {
            _ when integrationEventType == typeof(ReturnRequestedIntegrationEvent) => ReturnRequestedIntegrationEvent.EventTypeName,
            _ when integrationEventType == typeof(ReturnApprovedIntegrationEvent) => ReturnApprovedIntegrationEvent.EventTypeName,
            _ when integrationEventType == typeof(RefundSucceededIntegrationEvent) => RefundSucceededIntegrationEvent.EventTypeName,
            _ => throw new InvalidOperationException("Unmapped Returns integration event type."),
        };

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) =>
        eventTypeName switch
        {
            ReturnRequestedIntegrationEvent.EventTypeName => typeof(ReturnRequestedIntegrationEvent),
            ReturnApprovedIntegrationEvent.EventTypeName => typeof(ReturnApprovedIntegrationEvent),
            RefundSucceededIntegrationEvent.EventTypeName => typeof(RefundSucceededIntegrationEvent),
            _ => null,
        };
}
