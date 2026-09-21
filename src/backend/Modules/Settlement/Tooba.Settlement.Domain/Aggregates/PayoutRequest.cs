using Tooba.BuildingBlocks;
using Tooba.Settlement.Domain.ValueObjects;
using Tooba.Settlement.Domain.Events;

namespace Tooba.Settlement.Domain.Aggregates;

/// <summary>
/// Domain type.
/// </summary>
public sealed class PayoutRequest : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();
    private readonly List<PayoutAttempt> _attempts = [];

    private PayoutRequest()
    {
    }

    /// <summary>شناسه درخواست.</summary>
    public Guid PayoutRequestId { get; init; }

    /// <summary>حساب مالک.</summary>
    public Guid SettlementAccountId { get; init; }

    /// <summary>فروشنده.</summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>مبلغ درخواستی.</summary>
    public decimal Amount { get; init; }

    /// <summary>ارز.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>وضعیت درخواست.</summary>
    public PayoutStatus Status { get; private set; }

    /// <summary>کلید idempotency ایجاد.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>آخرین به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>تلاش‌های payout.</summary>
    public IReadOnlyList<PayoutAttempt> Attempts => _attempts;

    /// <summary>رویدادهای دامنه.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>درخواست payout جدید می‌سازد.</summary>
    public static PayoutRequest Create(
        Guid id,
        Guid settlementAccountId,
        Guid sellerPartyId,
        decimal amount,
        string currency,
        string idempotencyKey,
        DateTimeOffset now)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("settlement.amount.invalid");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new InvalidOperationException("settlement.idempotency.required");
        }

        return new PayoutRequest
        {
            PayoutRequestId = id,
            SettlementAccountId = settlementAccountId,
            SellerPartyId = sellerPartyId,
            Amount = amount,
            Currency = currency.Trim(),
            Status = PayoutStatus.Pending,
            IdempotencyKey = idempotencyKey.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>تلاش‌های بارگذاری‌شده را وصل می‌کند.</summary>
    public void AttachLoadedAttempts(IEnumerable<PayoutAttempt> attempts)
    {
        _attempts.Clear();
        _attempts.AddRange(attempts);
    }

    /// <summary>به Processing می‌رود و تلاش جدید ثبت می‌کند.</summary>
    public PayoutAttempt BeginAttempt(Guid attemptId, string attemptIdempotencyKey, DateTimeOffset now)
    {
        if (Status is PayoutStatus.Succeeded)
        {
            throw new InvalidOperationException("settlement.payout.invalid_state");
        }

        Status = PayoutStatus.Processing;
        UpdatedAt = now;
        var attempt = PayoutAttempt.CreatePending(attemptId, PayoutRequestId, attemptIdempotencyKey, now);
        _attempts.Add(attempt);
        return attempt;
    }

    /// <summary>درخواست را موفق علامت می‌زند.</summary>
    public void MarkSucceeded(Guid attemptId, string providerReference, DateTimeOffset now)
    {
        var attempt = _attempts.Single(x => x.PayoutAttemptId == attemptId);
        attempt.MarkSucceeded(providerReference, now);
        Status = PayoutStatus.Succeeded;
        UpdatedAt = now;
        _domainEvents.Add(new PayoutSucceededDomainEvent(PayoutRequestId, SettlementAccountId, SellerPartyId, Amount, Currency));
    }

    /// <summary>درخواست را شکست‌خورده علامت می‌زند.</summary>
    public void MarkFailed(Guid attemptId, string failureCode, DateTimeOffset now)
    {
        var attempt = _attempts.Single(x => x.PayoutAttemptId == attemptId);
        attempt.MarkFailed(failureCode, now);
        Status = PayoutStatus.Failed;
        UpdatedAt = now;
        _domainEvents.Add(new PayoutFailedDomainEvent(PayoutRequestId, SettlementAccountId, SellerPartyId, Amount, Currency, failureCode));
    }
}
