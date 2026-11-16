using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Infrastructure.Directories;

/// <summary>
/// TB-273: recovery of ONE already-approved case.
/// <para>
/// The intent is split by OWNER PHASE: money recovery repeats the exact stored pending attempt, while physical
/// recovery settles disposition with zero money calls. Both read the same stored-fact policy the admin projection
/// exposes (<see cref="ReturnRecoveryPolicy"/>), so no action is ever guessed from a status alone, and neither kind
/// mints a new attempt, chooses a destination, recomputes an amount or approves a case.
/// </para>
/// <para>
/// TB-273-R1: the policy is fail closed. A missing/opposite durable approval, an attempt that does not bind to this
/// exact case, or an incoherent courier source/closure state is a typed blocker with ZERO effect — the read
/// projection reports the same blocker, so an operator can never be told an action exists that the action refuses.
/// An effectful recovery is also refused while a caller-owned/enlisted/ambient transaction is open, so no external
/// money or stock call can ever span someone else's transaction.
/// </para>
/// </summary>
public sealed partial class ReturnDirectory
{
    /// <inheritdoc />
    public async Task<ReturnSnapshot> RecoverAsync(RecoverReturnCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await _guard.EnsureCanMutateAsync(cancellationToken);

        // TB-273-R1 B: refusal happens BEFORE any read of a mutable case and before any effect, so a caller-owned
        // transaction can never be joined by the owning outcome write or spanned by a gateway/Wallet/Inventory call.
        RefuseConstrainedTransaction();

        var request = await LoadMutableAsync(command.ReturnRequestId, cancellationToken);

        // ONE read description over the STORED facts (the same policy every admin read projection returns).
        var snapshot = await MapSnapshotAsync(request, cancellationToken);
        var description = ReturnRecoveryPolicy.Describe(snapshot);

        return command.Kind switch
        {
            ReturnRecoveryKind.ResumePendingRefund =>
                await RecoverPendingRefundAsync(request, snapshot, description, command, cancellationToken),
            ReturnRecoveryKind.SettlePhysicalDisposition =>
                await RecoverPhysicalDispositionAsync(request, snapshot, description, command, cancellationToken),
            _ => throw new ContractOperationException(ReturnsErrorCodes.RecoveryKindInvalid),
        };
    }

    /// <summary>
    /// Repeats the EXACT stored pending refund attempt. The attempt identity, payment, amount, currency and
    /// idempotency key all come from the durable row; nothing is supplied by the caller and nothing is minted.
    /// </summary>
    private async Task<ReturnSnapshot> RecoverPendingRefundAsync(
        ReturnRequest request,
        ReturnSnapshot snapshot,
        ReturnRecoveryDescription description,
        RecoverReturnCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ExpectedRefundAttemptId is not { } expected || expected == Guid.Empty)
        {
            // The transport shape is refused by the validator; this is the owning guard for any non-HTTP caller.
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryAttemptMismatch);
        }

        var attempt = request.RefundAttempts.SingleOrDefault(x => x.RefundAttemptId == expected)
            ?? throw new ContractOperationException(ReturnsErrorCodes.RecoveryAttemptMismatch);

        if (attempt.Status != RefundAttemptStatus.Pending)
        {
            // A recorded terminal outcome — success OR a definite failure — is answered exactly as stored. It is
            // never re-sent and never re-minted under a new key. TB-273-R1: the answer is only authorized when this
            // exact id is the case's single BOUND attempt; a wrong/stale id cannot borrow that completion and cannot
            // bypass validation merely because the case looks settled.
            if (description.RefundAttemptId == expected)
            {
                return snapshot;
            }

            throw new ContractOperationException(
                description.Phase is ReturnRecoveryPhase.Settled or ReturnRecoveryPhase.Blocked
                    ? ReturnsErrorCodes.RecoveryAttemptMismatch
                    : description.BlockerCode ?? ReturnsErrorCodes.RecoveryStateMismatch);
        }

        if (description.Phase == ReturnRecoveryPhase.Blocked)
        {
            // The read exposes this blocker; the action must refuse with the SAME typed reason and zero effect.
            throw new ContractOperationException(
                description.BlockerCode ?? ReturnsErrorCodes.RecoveryAttemptUnbound);
        }

        if (description.Phase != ReturnRecoveryPhase.PendingMoney || description.RefundAttemptId != expected)
        {
            // Unknown money is executable only when the case binds exactly ONE pending attempt, and it is this one.
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryStateMismatch);
        }

        if (request.Origin == ReturnOrigin.CourierOrigin)
        {
            // The owning closure phases are re-checked idempotently BEFORE money; this creates no receipt, no
            // allocation and no attempt. A case whose source links are incoherent never reaches here (blocked above).
            await EnsureCourierOriginClosureAsync(request, command.ActorUserId, cancellationToken);
        }
        else
        {
            // TB-274 (B): an ordinary delivered RMA repeats its unknown money ONLY over its own recorded, complete and
            // coherent customer receipt. The blocker is the SAME one the read description reports and it mints no key.
            var receiving = await ReadReceivingAsync(request, cancellationToken);
            if (receiving is not { IsCoherent: true, IsComplete: true })
            {
                throw new ContractOperationException(
                    receiving is { IsCoherent: false }
                        ? ReturnsErrorCodes.RecoveryEvidenceConflict
                        : ReturnsErrorCodes.RecoveryReceivingRequired);
            }
        }

        // The external call happens with NO transaction or row lock held; the outcome is applied under a short
        // owning coordination that re-reads the exact durable attempt.
        var outcome = await ExecuteRefundAsync(
            request, attempt, cancellationToken);

        if (outcome == RefundExecutionOutcome.Unavailable)
        {
            // TB-273 B: an explicitly unconfigured adapter is a truthful "cannot send now", never a refund and never
            // an invented terminal rejection. Nothing was written, so the SAME attempt stays pending for a later
            // recovery — no new attempt and no new key.
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryGatewayUnavailable);
        }

        if (request.Origin == ReturnOrigin.CourierOrigin && request.Status == ReturnRequestStatus.Completed)
        {
            // TB-273-R1: the final marker is decided from DURABLE item/closure facts, never from in-memory flags.
            await MarkRefundReleasedWhenFullyDisposedAsync(request, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MapSnapshotAsync(request, cancellationToken);
    }

    /// <summary>
    /// Settles the PHYSICAL disposition of a confirmed-money case: zero refund/Wallet calls, zero new attempts.
    /// A delivered-customer case has no recorded receipt/inspection fact, so it is refused with the explicit
    /// blocker instead of inventing a saleable restock from the default condition.
    /// <para>
    /// TB-273-R1: when every item disposition is already durable and only the FINAL owning closure marker is
    /// missing, this intent writes just that marker — it calls no external owner again and it touches no money.
    /// </para>
    /// </summary>
    private async Task<ReturnSnapshot> RecoverPhysicalDispositionAsync(
        ReturnRequest request,
        ReturnSnapshot snapshot,
        ReturnRecoveryDescription description,
        RecoverReturnCommand command,
        CancellationToken cancellationToken)
    {
        if (description.Phase == ReturnRecoveryPhase.Settled)
        {
            // Recorded completion: answered from the stored state with zero effect, never a second stock action.
            return snapshot;
        }

        if (description.Phase == ReturnRecoveryPhase.Blocked)
        {
            // Fail closed. For an ordinary RMA this is the explicit receiving-required blocker; for incoherent
            // courier evidence it is the evidence conflict. Neither may move stock.
            throw new ContractOperationException(
                description.BlockerCode ?? ReturnsErrorCodes.RecoveryStateMismatch);
        }

        if (description.Phase != ReturnRecoveryPhase.PendingDisposition || !description.FinancialComplete)
        {
            // Unconfirmed or failed money is never repaired by moving stock.
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryStateMismatch);
        }

        if (request.Origin != ReturnOrigin.CourierOrigin)
        {
            // TB-274: an ordinary delivered RMA is executable ONLY over its own recorded, complete and coherent
            // customer receipt. Without that evidence the confirmed money stays intact and the line stays pending —
            // the requested quantity is never restocked as if it had been received and inspected as usable.
            var receiving = await ReadReceivingAsync(request, cancellationToken);
            if (receiving is not { IsCoherent: true, IsComplete: true })
            {
                throw new ContractOperationException(ReturnsErrorCodes.RecoveryReceivingRequired);
            }

            await SettlePhysicalDispositionAsync(request, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return await MapSnapshotAsync(request, cancellationToken);
        }

        // Existing physical receipt/claim/closure sequencing is re-read idempotently. A completed lineage is history,
        // not a new receipt or allocation; a marker-only recovery performs no external owner call here.
        await EnsureCourierOriginClosureAsync(request, command.ActorUserId, cancellationToken);

        await SettlePhysicalDispositionAsync(request, cancellationToken);
        await MarkRefundReleasedWhenFullyDisposedAsync(request, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapSnapshotAsync(request, cancellationToken);
    }
}
