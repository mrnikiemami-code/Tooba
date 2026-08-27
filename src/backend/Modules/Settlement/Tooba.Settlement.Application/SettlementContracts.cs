using Tooba.BuildingBlocks;
using Tooba.Settlement.Domain;

namespace Tooba.Settlement.Application;

/// <summary>
/// snapshot سفارش برای تسویه. FK Order نیست.
/// </summary>
public sealed record SettlementOrderSnapshot(
    Guid SellerOrderId,
    Guid CheckoutId,
    Guid SellerPartyId,
    bool IsPaid,
    string Currency);

/// <summary>
/// snapshot تخصیص پرداخت برای accrual.
/// </summary>
public sealed record SettlementPaymentAllocationSnapshot(
    Guid SellerOrderId,
    decimal AllocatedAmount,
    string Currency);

/// <summary>
/// snapshot پرداخت برای تسویه.
/// </summary>
public sealed record SettlementPaymentSnapshot(
    Guid PaymentId,
    Guid CheckoutId,
    decimal Amount,
    string Currency,
    bool IsSucceeded);

/// <summary>
/// snapshot refund برای adjustment.
/// </summary>
public sealed record SettlementRefundSnapshot(
    Guid ReturnRequestId,
    Guid SellerOrderId,
    Guid SellerPartyId,
    decimal RefundAmount,
    string Currency);

/// <summary>
/// خواندن snapshot سفارش بدون DbContext Order.
/// </summary>
public interface ISettlementOrderReader
{
    /// <summary>snapshot سفارش را برمی‌گرداند.</summary>
    Task<SettlementOrderSnapshot?> GetAsync(Guid sellerOrderId, CancellationToken cancellationToken);
}

/// <summary>
/// خواندن snapshot پرداخت بدون DbContext Payment.
/// </summary>
public interface ISettlementPaymentReader
{
    /// <summary>snapshot پرداخت را برمی‌گرداند.</summary>
    Task<SettlementPaymentSnapshot?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken);

    /// <summary>تخصیص‌های پرداخت را برمی‌گرداند.</summary>
    Task<IReadOnlyList<SettlementPaymentAllocationSnapshot>> GetAllocationsAsync(
        Guid paymentId,
        CancellationToken cancellationToken);
}

/// <summary>
/// خواندن snapshot refund بدون DbContext Returns.
/// </summary>
public interface ISettlementReturnsReader
{
    /// <summary>snapshot refund را برمی‌گرداند.</summary>
    Task<SettlementRefundSnapshot?> GetAsync(Guid returnRequestId, CancellationToken cancellationToken);
}

/// <summary>
/// نتیجهٔ payout از درگاه.
/// </summary>
public sealed record GatewayPayoutResult(bool Succeeded, string? ProviderReference, string? FailureCode);

/// <summary>
/// قرارداد درگاه payout. PSP واقعی اینجا نیست.
/// </summary>
public interface IPayoutGateway
{
    /// <summary>کد پایدار درگاه.</summary>
    string ProviderCode { get; }

    /// <summary>payout را با idempotency نزد درگاه اجرا می‌کند.</summary>
    Task<GatewayPayoutResult> PayoutAsync(
        Guid payoutRequestId,
        Guid sellerPartyId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken);
}

/// <summary>
/// snapshot مانده حساب تسویه.
/// </summary>
public sealed record SettlementBalanceSnapshot(
    Guid SettlementAccountId,
    Guid SellerPartyId,
    string Currency,
    decimal PostedCredits,
    decimal PostedDebits,
    decimal ReservedPayouts,
    decimal AvailableBalance);

/// <summary>
/// snapshot سطر posted.
/// </summary>
public sealed record SettlementEntrySnapshot(
    Guid EntryId,
    Guid SettlementAccountId,
    Guid SellerPartyId,
    EntryType EntryType,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal NetAmount,
    string Currency,
    CommissionPolicySnapshot CommissionPolicySnapshot,
    string SourceType,
    Guid SourceId,
    Guid? SellerOrderId,
    DateTimeOffset PostedAt);

/// <summary>
/// snapshot صورت‌حساب.
/// </summary>
public sealed record SettlementStatementSnapshot(
    Guid StatementId,
    Guid SettlementAccountId,
    StatementStatus Status,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    decimal OpeningBalance,
    decimal ClosingBalance,
    string Currency,
    DateTimeOffset CreatedAt);

/// <summary>
/// snapshot تلاش payout.
/// </summary>
public sealed record PayoutAttemptSnapshot(
    Guid PayoutAttemptId,
    Guid PayoutRequestId,
    PayoutStatus Status,
    string IdempotencyKey,
    string? ProviderReference,
    string? FailureCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

/// <summary>
/// snapshot درخواست payout.
/// </summary>
public sealed record PayoutRequestSnapshot(
    Guid PayoutRequestId,
    Guid SettlementAccountId,
    Guid SellerPartyId,
    decimal Amount,
    string Currency,
    PayoutStatus Status,
    string IdempotencyKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<PayoutAttemptSnapshot> Attempts);

/// <summary>
/// فرمان درخواست payout.
/// </summary>
public sealed record RequestPayoutCommand(
    Guid SellerPartyId,
    decimal Amount,
    string IdempotencyKey,
    Guid ActorUserId);

/// <summary>
/// فرمان پردازش payout (admin/dev).
/// </summary>
public sealed record ProcessPayoutCommand(Guid PayoutRequestId, Guid ActorUserId);

/// <summary>
/// فرمان retry payout (admin/dev).
/// </summary>
public sealed record RetryPayoutCommand(Guid PayoutRequestId, Guid ActorUserId);

/// <summary>
/// ارکستراسیون تسویه و payout.
/// </summary>
public interface ISettlementDirectory
{
    /// <summary>مانده فروشنده را برمی‌گرداند.</summary>
    Task<SettlementBalanceSnapshot?> GetBalanceAsync(Guid sellerPartyId, CancellationToken cancellationToken);

    /// <summary>سطرهای posted فروشنده را فهرست می‌کند.</summary>
    Task<IReadOnlyList<SettlementEntrySnapshot>> ListEntriesAsync(Guid sellerPartyId, CancellationToken cancellationToken);

    /// <summary>صورت‌حساب‌های فروشنده را فهرست می‌کند.</summary>
    Task<IReadOnlyList<SettlementStatementSnapshot>> ListStatementsAsync(Guid sellerPartyId, CancellationToken cancellationToken);

    /// <summary>درخواست payout می‌سازد.</summary>
    Task<PayoutRequestSnapshot> RequestPayoutAsync(RequestPayoutCommand command, CancellationToken cancellationToken);

    /// <summary>درخواست payout را می‌خواند.</summary>
    Task<PayoutRequestSnapshot?> GetPayoutRequestAsync(Guid payoutRequestId, CancellationToken cancellationToken);

    /// <summary>فهرست payoutهای فروشنده.</summary>
    Task<IReadOnlyList<PayoutRequestSnapshot>> ListPayoutRequestsForSellerAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken);

    /// <summary>مانده همه فروشندگان (admin).</summary>
    Task<IReadOnlyList<SettlementBalanceSnapshot>> ListAllBalancesAsync(CancellationToken cancellationToken);

    /// <summary>صف payout (admin).</summary>
    Task<IReadOnlyList<PayoutRequestSnapshot>> ListPayoutQueueAsync(CancellationToken cancellationToken);

    /// <summary>payout را پردازش می‌کند (admin/dev).</summary>
    Task<PayoutRequestSnapshot> ProcessPayoutAsync(ProcessPayoutCommand command, CancellationToken cancellationToken);

    /// <summary>payout شکست‌خورده را retry می‌کند (admin/dev).</summary>
    Task<PayoutRequestSnapshot> RetryPayoutAsync(RetryPayoutCommand command, CancellationToken cancellationToken);

    /// <summary>accrual idempotent از payment.succeeded.</summary>
    Task AccrueFromPaymentAsync(
        Guid paymentId,
        Guid eventId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);

    /// <summary>adjustment idempotent از refund.succeeded.</summary>
    Task AdjustFromRefundAsync(
        Guid returnRequestId,
        decimal refundAmount,
        string currency,
        Guid eventId,
        CancellationToken cancellationToken);
}

/// <summary>
/// نگهبان use-case تسویه.
/// </summary>
public interface ISettlementUseCaseGuard
{
    /// <summary>اجازهٔ mutate را بررسی می‌کند.</summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}

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

    /// <summary>نوع سطر.</summary>
    public EntryType EntryType { get; set; }

    /// <summary>مبلغ خالص.</summary>
    public decimal NetAmount { get; set; }

    /// <summary>ارز.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>نوع منبع.</summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>شناسه منبع.</summary>
    public Guid SourceId { get; set; }
}

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

/// <summary>
/// رویداد Outbox payout.failed.v1
/// </summary>
public sealed class PayoutFailedIntegrationEvent : IIntegrationEvent
{
    /// <summary>نام قرارداد.</summary>
    public const string EventTypeName = "payout.failed.v1";

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

    /// <summary>کد شکست.</summary>
    public string FailureCode { get; set; } = string.Empty;
}
