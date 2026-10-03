using Tooba.BuildingBlocks;
using Tooba.Persistence;

namespace Tooba.Host.Tests.Fixtures.PlatformProbe;

/// <summary>ثبت Outbox تست‌محور با همان type map پایدار production probe.</summary>
public sealed class TestPlatformProbeOutboxRegistration : IOutboxModuleRegistration
{
    public string Schema => TestPlatformProbeDbContext.Schema;

    public string TableName => OutboxMessageMapping.TableName;

    public Type DbContextType => typeof(TestPlatformProbeDbContext);

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

    public string GetEventTypeName(Type integrationEventType)
    {
        if (integrationEventType == typeof(ProbeRecordCreatedIntegrationEvent))
        {
            return ProbeRecordCreatedIntegrationEvent.EventTypeName;
        }

        throw new InvalidOperationException("Unmapped test PlatformProbe integration event type.");
    }

    public Type? ResolveEventClrType(string eventTypeName) =>
        eventTypeName == ProbeRecordCreatedIntegrationEvent.EventTypeName
            ? typeof(ProbeRecordCreatedIntegrationEvent)
            : null;
}
