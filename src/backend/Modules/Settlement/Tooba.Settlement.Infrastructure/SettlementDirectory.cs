using Microsoft.EntityFrameworkCore;
using Tooba.Settlement.Application;
using Tooba.Settlement.Domain;
using Tooba.Settlement.Infrastructure.Persistence;

namespace Tooba.Settlement.Infrastructure;

/// <summary>
/// نگهبان باز موردکاربرد Settlement.
/// </summary>
public sealed class OpenSettlementUseCaseGuard : ISettlementUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// ارکستراسیون تسویه و payout در schema settlement.
/// </summary>
public sealed class SettlementDirectory : ISettlementDirectory
{
    private readonly SettlementDbContext _db;
    private readonly ISettlementUseCaseGuard _guard;
    private readonly ISettlementOrderReader _orders;
    private readonly ISettlementPaymentReader _payments;
    private readonly ISettlementReturnsReader _returns;
    private readonly IPayoutGateway _payoutGateway;
    private readonly SettlementInstrumentation _telemetry;

    /// <summary>
    /// دایرکتوری را به schema settlement و درز Order/Payment/Returns وصل می‌کند.
    /// </summary>
    public SettlementDirectory(
        SettlementDbContext db,
        ISettlementUseCaseGuard guard,
        ISettlementOrderReader orders,
        ISettlementPaymentReader payments,
        ISettlementReturnsReader returns,
        IPayoutGateway payoutGateway,
        SettlementInstrumentation telemetry)
    {
        _db = db;
        _guard = guard;
        _orders = orders;
        _payments = payments;
        _returns = returns;
        _payoutGateway = payoutGateway;
        _telemetry = telemetry;
    }

    /// <inheritdoc />
    public async Task AccrueFromPaymentAsync(
        Guid paymentId,
        Guid eventId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        if (await _db.PaymentInbox.AnyAsync(x => x.EventId == eventId, cancellationToken))
        {
            return;
        }

        var payment = await _payments.GetPaymentAsync(paymentId, cancellationToken)
            ?? throw new InvalidOperationException("پرداخت برای accrual پیدا نشد.");
        if (!payment.IsSucceeded)
        {
            throw new InvalidOperationException("accrual فقط برای پرداخت Succeeded مجاز است.");
        }

        var policy = await GetDefaultCommissionPolicyAsync(cancellationToken);
        var policySnapshot = CommissionPolicySnapshot.FromPolicy(policy);
        var allocations = await _payments.GetAllocationsAsync(paymentId, cancellationToken);
        var allocationBySellerOrder = allocations.ToDictionary(x => x.SellerOrderId);
        var now = DateTimeOffset.UtcNow;

        foreach (var sellerOrderId in sellerOrderIds.Distinct())
        {
            var idempotencyKey = $"payment-accrual:{paymentId:N}:{sellerOrderId:N}";
            if (await _db.SettlementEntries.AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken))
            {
                continue;
            }

            if (!allocationBySellerOrder.TryGetValue(sellerOrderId, out var allocation))
            {
                continue;
            }

            var order = await _orders.GetAsync(sellerOrderId, cancellationToken)
                ?? throw new InvalidOperationException("سفارش برای accrual پیدا نشد.");
            if (!order.IsPaid)
            {
                throw new InvalidOperationException("accrual فقط برای سفارش Paid مجاز است.");
            }

            var account = await EnsureAccountAsync(order.SellerPartyId, allocation.Currency, now, cancellationToken);
            var entry = SettlementEntry.PostCreditFromPayment(
                account.SettlementAccountId,
                order.SellerPartyId,
                paymentId,
                sellerOrderId,
                allocation.AllocatedAmount,
                allocation.Currency,
                policySnapshot,
                idempotencyKey,
                now);
            _db.SettlementEntries.Add(entry);
            _telemetry.RecordEntryPosted();
        }

        _db.PaymentInbox.Add(new SettlementPaymentInboxRecord
        {
            EventId = eventId,
            PaymentId = paymentId,
            ProcessedAt = now,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AdjustFromRefundAsync(
        Guid returnRequestId,
        decimal refundAmount,
        string currency,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (await _db.RefundInbox.AnyAsync(x => x.EventId == eventId, cancellationToken))
        {
            return;
        }

        var idempotencyKey = $"refund-adjustment:{returnRequestId:N}";
        if (await _db.SettlementEntries.AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken))
        {
            _db.RefundInbox.Add(new SettlementRefundInboxRecord
            {
                EventId = eventId,
                ReturnRequestId = returnRequestId,
                ProcessedAt = DateTimeOffset.UtcNow,
            });
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var refund = await _returns.GetAsync(returnRequestId, cancellationToken)
            ?? throw new InvalidOperationException("refund برای adjustment پیدا نشد.");
        if (!string.Equals(refund.Currency, currency, StringComparison.OrdinalIgnoreCase)
            || refund.RefundAmount != refundAmount)
        {
            throw new InvalidOperationException("snapshot refund با رویداد هم‌خوان نیست.");
        }

        var policy = await GetDefaultCommissionPolicyAsync(cancellationToken);
        var policySnapshot = CommissionPolicySnapshot.FromPolicy(policy);
        var now = DateTimeOffset.UtcNow;
        var account = await EnsureAccountAsync(refund.SellerPartyId, currency, now, cancellationToken);
        var entry = SettlementEntry.PostDebitFromRefund(
            account.SettlementAccountId,
            refund.SellerPartyId,
            returnRequestId,
            refund.SellerOrderId,
            refundAmount,
            currency,
            policySnapshot,
            idempotencyKey,
            now);
        _db.SettlementEntries.Add(entry);
        _db.RefundInbox.Add(new SettlementRefundInboxRecord
        {
            EventId = eventId,
            ReturnRequestId = returnRequestId,
            ProcessedAt = now,
        });
        _telemetry.RecordEntryPosted();
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SettlementBalanceSnapshot?> GetBalanceAsync(Guid sellerPartyId, CancellationToken cancellationToken)
    {
        var account = await _db.SettlementAccounts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SellerPartyId == sellerPartyId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        return await BuildBalanceAsync(account, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SettlementEntrySnapshot>> ListEntriesAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var entries = await _db.SettlementEntries.AsNoTracking()
            .Where(x => x.SellerPartyId == sellerPartyId)
            .OrderByDescending(x => x.PostedAt)
            .ToListAsync(cancellationToken);
        return entries.Select(MapEntry).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<SettlementEntrySnapshot>>> ListEntriesBySellerOrderIdsAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        if (sellerOrderIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<SettlementEntrySnapshot>>();
        }

        var entries = await _db.SettlementEntries.AsNoTracking()
            .Where(x => x.SellerOrderId != null && sellerOrderIds.Contains(x.SellerOrderId.Value))
            .OrderByDescending(x => x.PostedAt)
            .ToListAsync(cancellationToken);
        return entries
            .GroupBy(x => x.SellerOrderId!.Value)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<SettlementEntrySnapshot>)group.Select(MapEntry).ToArray());
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SettlementStatementSnapshot>> ListStatementsAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var account = await _db.SettlementAccounts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SellerPartyId == sellerPartyId, cancellationToken);
        if (account is null)
        {
            return [];
        }

        var statements = await _db.SettlementStatements.AsNoTracking()
            .Where(x => x.SettlementAccountId == account.SettlementAccountId)
            .OrderByDescending(x => x.PeriodStart)
            .ToListAsync(cancellationToken);
        return statements.Select(MapStatement).ToArray();
    }

    /// <inheritdoc />
    public async Task<PayoutRequestSnapshot> RequestPayoutAsync(
        RequestPayoutCommand command,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);

        var existing = await _db.PayoutRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return await MapPayoutAsync(existing, cancellationToken);
        }

        var account = await _db.SettlementAccounts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SellerPartyId == command.SellerPartyId, cancellationToken)
            ?? throw new InvalidOperationException("حساب تسویه برای فروشنده پیدا نشد.");

        var balance = await BuildBalanceAsync(account, cancellationToken);
        if (command.Amount > balance.AvailableBalance)
        {
            throw new InvalidOperationException("مبلغ payout از ماندهٔ قابل برداشت بیشتر است.");
        }

        var now = DateTimeOffset.UtcNow;
        var request = PayoutRequest.Create(
            account.SettlementAccountId,
            command.SellerPartyId,
            command.Amount,
            account.Currency,
            command.IdempotencyKey,
            now);
        _db.PayoutRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapPayoutAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PayoutRequestSnapshot?> GetPayoutRequestAsync(Guid payoutRequestId, CancellationToken cancellationToken)
    {
        var request = await _db.PayoutRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PayoutRequestId == payoutRequestId, cancellationToken);
        return request is null ? null : await MapPayoutAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PayoutRequestSnapshot>> ListPayoutRequestsForSellerAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var requests = await _db.PayoutRequests.AsNoTracking()
            .Where(x => x.SellerPartyId == sellerPartyId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var snapshots = new List<PayoutRequestSnapshot>();
        foreach (var request in requests)
        {
            snapshots.Add(await MapPayoutAsync(request, cancellationToken));
        }

        return snapshots;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SettlementBalanceSnapshot>> ListAllBalancesAsync(CancellationToken cancellationToken)
    {
        var accounts = await _db.SettlementAccounts.AsNoTracking().ToListAsync(cancellationToken);
        var balances = new List<SettlementBalanceSnapshot>();
        foreach (var account in accounts)
        {
            balances.Add(await BuildBalanceAsync(account, cancellationToken));
        }

        return balances;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PayoutRequestSnapshot>> ListPayoutQueueAsync(CancellationToken cancellationToken)
    {
        var requests = await _db.PayoutRequests.AsNoTracking()
            .Where(x => x.Status == PayoutStatus.Pending || x.Status == PayoutStatus.Failed)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var snapshots = new List<PayoutRequestSnapshot>();
        foreach (var request in requests)
        {
            snapshots.Add(await MapPayoutAsync(request, cancellationToken));
        }

        return snapshots;
    }

    /// <inheritdoc />
    public async Task<PayoutRequestSnapshot> ProcessPayoutAsync(ProcessPayoutCommand command, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        return await ExecutePayoutAsync(command.PayoutRequestId, $"process:{command.PayoutRequestId:N}", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PayoutRequestSnapshot> RetryPayoutAsync(RetryPayoutCommand command, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        return await ExecutePayoutAsync(command.PayoutRequestId, $"retry:{command.ActorUserId:N}:{DateTimeOffset.UtcNow.Ticks}", cancellationToken);
    }

    private async Task<PayoutRequestSnapshot> ExecutePayoutAsync(
        Guid payoutRequestId,
        string attemptIdempotencyKey,
        CancellationToken cancellationToken)
    {
        var request = await _db.PayoutRequests
            .SingleOrDefaultAsync(x => x.PayoutRequestId == payoutRequestId, cancellationToken)
            ?? throw new InvalidOperationException("درخواست payout پیدا نشد.");
        if (request.Status == PayoutStatus.Succeeded)
        {
            return await MapPayoutAsync(request, cancellationToken);
        }

        var attempts = await _db.PayoutAttempts.AsNoTracking()
            .Where(x => x.PayoutRequestId == payoutRequestId)
            .ToListAsync(cancellationToken);
        request.AttachLoadedAttempts(attempts);

        var now = DateTimeOffset.UtcNow;
        var attempt = request.BeginAttempt(attemptIdempotencyKey, now);
        _db.PayoutAttempts.Add(attempt);

        var result = await _payoutGateway.PayoutAsync(
            request.PayoutRequestId,
            request.SellerPartyId,
            request.Amount,
            request.Currency,
            attempt.IdempotencyKey,
            cancellationToken);

        if (result.Succeeded)
        {
            request.MarkSucceeded(attempt.PayoutAttemptId, result.ProviderReference ?? "unknown", now);
            _telemetry.RecordPayoutSucceeded();
        }
        else
        {
            request.MarkFailed(attempt.PayoutAttemptId, result.FailureCode ?? "PAYOUT_FAILED", now);
            _telemetry.RecordPayoutFailed();
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MapPayoutAsync(request, cancellationToken);
    }

    private async Task<SettlementAccount> EnsureAccountAsync(
        Guid sellerPartyId,
        string currency,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var account = await _db.SettlementAccounts
            .SingleOrDefaultAsync(x => x.SellerPartyId == sellerPartyId, cancellationToken);
        if (account is not null)
        {
            return account;
        }

        account = SettlementAccount.Create(sellerPartyId, currency, now);
        _db.SettlementAccounts.Add(account);
        if (!await _db.SellerPayoutProfiles.AnyAsync(x => x.SellerPartyId == sellerPartyId, cancellationToken))
        {
            _db.SellerPayoutProfiles.Add(SellerPayoutProfile.CreateDevPlaceholder(sellerPartyId, now));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return account;
    }

    private async Task<CommissionPolicy> GetDefaultCommissionPolicyAsync(CancellationToken cancellationToken)
    {
        var policy = await _db.CommissionPolicies.AsNoTracking()
            .Where(x => x.IsDefault)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
        if (policy is not null)
        {
            return policy;
        }

        policy = CommissionPolicy.CreateDefaultMarketplace(DateTimeOffset.UtcNow);
        _db.CommissionPolicies.Add(policy);
        await _db.SaveChangesAsync(cancellationToken);
        return policy;
    }

    private async Task<SettlementBalanceSnapshot> BuildBalanceAsync(
        SettlementAccount account,
        CancellationToken cancellationToken)
    {
        var entries = await _db.SettlementEntries.AsNoTracking()
            .Where(x => x.SettlementAccountId == account.SettlementAccountId)
            .ToListAsync(cancellationToken);
        var credits = entries.Where(x => x.EntryType == EntryType.Credit).Sum(x => x.NetAmount);
        var debits = entries.Where(x => x.EntryType == EntryType.Debit).Sum(x => x.NetAmount);

        var payouts = await _db.PayoutRequests.AsNoTracking()
            .Where(x => x.SettlementAccountId == account.SettlementAccountId)
            .ToListAsync(cancellationToken);
        var reserved = payouts
            .Where(x => x.Status is PayoutStatus.Pending or PayoutStatus.Processing or PayoutStatus.Succeeded)
            .Sum(x => x.Amount);
        var available = credits - debits - reserved;
        if (available < 0)
        {
            available = 0;
        }

        return new SettlementBalanceSnapshot(
            account.SettlementAccountId,
            account.SellerPartyId,
            account.Currency,
            credits,
            debits,
            reserved,
            available);
    }

    private async Task<PayoutRequestSnapshot> MapPayoutAsync(PayoutRequest request, CancellationToken cancellationToken)
    {
        var attempts = await _db.PayoutAttempts.AsNoTracking()
            .Where(x => x.PayoutRequestId == request.PayoutRequestId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return new PayoutRequestSnapshot(
            request.PayoutRequestId,
            request.SettlementAccountId,
            request.SellerPartyId,
            request.Amount,
            request.Currency,
            request.Status,
            request.IdempotencyKey,
            request.CreatedAt,
            request.UpdatedAt,
            attempts.Select(x => new PayoutAttemptSnapshot(
                x.PayoutAttemptId,
                x.PayoutRequestId,
                x.Status,
                x.IdempotencyKey,
                x.ProviderReference,
                x.FailureCode,
                x.CreatedAt,
                x.CompletedAt)).ToArray());
    }

    private static SettlementEntrySnapshot MapEntry(SettlementEntry entry) =>
        new(
            entry.EntryId,
            entry.SettlementAccountId,
            entry.SellerPartyId,
            entry.EntryType,
            entry.GrossAmount,
            entry.CommissionAmount,
            entry.NetAmount,
            entry.Currency,
            entry.CommissionPolicySnapshot,
            entry.SourceType,
            entry.SourceId,
            entry.SellerOrderId,
            entry.PostedAt);

    private static SettlementStatementSnapshot MapStatement(SettlementStatement statement) =>
        new(
            statement.StatementId,
            statement.SettlementAccountId,
            statement.Status,
            statement.PeriodStart,
            statement.PeriodEnd,
            statement.OpeningBalance,
            statement.ClosingBalance,
            statement.Currency,
            statement.CreatedAt);
}
