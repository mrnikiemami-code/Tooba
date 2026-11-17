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
        var description = ReturnRecoveryPolicy.Describe(snapshot);

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

        // The recorded APPROVE decision is a stored fact the policy already required; re-read it as a positive guard
        // so no other code path can ever start money over a missing or opposite decision.
        var recordedDecision = await _db.ReturnDecisions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReturnRequestId == request.ReturnRequestId, cancellationToken);
        if (recordedDecision is null || !AnswersDecision(recordedDecision, ReturnDecisionKind.Approve))
        {
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
            // approval itself never released a claim, a hold or stock.
            await EnsureCourierOriginClosureAsync(request, command.ActorUserId, cancellationToken);
        }
        else
        {
            // TB-274: an ordinary delivered RMA may only start money over its OWN complete and coherent recorded
            // customer receipt; the client's ceiling is never the authority.
            var receiving = await ReadReceivingAsync(request, cancellationToken);
            if (receiving is not { IsCoherent: true, IsComplete: true })
            {
                throw new ContractOperationException(
                    receiving is { IsCoherent: false }
                        ? ReturnsErrorCodes.RecoveryEvidenceConflict
                        : ReturnsErrorCodes.RecoveryReceivingRequired);
            }
        }

        var payment = await _payments.GetAsync(paymentId, request.RequestedByUserId, null, cancellationToken)
            ?? throw new ContractOperationException(ReturnsErrorCodes.RefundPaymentMissing);
        if (!string.Equals(payment.Status, "Succeeded", StringComparison.Ordinal))
        {
            throw new ContractOperationException(ReturnsErrorCodes.RefundAlreadyStarted);
        }

        // Persist the ONE durable attempt BEFORE the external call: a crash after the send must leave the same
        // identity/key behind for reconciliation, and the unique key is what makes a concurrent duplicate lose.
        request.MarkRefundProcessing(_clock.UtcNow);
        var attempt = request.BeginRefundAttempt(attemptId, paymentId, idempotencyKey, _clock.UtcNow);
        _db.RefundAttempts.Add(attempt);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsFirstAttemptKeyConflict(ex))
        {
            // Another duplicate of this SAME intent already persisted the first attempt. Only this attempt's entry is
            // detached (never a caller-wide tracker clear) and the stored row is answered as-is: no second send.
            _db.Entry(attempt).State = EntityState.Detached;
            var stored = await _db.RefundAttempts.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.ReturnRequestId == request.ReturnRequestId
                        && (x.RefundAttemptId == attemptId
                            || x.IdempotencyKey == idempotencyKey),
                    cancellationToken);
            if (stored is null)
            {
                throw;
            }

            var storedSnapshot = await GetAsync(request.ReturnRequestId, cancellationToken);
            return storedSnapshot ?? await MapSnapshotAsync(request, cancellationToken);
        }

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
