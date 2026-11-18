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
/// TB-279-R1 (F1/F4): the reviewed decision revision travels INSIDE the frozen command; the owning payment facts are
/// read before any transaction; and the SAME shared reservation protocol the reviewed retry uses persists the attempt +
/// its payment budget atomically before the external call.
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
            // TB-274-R1 / TB-279-R1 (F2): an ordinary delivered RMA may only start money over its OWN coherent recorded
            // customer receipt AND a complete reviewed resolution. The resolution — not "every requested unit arrived" —
            // is the money basis: accepted 2 received + rejected 1 never received is a lawful, complete plan.
            if (snapshot.Receiving is { IsCoherent: false })
            {
                throw new ContractOperationException(ReturnsErrorCodes.RecoveryEvidenceConflict);
            }

            if (snapshot.Resolution is not { IsComplete: true })
            {
                throw new ContractOperationException(
                    snapshot.Resolution?.BlockerCode ?? ReturnsErrorCodes.ResolutionRequired);
            }
        }

        // TB-279-R1 (F4): the operator's reviewed decision revision is REQUIRED, and it is checked AFTER the semantic
        // gates above (approval, receiving/resolution, courier closure) so an unlawful case answers its own truthful
        // refusal first. A fresh server-side read can never stand in for the version that was actually reviewed.
        var expectedDecisionVersion = RequireReviewedDecisionVersion(command.ExpectedDecisionVersion);

        // TB-279-R1 (F1): the owning payment facts are acquired BEFORE any transaction/lock, and a missing authority is
        // a refusal — never a skipped ceiling.
        var authority = await RequirePaymentAuthorityAsync(request, cancellationToken);

        var reservation = await PersistNewAttemptUnderReservationAsync(
            request,
            attemptId,
            idempotencyKey,
            authority,
            expectedDecisionVersion,
            reviewedFailedAttemptId: null,
            cancellationToken);
        if (reservation == AttemptReservation.AlreadyAnswered)
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
}
