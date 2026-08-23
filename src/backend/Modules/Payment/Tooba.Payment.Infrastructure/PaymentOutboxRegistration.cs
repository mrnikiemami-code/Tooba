using Tooba.BuildingBlocks;
using Tooba.Payment.Application;
using Tooba.Payment.Domain;
using Tooba.Payment.Infrastructure.Events;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Payment.Infrastructure;

/// <summary>
/// ثبت Outbox ماژول Payment. سفارش را مستقیم به‌روز نمی‌کند؛ ترجمه فقط Integration پایدار است.
/// </summary>
public sealed class PaymentOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => PaymentDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(PaymentDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata)
    {
        return domainEvent switch
        {
            PaymentCreatedDomainEvent created => new PaymentCreatedIntegrationEvent
            {
                Metadata = metadata with { EventType = PaymentCreatedIntegrationEvent.EventTypeName, Version = 1 },
                PaymentId = created.PaymentId,
                CheckoutId = created.CheckoutId,
            },
            PaymentInitiatedDomainEvent initiated => new PaymentInitiatedIntegrationEvent
            {
                Metadata = metadata with { EventType = PaymentInitiatedIntegrationEvent.EventTypeName, Version = 1 },
                PaymentId = initiated.PaymentId,
                AttemptId = initiated.AttemptId,
            },
            PaymentSucceededDomainEvent succeeded => new PaymentSucceededIntegrationEvent
            {
                Metadata = metadata with { EventType = PaymentSucceededIntegrationEvent.EventTypeName, Version = 1 },
                PaymentId = succeeded.PaymentId,
                CheckoutId = succeeded.CheckoutId,
                Amount = succeeded.Amount,
                Currency = succeeded.Currency,
                ProviderTransactionReference = succeeded.ProviderTransactionReference,
                SellerOrderIds = succeeded.SellerOrderIds.ToArray(),
            },
            PaymentFailedDomainEvent failed => new PaymentFailedIntegrationEvent
            {
                Metadata = metadata with { EventType = PaymentFailedIntegrationEvent.EventTypeName, Version = 1 },
                PaymentId = failed.PaymentId,
                CheckoutId = failed.CheckoutId,
                FailureCode = failed.FailureCode,
            },
            _ => null,
        };
    }

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType)
    {
        if (integrationEventType == typeof(PaymentCreatedIntegrationEvent))
        {
            return PaymentCreatedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(PaymentInitiatedIntegrationEvent))
        {
            return PaymentInitiatedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(PaymentSucceededIntegrationEvent))
        {
            return PaymentSucceededIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(PaymentFailedIntegrationEvent))
        {
            return PaymentFailedIntegrationEvent.EventTypeName;
        }

        throw new InvalidOperationException("Unmapped Payment integration event type.");
    }

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) =>
        eventTypeName switch
        {
            PaymentCreatedIntegrationEvent.EventTypeName => typeof(PaymentCreatedIntegrationEvent),
            PaymentInitiatedIntegrationEvent.EventTypeName => typeof(PaymentInitiatedIntegrationEvent),
            PaymentSucceededIntegrationEvent.EventTypeName => typeof(PaymentSucceededIntegrationEvent),
            PaymentFailedIntegrationEvent.EventTypeName => typeof(PaymentFailedIntegrationEvent),
            _ => null,
        };
}
