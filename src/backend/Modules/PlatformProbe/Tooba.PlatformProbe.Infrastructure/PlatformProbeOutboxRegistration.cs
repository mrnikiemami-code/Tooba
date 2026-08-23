using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.PlatformProbe.Infrastructure.Events;
using Tooba.PlatformProbe.Infrastructure.Persistence;

namespace Tooba.PlatformProbe.Infrastructure;

/// <summary>
/// ثبت نمونهٔ disposable برای Outbox ماژول PlatformProbe. هستهٔ عمومی نام این ماژول را hard-code نمی‌کند.
/// </summary>
public sealed class PlatformProbeOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => PlatformProbeDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(PlatformProbeDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata)
    {
        if (domainEvent is not ProbeRecordCreatedDomainEvent created)
        {
            return null;
        }

        return new ProbeRecordCreatedIntegrationEvent
        {
            Metadata = metadata with
            {
                EventType = ProbeRecordCreatedIntegrationEvent.EventTypeName,
                Version = 1,
            },
            RecordId = created.RecordId,
        };
    }

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType)
    {
        if (integrationEventType == typeof(ProbeRecordCreatedIntegrationEvent))
        {
            return ProbeRecordCreatedIntegrationEvent.EventTypeName;
        }

        throw new InvalidOperationException("Unmapped PlatformProbe integration event type.");
    }

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) =>
        eventTypeName == ProbeRecordCreatedIntegrationEvent.EventTypeName
            ? typeof(ProbeRecordCreatedIntegrationEvent)
            : null;
}
