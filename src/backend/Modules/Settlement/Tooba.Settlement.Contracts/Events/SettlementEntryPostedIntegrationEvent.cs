using Tooba.BuildingBlocks;

namespace Tooba.Settlement.Contracts.Events;

/// <summary>
/// رویداد Outbox settlement.entry.posted.v1
/// </summary>
public sealed class SettlementEntryPostedIntegrationEvent : IIntegrationEvent
{
    /// <summary>نام قرارداد.</summary>
    public const string EventTypeName = "settlement.entry.posted.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>شناسه سطر.</summary>
    public Guid EntryId { get; set; }

    /// <summary>حساب.</summary>
    public Guid SettlementAccountId { get; set; }

    /// <summary>فروشنده.</summary>
    public Guid SellerPartyId { get; set; }

    /// <summary>
    /// نوع سطر (<c>0</c> = Credit، <c>1</c> = Debit). عمداً عدد صحیح است تا قرارداد سیمی ماژول به
    /// enum داخلی Domain وابسته نشود و Settlement.Contracts تنها مرز قابل استخراج باقی بماند.
    /// </summary>
    public int EntryType { get; set; }

    /// <summary>مبلغ خالص.</summary>
    public decimal NetAmount { get; set; }

    /// <summary>ارز.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>نوع منبع.</summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>شناسه منبع.</summary>
    public Guid SourceId { get; set; }
}
