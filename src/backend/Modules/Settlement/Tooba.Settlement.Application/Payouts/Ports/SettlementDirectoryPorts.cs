using Tooba.Settlement.Domain.ValueObjects;

namespace Tooba.Settlement.Application.Payouts.Ports;

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
/// فرمان درخواست payout (Application-internal command shape, not a boundary contract).
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
/// دروازه فقط‌خواندنی بازگردانی سفارش نسبت به تسویه/واریز فروشنده.
/// </summary>
public sealed record SellerOrderRestoreSettlementGate(
    Guid SellerOrderId,
    bool HasPostedAccrual,
    bool HasCompletedPayoutEffect);

/// <summary>
/// نگهبان use-case تسویه.
/// </summary>
public interface ISettlementUseCaseGuard
{
    /// <summary>اجازهٔ mutate را بررسی می‌کند.</summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// ارکستراسیون تسویه و payout.
/// </summary>
public interface ISettlementDirectory
{
    /// <summary>مانده فروشنده را برمی‌گرداند.</summary>
    Task<SettlementBalanceSnapshot?> GetBalanceAsync(Guid sellerPartyId, CancellationToken cancellationToken);

    /// <summary>سطرهای posted فروشنده را فهرست می‌کند.</summary>
    Task<IReadOnlyList<SettlementEntrySnapshot>> ListEntriesAsync(Guid sellerPartyId, CancellationToken cancellationToken);

    /// <summary>سطرهای posted مرتبط با سفارش‌های فروشنده را batch می‌خواند.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<SettlementEntrySnapshot>>> ListEntriesBySellerOrderIdsAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);

    /// <summary>دروازه فقط‌خواندنی بازگردانی نسبت به واریز تکمیل‌شده سهم فروشنده.</summary>
    Task<IReadOnlyDictionary<Guid, SellerOrderRestoreSettlementGate>> GetRestoreSettlementGatesAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);

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

    /// <summary>
    /// accrual پرداخت را وقتی هنوز واریز سهم فروشنده تکمیل نشده حذف می‌کند.
    /// مسیر برگشت تأیید واریز؛ لغو سفارش نباید این را صدا بزند.
    /// </summary>
    Task VoidUnpaidAccrualForPaymentAsync(
        Guid paymentId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// accrual پرداخت‌نشده را با سطر Debit خنثی می‌کند؛ Credit را پاک یا بازنویسی نمی‌کند.
    /// </summary>
    Task NeutralizeUnpaidAccrualForCancelAsync(
        Guid paymentId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// خنثی‌سازی لغو را با سطر Credit برمی‌گرداند؛ سطرهای قبلی پاک نمی‌شوند.
    /// </summary>
    Task ReinstateAccrualAfterCancelRestoreAsync(
        Guid paymentId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);
}
