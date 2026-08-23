using System.Text.Json.Serialization;
using Tooba.BuildingBlocks;

namespace Tooba.PlatformProbe.Infrastructure.Events;

/// <summary>
/// واقعیت داخلی ایجاد ردیف probe. قرارداد خارجی نیست و به‌تنهایی منتشر نمی‌شود.
/// </summary>
public sealed class ProbeRecordCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد دامنه را با شناسهٔ ردیف probe می‌سازد.
    /// </summary>
    public ProbeRecordCreatedDomainEvent(Guid recordId)
    {
        RecordId = recordId;
        Metadata = EventMetadataFactory.ForDomain("platform_probe.record_created.domain");
    }

    /// <summary>
    /// کلید ردیف probe که ایجاد شده است.
    /// </summary>
    public Guid RecordId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// واقعیت داخلی بدون ترجمهٔ Integration؛ برای اثبات «هر Domain Event منتشر نمی‌شود».
/// </summary>
public sealed class ProbeInternalNoteDomainEvent : IDomainEvent
{
    /// <summary>
    /// یادداشت داخلی ماژول که نباید Outbox بسازد.
    /// </summary>
    public ProbeInternalNoteDomainEvent(string note)
    {
        Note = note;
        Metadata = EventMetadataFactory.ForDomain("platform_probe.internal_note.domain");
    }

    /// <summary>
    /// متن داخلی؛ نباید به Integration تبدیل شود.
    /// </summary>
    public string Note { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// قرارداد Integration نمونه برای PlatformProbe. disposable است و الگوی Catalog نیست.
/// </summary>
public sealed class ProbeRecordCreatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام type map پایدار این قرارداد.
    /// </summary>
    public const string EventTypeName = "platform_probe.record_created.v1";

    /// <summary>
    /// فراداده از ستون‌های Outbox پس از deserialize بازنویسی می‌شود.
    /// </summary>
    [JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// شناسهٔ ردیف probe در payload؛ منبع Tenant نیست.
    /// </summary>
    public Guid RecordId { get; set; }
}
