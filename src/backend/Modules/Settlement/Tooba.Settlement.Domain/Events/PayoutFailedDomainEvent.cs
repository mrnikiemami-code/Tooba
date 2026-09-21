using Tooba.BuildingBlocks;

namespace Tooba.Settlement.Domain.Events;

/// <summary>
/// Domain type.
/// </summary>
public sealed class PayoutFailedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public PayoutFailedDomainEvent(
        Guid payoutRequestId,
        Guid settlementAccountId,
        Guid sellerPartyId,
        decimal amount,
        string currency,
        string failureCode)
    {
        PayoutRequestId = payoutRequestId;
        SettlementAccountId = settlementAccountId;
        SellerPartyId = sellerPartyId;
        Amount = amount;
        Currency = currency;
        FailureCode = failureCode;
        Metadata = EventMetadataFactory.ForDomain("payout.failed.v1");
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

    /// <summary>کد شکست.</summary>
    public string FailureCode { get; }
}
