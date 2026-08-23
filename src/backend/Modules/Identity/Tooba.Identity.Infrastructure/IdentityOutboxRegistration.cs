using Tooba.BuildingBlocks;
using Tooba.Identity.Domain;
using Tooba.Identity.Infrastructure.Events;
using Tooba.Identity.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Identity.Infrastructure;

/// <summary>
/// ثبت Outbox ماژول Identity. فقط قرارداد صریح ثبت User را ترجمه می‌کند.
/// </summary>
public sealed class IdentityOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => IdentityDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(IdentityDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata)
    {
        if (domainEvent is not UserRegisteredDomainEvent registered)
        {
            return null;
        }

        return new UserRegisteredIntegrationEvent
        {
            Metadata = metadata with
            {
                EventType = UserRegisteredIntegrationEvent.EventTypeName,
                Version = 1,
            },
            UserId = registered.UserId,
        };
    }

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType)
    {
        if (integrationEventType == typeof(UserRegisteredIntegrationEvent))
        {
            return UserRegisteredIntegrationEvent.EventTypeName;
        }

        throw new InvalidOperationException("Unmapped Identity integration event type.");
    }

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) =>
        eventTypeName == UserRegisteredIntegrationEvent.EventTypeName
            ? typeof(UserRegisteredIntegrationEvent)
            : null;
}
