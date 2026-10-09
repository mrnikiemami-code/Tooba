using Tooba.BuildingBlocks;

namespace Tooba.Settlement.Contracts.Events;

/// <summary>
/// رویداد Outbox payout.succeeded.v1
/// </summary>
public sealed class PayoutSucceededIntegrationEvent : IIntegrationEvent
{
    /// <summary>نام قرارداد.</summary>
    public const string EventTypeName = "payout.succeeded.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>درخواست payout.</summary>
    public Guid PayoutRequestId { get; set; }

    /// <summary>حساب.</summary>
    public Guid SettlementAccountId { get; set; }

    /// <summary>فروشنده.</summary>
    public Guid SellerPartyId { get; set; }

    /// <summary>مبلغ.</summary>
    public decimal Amount { get; set; }

    /// <summary>ارز.</summary>
    public string Currency { get; set; } = string.Empty;
}
