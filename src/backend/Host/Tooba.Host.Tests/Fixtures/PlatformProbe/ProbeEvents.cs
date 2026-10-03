using System.Text.Json.Serialization;
using Tooba.BuildingBlocks;

namespace Tooba.Host.Tests.Fixtures.PlatformProbe;

/// <summary>واقعیت داخلی ایجاد ردیف probe برای تست foundation؛ قرارداد production نیست.</summary>
public sealed class ProbeRecordCreatedDomainEvent : IDomainEvent
{
    public ProbeRecordCreatedDomainEvent(Guid recordId)
    {
        RecordId = recordId;
        Metadata = EventMetadataFactory.ForDomain("platform_probe.record_created.domain");
    }

    public Guid RecordId { get; }

    public EventMetadata Metadata { get; }
}

/// <summary>یادداشت داخلی بدون ترجمهٔ Integration برای تست.</summary>
public sealed class ProbeInternalNoteDomainEvent : IDomainEvent
{
    public ProbeInternalNoteDomainEvent(string note)
    {
        Note = note;
        Metadata = EventMetadataFactory.ForDomain("platform_probe.internal_note.domain");
    }

    public string Note { get; }

    public EventMetadata Metadata { get; }
}

/// <summary>قرارداد Integration نمونه برای تست Outbox/messaging؛ type map پایدار حفظ می‌شود.</summary>
public sealed class ProbeRecordCreatedIntegrationEvent : IIntegrationEvent
{
    public const string EventTypeName = "platform_probe.record_created.v1";

    [JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    public Guid RecordId { get; set; }
}
