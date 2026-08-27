using Tooba.BuildingBlocks;

namespace Tooba.Settlement.Domain;

/// <summary>
/// نوع سطر دفتر تسویه. Credit یعنی بستانکار فروشنده؛ Debit یعنی بدهکار.
/// </summary>
public enum EntryType
{
    /// <summary>بستانکار فروشنده (مثلاً پس از پرداخت).</summary>
    Credit = 0,

    /// <summary>بدهکار فروشنده (مثلاً پس از refund).</summary>
    Debit = 1,
}

/// <summary>
/// وضعیت صورت‌حساب دوره‌ای. با PayoutStatus یکی نیست.
/// </summary>
public enum StatementStatus
{
    /// <summary>دوره باز و قابل جمع‌بندی.</summary>
    Open = 0,

    /// <summary>دوره بسته شده.</summary>
    Closed = 1,
}

/// <summary>
/// وضعیت درخواست/تلاش payout. با PaymentStatus یکی نیست.
/// </summary>
public enum PayoutStatus
{
    /// <summary>در انتظار پردازش.</summary>
    Pending = 0,

    /// <summary>در حال ارسال به درگاه.</summary>
    Processing = 1,

    /// <summary>موفق.</summary>
    Succeeded = 2,

    /// <summary>شکست.</summary>
    Failed = 3,
}

/// <summary>
/// سیاست کارمزد marketplace قابل پیکربندی. snapshot روی سطر ثبت می‌شود.
/// </summary>
public sealed class CommissionPolicy
{
    private CommissionPolicy()
    {
    }

    /// <summary>شناسه سیاست.</summary>
    public Guid PolicyId { get; init; }

    /// <summary>نام نمایشی.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>نرخ کارمزد (۰.۱۰ = ۱۰٪).</summary>
    public decimal Rate { get; init; }

    /// <summary>آیا پیش‌فرض marketplace است.</summary>
    public bool IsDefault { get; init; }

    /// <summary>زمان اعتبار.</summary>
    public DateTimeOffset EffectiveFrom { get; init; }

    /// <summary>سیاست پیش‌فرض ۱۰٪ marketplace را می‌سازد.</summary>
    public static CommissionPolicy CreateDefaultMarketplace(DateTimeOffset now) =>
        new()
        {
            PolicyId = Guid.Parse("00000000-0000-0000-0000-000000000010"),
            Name = "marketplace-default-10pct",
            Rate = 0.10m,
            IsDefault = true,
            EffectiveFrom = now,
        };
}

/// <summary>
/// snapshot immutable سیاست کارمزد روی سطر posted.
/// </summary>
public sealed class CommissionPolicySnapshot
{
    /// <summary>شناسه سیاست مرجع.</summary>
    public Guid PolicyId { get; init; }

    /// <summary>نام snapshot.</summary>
    public string PolicyName { get; init; } = string.Empty;

    /// <summary>نرخ snapshot.</summary>
    public decimal Rate { get; init; }

    /// <summary>snapshot را از سیاست فعال می‌سازد.</summary>
    public static CommissionPolicySnapshot FromPolicy(CommissionPolicy policy) =>
        new()
        {
            PolicyId = policy.PolicyId,
            PolicyName = policy.Name,
            Rate = policy.Rate,
        };
}

/// <summary>
/// حساب تسویهٔ یک فروشنده. FK Party نیست.
/// </summary>
public sealed class SettlementAccount
{
    private SettlementAccount()
    {
    }

    /// <summary>شناسه حساب.</summary>
    public Guid SettlementAccountId { get; init; }

    /// <summary>فروشنده.</summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>ارز حساب.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>حساب جدید برای فروشنده می‌سازد.</summary>
    public static SettlementAccount Create(Guid sellerPartyId, string currency, DateTimeOffset now) =>
        new()
        {
            SettlementAccountId = Guid.NewGuid(),
            SellerPartyId = sellerPartyId,
            Currency = currency.Trim(),
            CreatedAt = now,
        };
}

/// <summary>
/// سطر posted غیرقابل‌ویرایش دفتر تسویه.
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
            throw new InvalidOperationException("مبلغ ناخالص باید مثبت باشد.");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new InvalidOperationException("کلید idempotency الزامی است.");
        }

        var commission = decimal.Round(grossAmount * policySnapshot.Rate, 4, MidpointRounding.AwayFromZero);
        var net = grossAmount - commission;
        var entry = new SettlementEntry
        {
            EntryId = Guid.NewGuid(),
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
            throw new InvalidOperationException("مبلغ refund باید مثبت باشد.");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new InvalidOperationException("کلید idempotency الزامی است.");
        }

        var commission = decimal.Round(refundGrossAmount * policySnapshot.Rate, 4, MidpointRounding.AwayFromZero);
        var net = refundGrossAmount - commission;
        var entry = new SettlementEntry
        {
            EntryId = Guid.NewGuid(),
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
}

/// <summary>
/// صورت‌حساب دوره‌ای حساب تسویه.
/// </summary>
public sealed class SettlementStatement
{
    private SettlementStatement()
    {
    }

    /// <summary>شناسه صورت‌حساب.</summary>
    public Guid StatementId { get; init; }

    /// <summary>حساب مالک.</summary>
    public Guid SettlementAccountId { get; init; }

    /// <summary>وضعیت دوره.</summary>
    public StatementStatus Status { get; private set; }

    /// <summary>شروع دوره.</summary>
    public DateTimeOffset PeriodStart { get; init; }

    /// <summary>پایان دوره.</summary>
    public DateTimeOffset PeriodEnd { get; init; }

    /// <summary>مانده ابتدای دوره.</summary>
    public decimal OpeningBalance { get; init; }

    /// <summary>مانده انتهای دوره.</summary>
    public decimal ClosingBalance { get; private set; }

    /// <summary>ارز.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>صورت‌حساب باز می‌سازد.</summary>
    public static SettlementStatement Open(
        Guid settlementAccountId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        decimal openingBalance,
        string currency,
        DateTimeOffset now) =>
        new()
        {
            StatementId = Guid.NewGuid(),
            SettlementAccountId = settlementAccountId,
            Status = StatementStatus.Open,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            OpeningBalance = openingBalance,
            ClosingBalance = openingBalance,
            Currency = currency.Trim(),
            CreatedAt = now,
        };

    /// <summary>دوره را می‌بندد.</summary>
    public void Close(decimal closingBalance, DateTimeOffset now)
    {
        _ = now;
        Status = StatementStatus.Closed;
        ClosingBalance = closingBalance;
    }
}

/// <summary>
/// پروفایل payout فروشنده. FK Party نیست.
/// </summary>
public sealed class SellerPayoutProfile
{
    private SellerPayoutProfile()
    {
    }

    /// <summary>شناسه پروفایل.</summary>
    public Guid SellerPayoutProfileId { get; init; }

    /// <summary>فروشنده.</summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>شماره شبا/IBAN.</summary>
    public string? Iban { get; init; }

    /// <summary>نام صاحب حساب.</summary>
    public string? AccountHolderName { get; init; }

    /// <summary>آیا پروفایل تأیید شده.</summary>
    public bool IsVerified { get; init; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>پروفایل placeholder برای dev می‌سازد.</summary>
    public static SellerPayoutProfile CreateDevPlaceholder(Guid sellerPartyId, DateTimeOffset now) =>
        new()
        {
            SellerPayoutProfileId = Guid.NewGuid(),
            SellerPartyId = sellerPartyId,
            Iban = "IR000000000000000000000000",
            AccountHolderName = "dev-seller",
            IsVerified = true,
            CreatedAt = now,
        };
}

/// <summary>
/// تلاش payout نزد درگاه.
/// </summary>
public sealed class PayoutAttempt
{
    private PayoutAttempt()
    {
    }

    /// <summary>شناسه تلاش.</summary>
    public Guid PayoutAttemptId { get; init; }

    /// <summary>درخواست مالک.</summary>
    public Guid PayoutRequestId { get; init; }

    /// <summary>وضعیت تلاش.</summary>
    public PayoutStatus Status { get; private set; }

    /// <summary>کلید idempotency درگاه.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>مرجع درگاه.</summary>
    public string? ProviderReference { get; private set; }

    /// <summary>کد شکست.</summary>
    public string? FailureCode { get; private set; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان تکمیل.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    internal static PayoutAttempt CreatePending(Guid payoutRequestId, string idempotencyKey, DateTimeOffset now) =>
        new()
        {
            PayoutAttemptId = Guid.NewGuid(),
            PayoutRequestId = payoutRequestId,
            Status = PayoutStatus.Processing,
            IdempotencyKey = idempotencyKey.Trim(),
            CreatedAt = now,
        };

    /// <summary>تلاش را موفق علامت می‌زند.</summary>
    public void MarkSucceeded(string providerReference, DateTimeOffset now)
    {
        Status = PayoutStatus.Succeeded;
        ProviderReference = providerReference.Trim();
        CompletedAt = now;
    }

    /// <summary>تلاش را شکست‌خورده علامت می‌زند.</summary>
    public void MarkFailed(string failureCode, DateTimeOffset now)
    {
        Status = PayoutStatus.Failed;
        FailureCode = failureCode.Trim();
        CompletedAt = now;
    }
}

/// <summary>
/// درخواست payout فروشنده. aggregate root.
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
        Guid settlementAccountId,
        Guid sellerPartyId,
        decimal amount,
        string currency,
        string idempotencyKey,
        DateTimeOffset now)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("مبلغ payout باید مثبت باشد.");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new InvalidOperationException("کلید idempotency الزامی است.");
        }

        return new PayoutRequest
        {
            PayoutRequestId = Guid.NewGuid(),
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
    public PayoutAttempt BeginAttempt(string attemptIdempotencyKey, DateTimeOffset now)
    {
        if (Status is PayoutStatus.Succeeded)
        {
            throw new InvalidOperationException("payout قبلاً موفق شده است.");
        }

        Status = PayoutStatus.Processing;
        UpdatedAt = now;
        var attempt = PayoutAttempt.CreatePending(PayoutRequestId, attemptIdempotencyKey, now);
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

/// <summary>رویداد posting سطر تسویه.</summary>
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

/// <summary>رویداد موفقیت payout.</summary>
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

/// <summary>رویداد شکست payout.</summary>
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
