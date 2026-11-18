using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Persistence;

namespace Tooba.Returns.Infrastructure.Directories;

/// <summary>
/// TB-277: the FIRST refund of an APPROVED case — the deliberate financial step that approval deliberately does not
/// take.
/// <para>
/// Approving a return records a decision only. This intent is what actually starts the money, and it exists exactly
/// once per case: the attempt id and its provider-facing key are derived from the case id, so a duplicate click, a
/// lost response or a remount resolves to the SAME durable attempt instead of minting a second first attempt.
/// </para>
/// <para>
/// It is deliberately neither <c>resume_pending_refund</c> (repeat ONE stored pending attempt whose outcome is
/// unknown) nor <c>retry_refund</c> (mint the deterministic child of ONE reviewed definite failure). A case that
/// already owns any attempt is answered from its own stored row with zero effect — it is never re-sent and never
/// re-keyed from the current status or an attempt count.
/// </para>
/// <para>
/// TB-279 (B/E): the FIRST attempt is persisted inside a SHORT owning transaction that re-reads the case AFTER its row
/// lock and the shared seller-order serialization, verifies the CURRENT effective decision revision and the frozen
/// approved amount, and reserves the payment's shared refund budget atomically. The transaction commits BEFORE any
/// gateway/Wallet call, so a concurrent correction can never let a stale approval send money, and an unknown outcome
/// keeps its reservation instead of losing it.
/// </para>
/// </summary>
public sealed partial class ReturnDirectory
{
    /// <inheritdoc />
    public async Task<ReturnSnapshot> StartFirstRefundAsync(
        StartReturnRefundCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await _guard.EnsureCanMutateAsync(cancellationToken);

        // TB-273-R1 B: the external money call may never span a caller-owned/enlisted/ambient transaction, so the
        // intent is refused before any read or effect of this request's own work.
        RefuseConstrainedTransaction();

        var request = await LoadMutableAsync(command.ReturnRequestId, cancellationToken);
        var snapshot = await MapSnapshotAsync(request, cancellationToken);

        // ONE exact identity for the first money step of this case.
        var attemptId = ReturnFirstRefundIdentity.DeriveAttemptId(request.ReturnRequestId);
        var idempotencyKey = ReturnFirstRefundIdentity.DeriveIdempotencyKey(request.ReturnRequestId);

        var existing = request.RefundAttempts.FirstOrDefault(x =>
            x.RefundAttemptId == attemptId
            || string.Equals(x.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));

        if (existing is not null)
        {
            // This exact intent already owns a durable attempt: answer the stored row (pending, failed or succeeded)
            // with ZERO effect. A terminal attempt is never re-sent, and an unknown one stays that case's pending
            // money for the explicit resume intent — a second first attempt can never exist.
            return await MapSnapshotAsync(request, cancellationToken);
        }

        // TB-279 (B): the reviewed revision this intent is acting on. A correction that landed after this read is
        // refused by the locked re-check below instead of sending the money of a superseded decision.
        var expectedDecisionVersion = snapshot.DecisionVersion;
        var description = ReturnRecoveryPolicy.Describe(snapshot);
        if (description.Phase == ReturnRecoveryPhase.Blocked)
        {
            // The read exposes this typed blocker; the action refuses with the SAME reason and zero effect.
            throw new ContractOperationException(
                description.BlockerCode ?? ReturnsErrorCodes.FirstRefundNotLawful);
        }

        if (description.Phase != ReturnRecoveryPhase.AwaitingFirstRefund)
        {
            // Anything else (unapproved, already started, unknown/failed money, physical-only or settled) is NOT a
            // first refund. It is a typed refusal with zero effect rather than an inferred attempt.
            throw new ContractOperationException(ReturnsErrorCodes.FirstRefundNotLawful);
        }

        if (request.Status != ReturnRequestStatus.Approved)
        {
            throw new ContractOperationException(ReturnsErrorCodes.FirstRefundNotLawful);
        }

        if (request.PaymentId is not { } paymentId || paymentId == Guid.Empty)
        {
            throw new ContractOperationException(ReturnsErrorCodes.RefundPaymentMissing);
        }

        if (request.Origin == ReturnOrigin.CourierOrigin)
        {
            // TB-265 / TB-277: the courier source/claim/obligation/branch closure phases are the PREREQUISITE of the
            // money step, so they run here — before the attempt is persisted and before any external call — while
            // approval itself never released a claim, a hold or stock. No external call runs inside the short
            // attempt/budget transaction below.
            await EnsureCourierOriginClosureAsync(request, command.ActorUserId, cancellationToken);
        }
        else
        {
            // TB-274: an ordinary delivered RMA may only start money over its OWN complete and coherent recorded
            // customer receipt; the client's ceiling is never the authority. TB-279: the reviewed resolution must also
            // be complete, because its accepted quantities are the ONLY basis of the approved amount.
            var receiving = await ReadReceivingAsync(request, cancellationToken);
            if (receiving is not { IsCoherent: true, IsComplete: true })
            {
                throw new ContractOperationException(
                    receiving is { IsCoherent: false }
                        ? ReturnsErrorCodes.RecoveryEvidenceConflict
                        : ReturnsErrorCodes.RecoveryReceivingRequired);
            }

            if (snapshot.Resolution is not { IsComplete: true })
            {
                throw new ContractOperationException(
                    snapshot.Resolution?.BlockerCode ?? ReturnsErrorCodes.ResolutionRequired);
            }
        }

        var payment = await _payments.GetAsync(paymentId, request.RequestedByUserId, null, cancellationToken)
            ?? throw new ContractOperationException(ReturnsErrorCodes.RefundPaymentMissing);
        if (!string.Equals(payment.Status, "Succeeded", StringComparison.Ordinal))
        {
            throw new ContractOperationException(ReturnsErrorCodes.RefundAlreadyStarted);
        }

        var amount = ReturnResolutionPolicy.MoneyOf(snapshot);
        if (amount < 0m || amount > payment.Amount)
        {
            // A reviewed amount can never exceed the owning payment, and a negative one is not money.
            throw new ContractOperationException(ReturnsErrorCodes.RefundBudgetExceeded);
        }

        // Persist the ONE durable attempt + its shared budget reservation BEFORE the external call: a crash after the
        // send must leave the same identity/key/reservation behind for reconciliation.
        var persisted = await PersistFirstAttemptUnderLockAsync(
            request,
            attemptId,
            idempotencyKey,
            paymentId,
            amount,
            expectedDecisionVersion,
            cancellationToken);
        if (!persisted)
        {
            // A concurrent duplicate of this exact intent (or a correction that won the version) answered inside the
            // lock. Either way this request sends nothing.
            return await GetAsync(request.ReturnRequestId, cancellationToken)
                ?? await MapSnapshotAsync(request, cancellationToken);
        }

        var attempt = request.RefundAttempts.Single(x => x.RefundAttemptId == attemptId);
        _telemetry.RecordFirstRefundStarted();

        // The external call happens with NO transaction and NO row lock held; the outcome is applied by the existing
        // short owning coordination which re-reads the exact durable attempt.
        var outcome = await ExecuteRefundAsync(request, attempt, cancellationToken);
        if (outcome == RefundExecutionOutcome.Unavailable)
        {
            // An explicitly unconfigured adapter proves nothing was sent: the pending attempt and its key are kept for
            // the same intent — never a fabricated refund and never a second key.
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryGatewayUnavailable);
        }

        if (request.Origin == ReturnOrigin.CourierOrigin && request.Status == ReturnRequestStatus.Completed)
        {
            await MarkRefundReleasedWhenFullyDisposedAsync(request, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MapSnapshotAsync(request, cancellationToken);
    }

    /// <summary>
    /// TB-279 (B/E): the SHORT owning transaction of the first money step. It takes the shared seller-order
    /// serialization and the case row lock, re-reads every fact, verifies the CURRENT effective decision revision and
    /// the frozen approved amount, checks the payment's shared refund budget, and persists the attempt + reservation.
    /// Returns <c>false</c> when nothing was persisted (a concurrent duplicate or a superseded decision) — no money may
    /// be sent in that case.
    /// </summary>
    private async Task<bool> PersistFirstAttemptUnderLockAsync(
        ReturnRequest request,
        Guid attemptId,
        string idempotencyKey,
        Guid paymentId,
        decimal amount,
        int expectedDecisionVersion,
        CancellationToken cancellationToken)
    {
        var relational = _db.Database.IsRelational();
        var ownsTransaction = relational && _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            if (relational)
            {
                await LockOwningSellerOrderAsync(request.ReturnRequestId, cancellationToken);
                await _db.Database.ExecuteSqlAsync(
                    $"SELECT 1 FROM returns.return_requests WHERE return_request_id = {request.ReturnRequestId} FOR UPDATE",
                    cancellationToken);
            }

            // Re-read AFTER the lock: every fact this decision depends on is the CURRENT committed one, not the value
            // this request read before the lock.
            await _db.Entry(request).ReloadAsync(cancellationToken);
            var state = await ReadDecisionStateAsync(request.ReturnRequestId, cancellationToken);
            var staleAttempt = await _db.RefundAttempts.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.ReturnRequestId == request.ReturnRequestId
                        && (x.RefundAttemptId == attemptId || x.IdempotencyKey == idempotencyKey),
                    cancellationToken);
            if (staleAttempt is not null)
            {
                await CommitCorrectionAsync(transaction, ownsTransaction, cancellationToken);
                return false;
            }

            if (state.EffectiveVersion != expectedDecisionVersion
                || !string.Equals(
                    state.EffectiveDecision,
                    ReturnDecisionRevision.ApproveDecision,
                    StringComparison.Ordinal)
                || request.Status != ReturnRequestStatus.Approved
                || request.PaymentId != paymentId)
            {
                // The reviewed decision was corrected (or the case moved) while this request waited: a stale approval
                // can never send money.
                throw new ContractOperationException(ReturnsErrorCodes.CorrectionVersionStale);
            }

            var currentApproved = request.ApprovedAmount ?? amount;
            if (currentApproved != amount)
            {
                // The frozen approved money changed after this request read it. It is never re-derived from the older
                // in-memory value.
                throw new ContractOperationException(ReturnsErrorCodes.CorrectionVersionStale);
            }

            if (relational)
            {
                // TB-279 (E): the shared refund budget of this PAYMENT is serialized by its own Returns-owned lock, so
                // two seller cases of one checkout can never both reserve the same remaining budget.
                await ReturnBudgetSerialization.AcquireAsync(_db.Database, paymentId, cancellationToken);
            }

            var committedPaymentObligations = await _db.RefundAttempts.AsNoTracking()
                .Where(x => x.PaymentId == paymentId
                    && (x.Status == RefundAttemptStatus.Pending || x.Status == RefundAttemptStatus.Succeeded))
                .Select(x => x.Amount)
                .ToListAsync(cancellationToken);
            var budget = await _payments.GetAsync(paymentId, request.RequestedByUserId, null, cancellationToken);
            var reserved = committedPaymentObligations.Sum();
            if (budget is not null && reserved + amount > budget.Amount)
            {
                throw new ContractOperationException(ReturnsErrorCodes.RefundBudgetExceeded);
            }

            request.MarkRefundProcessing(_clock.UtcNow);
            request.FreezeApprovedAmount(amount);
            var attempt = request.BeginRefundAttempt(attemptId, paymentId, idempotencyKey, _clock.UtcNow);
            _db.RefundAttempts.Add(attempt);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsFirstAttemptKeyConflict(ex))
            {
                // Another duplicate of this SAME intent already persisted the first attempt. Only this attempt's entry
                // is detached (never a caller-wide tracker clear) and nothing is sent.
                _db.Entry(attempt).State = EntityState.Detached;
                if (ownsTransaction && transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return false;
            }

            await CommitCorrectionAsync(transaction, ownsTransaction, cancellationToken);
            return true;
        }
        catch
        {
            if (ownsTransaction && transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    /// <summary>
    /// TB-277: ONLY this case's own first-attempt identities mean "a duplicate of the same first intent won". Any
    /// other unique violation is a genuine error and surfaces unchanged.
    /// </summary>
    private static bool IsFirstAttemptKeyConflict(DbUpdateException exception) =>
        exception.InnerException is Npgsql.PostgresException { SqlState: "23505" } postgres
        && string.Equals(postgres.TableName, "refund_attempts", StringComparison.Ordinal)
        && string.Equals(postgres.SchemaName, ReturnsDbContext.Schema, StringComparison.Ordinal)
        && (string.Equals(postgres.ConstraintName, "pk_refund_attempts", StringComparison.OrdinalIgnoreCase)
            || string.Equals(postgres.ConstraintName, "ix_refund_attempts_IdempotencyKey", StringComparison.OrdinalIgnoreCase));
}
