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
/// </summary>
public sealed partial class ReturnDirectory
{
    /// <inheritdoc />
    public async Task<ReturnSnapshot> RecoverAsync(RecoverReturnCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await _guard.EnsureCanMutateAsync(cancellationToken);

        var request = await LoadMutableAsync(command.ReturnRequestId, cancellationToken);

        // The durable decision authority wins over recovery: a case another authority rejected can never gain a
        // money or stock action here, and this read writes nothing.
        var decision = await _db.ReturnDecisions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReturnRequestId == command.ReturnRequestId, cancellationToken);
        if (decision is not null && !AnswersDecision(decision, ReturnDecisionKind.Approve))
        {
            throw new ContractOperationException(ReturnsErrorCodes.AlreadyRejected);
        }

        // ONE read description over the STORED facts (the same policy the admin projection returns).
        var snapshot = await MapSnapshotAsync(request, cancellationToken);
        var description = ReturnRecoveryPolicy.Describe(snapshot);

        if (description.Phase == ReturnRecoveryPhase.Settled)
        {
            // Recorded completion: answered from the stored state with zero effect, never a second money/stock action.
            return snapshot;
        }

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
            // never re-sent and never re-minted under a new key by a recovery intent.
            return snapshot;
        }

        if (description.Phase != ReturnRecoveryPhase.PendingMoney || description.RefundAttemptId != expected)
        {
            // Unknown money is executable only when the case binds exactly ONE pending attempt, and it is this one.
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryStateMismatch);
        }

        if (request.PaymentId != attempt.PaymentId
            || attempt.Amount != request.RefundAmount
            || !string.Equals(attempt.Currency, request.Currency, StringComparison.Ordinal))
        {
            // Case/payment/amount/currency coherence is a stored fact, never recomputed from the request.
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryAttemptMismatch);
        }

        if (request.Origin == ReturnOrigin.CourierOrigin)
        {
            // The owning closure phases are re-checked idempotently BEFORE money; this creates no receipt, no
            // allocation and no attempt.
            await EnsureCourierOriginClosureAsync(request, command.ActorUserId, cancellationToken);
        }

        // The external call happens with NO transaction or row lock held; the outcome is applied under a short
        // owning coordination that re-reads the exact durable attempt.
        var outcome = await ExecuteRefundAsync(
            request, attempt, cancellationToken, settleOrdinaryPhysicalDisposition: false);

        if (outcome == RefundExecutionOutcome.Unavailable)
        {
            // TB-273 B: an explicitly unconfigured adapter is a truthful "cannot send now", never a refund and never
            // an invented terminal rejection. Nothing was written, so the SAME attempt stays pending for a later
            // recovery — no new attempt and no new key.
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryGatewayUnavailable);
        }

        if (request.Origin == ReturnOrigin.CourierOrigin && request.Status == ReturnRequestStatus.Completed)
        {
            MarkRefundReleasedWhenFullyDisposed(request);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MapSnapshotAsync(request, cancellationToken);
    }

    /// <summary>
    /// Settles the PHYSICAL disposition of a confirmed-money case: zero refund/Wallet calls, zero new attempts.
    /// A delivered-customer case has no recorded receipt/inspection fact, so it is refused with the explicit
    /// blocker instead of inventing a saleable restock from the default condition.
    /// </summary>
    private async Task<ReturnSnapshot> RecoverPhysicalDispositionAsync(
        ReturnRequest request,
        ReturnSnapshot snapshot,
        ReturnRecoveryDescription description,
        RecoverReturnCommand command,
        CancellationToken cancellationToken)
    {
        if (!description.FinancialComplete)
        {
            // Unconfirmed or failed money is never repaired by moving stock.
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryStateMismatch);
        }

        if (request.Origin != ReturnOrigin.CourierOrigin)
        {
            throw new ContractOperationException(ReturnsErrorCodes.RecoveryReceivingRequired);
        }

        // Existing physical receipt/claim/closure sequencing is re-read idempotently. A completed lineage is history,
        // not a new receipt or allocation.
        await EnsureCourierOriginClosureAsync(request, command.ActorUserId, cancellationToken);

        await SettlePhysicalDispositionAsync(request, cancellationToken, allowOrdinaryDisposition: false);
        MarkRefundReleasedWhenFullyDisposed(request);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapSnapshotAsync(request, cancellationToken);
    }
}
