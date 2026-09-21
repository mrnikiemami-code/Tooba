using Tooba.BuildingBlocks;
using Tooba.Settlement.Domain.ValueObjects;

namespace Tooba.Settlement.Domain.Events;

/// <summary>
/// Domain type.
/// </summary>
public sealed class SettlementEntryPostedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public SettlementEntryPostedDomainEvent(
        Guid entryId,
        Guid settlementAccountId,
        Guid sellerPartyId,
        EntryType entryType,
        decimal netAmount,
        string currency,
        string sourceType,
        Guid sourceId)
    {
        EntryId = entryId;
        SettlementAccountId = settlementAccountId;
        SellerPartyId = sellerPartyId;
        EntryType = entryType;
        NetAmount = netAmount;
        Currency = currency;
        SourceType = sourceType;
        SourceId = sourceId;
        Metadata = EventMetadataFactory.ForDomain("settlement.entry.posted.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه سطر.</summary>
    public Guid EntryId { get; }

    /// <summary>حساب.</summary>
    public Guid SettlementAccountId { get; }

    /// <summary>فروشنده.</summary>
    public Guid SellerPartyId { get; }

    /// <summary>نوع سطر.</summary>
    public EntryType EntryType { get; }

    /// <summary>مبلغ خالص.</summary>
    public decimal NetAmount { get; }

    /// <summary>ارز.</summary>
    public string Currency { get; }

    /// <summary>نوع منبع.</summary>
    public string SourceType { get; }

    /// <summary>شناسه منبع.</summary>
    public Guid SourceId { get; }
}
