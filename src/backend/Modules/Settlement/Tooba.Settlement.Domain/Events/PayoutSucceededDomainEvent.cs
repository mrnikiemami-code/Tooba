using Tooba.BuildingBlocks;

namespace Tooba.Settlement.Domain.Events;

/// <summary>
/// Domain type.
/// </summary>
public sealed class PayoutSucceededDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public PayoutSucceededDomainEvent(
        Guid payoutRequestId,
        Guid settlementAccountId,
        Guid sellerPartyId,
        decimal amount,
        string currency)
    {
        PayoutRequestId = payoutRequestId;
        SettlementAccountId = settlementAccountId;
        SellerPartyId = sellerPartyId;
        Amount = amount;
        Currency = currency;
        Metadata = EventMetadataFactory.ForDomain("payout.succeeded.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>درخواست payout.</summary>
    public Guid PayoutRequestId { get; }

    /// <summary>حساب.</summary>
    public Guid SettlementAccountId { get; }

    /// <summary>فروشنده.</summary>
    public Guid SellerPartyId { get; }

    /// <summary>مبلغ.</summary>
    public decimal Amount { get; }

    /// <summary>ارز.</summary>
    public string Currency { get; }
}
