using Tooba.BuildingBlocks;
using Tooba.Settlement.Domain.ValueObjects;
using Tooba.Settlement.Domain.Events;

namespace Tooba.Settlement.Domain.Aggregates;

/// <summary>
/// Domain type.
/// </summary>
public sealed class SettlementEntry : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    private SettlementEntry()
    {
    }

    /// <summary>شناسه سطر.</summary>
    public Guid EntryId { get; init; }

    /// <summary>حساب مالک.</summary>
    public Guid SettlementAccountId { get; init; }

    /// <summary>فروشنده snapshot.</summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>نوع سطر.</summary>
    public EntryType EntryType { get; init; }

    /// <summary>مبلغ ناخالص مرجع.</summary>
    public decimal GrossAmount { get; init; }

    /// <summary>کارمزد marketplace.</summary>
    public decimal CommissionAmount { get; init; }

    /// <summary>مبلغ خالص posted.</summary>
    public decimal NetAmount { get; init; }

    /// <summary>ارز.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>snapshot سیاست کارمزد.</summary>
    public CommissionPolicySnapshot CommissionPolicySnapshot { get; init; } = null!;

    /// <summary>نوع منبع (payment/refund).</summary>
    public string SourceType { get; init; } = string.Empty;

    /// <summary>شناسه منبع بدون FK.</summary>
    public Guid SourceId { get; init; }

    /// <summary>سفارش فروشنده مرجع در صورت وجود.</summary>
    public Guid? SellerOrderId { get; init; }

    /// <summary>کلید idempotency posting.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>زمان posting.</summary>
    public DateTimeOffset PostedAt { get; init; }

    /// <summary>رویدادهای دامنه.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>سطر Credit از پرداخت می‌سازد.</summary>
    public static SettlementEntry PostCreditFromPayment(
        Guid id,
        Guid settlementAccountId,
        Guid sellerPartyId,
        Guid paymentId,
        Guid sellerOrderId,
        decimal grossAmount,
        string currency,
        CommissionPolicySnapshot policySnapshot,
        string idempotencyKey,
        DateTimeOffset now)
    {
        if (grossAmount <= 0)
        {
            throw new ContractOperationException("settlement.amount.invalid");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ContractOperationException("settlement.idempotency.required");
        }

        var commission = decimal.Round(grossAmount * policySnapshot.Rate, 4, MidpointRounding.AwayFromZero);
        var net = grossAmount - commission;
        var entry = new SettlementEntry
        {
            EntryId = id,
            SettlementAccountId = settlementAccountId,
            SellerPartyId = sellerPartyId,
            EntryType = EntryType.Credit,
            GrossAmount = grossAmount,
            CommissionAmount = commission,
            NetAmount = net,
            Currency = currency.Trim(),
            CommissionPolicySnapshot = policySnapshot,
            SourceType = "payment",
            SourceId = paymentId,
            SellerOrderId = sellerOrderId,
            IdempotencyKey = idempotencyKey.Trim(),
            PostedAt = now,
        };
        entry._domainEvents.Add(new SettlementEntryPostedDomainEvent(
            entry.EntryId,
            entry.SettlementAccountId,
            entry.SellerPartyId,
            entry.EntryType,
            entry.NetAmount,
            entry.Currency,
            entry.SourceType,
            entry.SourceId));
        return entry;
    }

    /// <summary>سطر Debit از refund می‌سازد.</summary>
    public static SettlementEntry PostDebitFromRefund(
        Guid id,
        Guid settlementAccountId,
        Guid sellerPartyId,
        Guid returnRequestId,
        Guid sellerOrderId,
        decimal refundGrossAmount,
        string currency,
        CommissionPolicySnapshot policySnapshot,
        string idempotencyKey,
        DateTimeOffset now)
    {
        if (refundGrossAmount <= 0)
        {
            throw new ContractOperationException("settlement.amount.invalid");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ContractOperationException("settlement.idempotency.required");
        }

        var commission = decimal.Round(refundGrossAmount * policySnapshot.Rate, 4, MidpointRounding.AwayFromZero);
        var net = refundGrossAmount - commission;
        var entry = new SettlementEntry
        {
            EntryId = id,
            SettlementAccountId = settlementAccountId,
            SellerPartyId = sellerPartyId,
            EntryType = EntryType.Debit,
            GrossAmount = refundGrossAmount,
            CommissionAmount = commission,
            NetAmount = net,
            Currency = currency.Trim(),
            CommissionPolicySnapshot = policySnapshot,
            SourceType = "refund",
            SourceId = returnRequestId,
            SellerOrderId = sellerOrderId,
            IdempotencyKey = idempotencyKey.Trim(),
            PostedAt = now,
        };
        entry._domainEvents.Add(new SettlementEntryPostedDomainEvent(
            entry.EntryId,
            entry.SettlementAccountId,
            entry.SellerPartyId,
            entry.EntryType,
            entry.NetAmount,
            entry.Currency,
            entry.SourceType,
            entry.SourceId));
        return entry;
    }

    /// <summary>سطر Debit برای خنثی‌سازی accrual پس از لغو سفارش؛ Credit را پاک نمی‌کند.</summary>
    public static SettlementEntry PostDebitFromOrderCancel(
        Guid id,
        Guid settlementAccountId,
        Guid sellerPartyId,
        Guid paymentId,
        Guid sellerOrderId,
        decimal grossAmount,
        string currency,
        CommissionPolicySnapshot policySnapshot,
        string idempotencyKey,
        DateTimeOffset now)
    {
        if (grossAmount <= 0)
        {
            throw new ContractOperationException("settlement.amount.invalid");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ContractOperationException("settlement.idempotency.required");
        }

        var commission = decimal.Round(grossAmount * policySnapshot.Rate, 4, MidpointRounding.AwayFromZero);
        var net = grossAmount - commission;
        var entry = new SettlementEntry
        {
            EntryId = id,
            SettlementAccountId = settlementAccountId,
            SellerPartyId = sellerPartyId,
            EntryType = EntryType.Debit,
            GrossAmount = grossAmount,
            CommissionAmount = commission,
            NetAmount = net,
            Currency = currency.Trim(),
            CommissionPolicySnapshot = policySnapshot,
            SourceType = "order_cancel",
            SourceId = paymentId,
            SellerOrderId = sellerOrderId,
            IdempotencyKey = idempotencyKey.Trim(),
            PostedAt = now,
        };
        entry._domainEvents.Add(new SettlementEntryPostedDomainEvent(
            entry.EntryId,
            entry.SettlementAccountId,
            entry.SellerPartyId,
            entry.EntryType,
            entry.NetAmount,
            entry.Currency,
            entry.SourceType,
            entry.SourceId));
        return entry;
    }

    /// <summary>سطر Credit برای بازگرداندن accrual پس از Restore لغو؛ Debit خنثی‌سازی پاک نمی‌شود.</summary>
    public static SettlementEntry PostCreditFromOrderRestore(
        Guid id,
        Guid settlementAccountId,
        Guid sellerPartyId,
        Guid paymentId,
        Guid sellerOrderId,
        decimal grossAmount,
        string currency,
        CommissionPolicySnapshot policySnapshot,
        string idempotencyKey,
        DateTimeOffset now)
    {
        if (grossAmount <= 0)
        {
            throw new ContractOperationException("settlement.amount.invalid");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ContractOperationException("settlement.idempotency.required");
        }

        var commission = decimal.Round(grossAmount * policySnapshot.Rate, 4, MidpointRounding.AwayFromZero);
        var net = grossAmount - commission;
        var entry = new SettlementEntry
        {
            EntryId = id,
            SettlementAccountId = settlementAccountId,
            SellerPartyId = sellerPartyId,
            EntryType = EntryType.Credit,
            GrossAmount = grossAmount,
            CommissionAmount = commission,
            NetAmount = net,
            Currency = currency.Trim(),
            CommissionPolicySnapshot = policySnapshot,
            SourceType = "order_restore",
            SourceId = paymentId,
            SellerOrderId = sellerOrderId,
            IdempotencyKey = idempotencyKey.Trim(),
            PostedAt = now,
        };
        entry._domainEvents.Add(new SettlementEntryPostedDomainEvent(
            entry.EntryId,
            entry.SettlementAccountId,
            entry.SellerPartyId,
            entry.EntryType,
            entry.NetAmount,
            entry.Currency,
            entry.SourceType,
            entry.SourceId));
        return entry;
    }
}
