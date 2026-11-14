using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Persistence;

namespace Tooba.Returns.Infrastructure.Directories;

/// <summary>
/// TB-265-R2: the durable, serialized approve-vs-reject decision of one return case.
/// <para>
/// Both operator paths used to read the mutable case independently, so two requests could both observe
/// <c>Requested</c> and then approve (refund + release the source claim) while the other rejected (release the claim).
/// The decision row is now the ONE authority: it is written under the case row lock BEFORE any money, stock or claim
/// effect, the first committed decision wins, and the loser is refused with the exact typed code instead of acting on
/// a stale read. The short lock transaction never spans an external payment call.
/// </para>
/// </summary>
public sealed partial class ReturnDirectory
{
    private enum ReturnDecisionKind
    {
        Approve,
        Reject,
    }

    /// <summary>
    /// Records — or reads back — the durable decision of this case, serialized on the case row.
    /// Returns <c>null</c> only when the case does not exist.
    /// </summary>
    private async Task<ReturnDecisionRecord?> RecordReturnDecisionAsync(
        Guid returnRequestId,
        ReturnDecisionKind kind,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var ownsTransaction = _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        // The case row is the shared protocol: both decisions serialize here, so the read-then-write of the decision
        // cannot interleave. When this adapter owns the transaction it is committed before any external/money work;
        // inside a caller-owned transaction the lock simply lives until that caller commits.
        await _db.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM returns.return_requests WHERE return_request_id = {returnRequestId} FOR UPDATE",
            cancellationToken);

        var recorded = await _db.ReturnDecisions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReturnRequestId == returnRequestId, cancellationToken);
        if (recorded is not null)
        {
            await CommitDecisionAsync(transaction, ownsTransaction, cancellationToken);
            return recorded;
        }

        var current = await _db.ReturnRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReturnRequestId == returnRequestId, cancellationToken);
        if (current is null)
        {
            await CommitDecisionAsync(transaction, ownsTransaction, cancellationToken);
            return null;
        }

        var conflictsWithCurrentState = kind == ReturnDecisionKind.Approve
            ? current.Status is ReturnRequestStatus.Rejected or ReturnRequestStatus.Cancelled
            : current.Status is ReturnRequestStatus.Approved
                or ReturnRequestStatus.RefundProcessing
                or ReturnRequestStatus.RefundFailed
                or ReturnRequestStatus.Completed;

        if (conflictsWithCurrentState)
        {
            // A legal, already-started decision (or a terminal state) exists. Answering with the opposite decision
            // would release/refund capacity another authority already owns.
            await CommitDecisionAsync(transaction, ownsTransaction, cancellationToken);
            throw new ContractOperationException(kind == ReturnDecisionKind.Approve
                ? ReturnsErrorCodes.AlreadyRejected
                : ReturnsErrorCodes.AlreadyApproved);
        }

        var decision = new ReturnDecisionRecord
        {
            ReturnRequestId = returnRequestId,
            Decision = kind == ReturnDecisionKind.Approve
                ? nameof(ReturnDecisionKind.Approve)
                : nameof(ReturnDecisionKind.Reject),
            DecidedByUserId = actorUserId,
            DecidedAt = _clock.UtcNow,
        };

        _db.ReturnDecisions.Add(decision);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await CommitDecisionAsync(transaction, ownsTransaction, cancellationToken);
            return decision;
        }
        catch (DbUpdateException)
        {
            // The primary key on the case is the durable guard: a racing context that did not wait on the row lock
            // still cannot record a second, opposite decision.
            if (ownsTransaction && transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            _db.ChangeTracker.Clear();
            return await _db.ReturnDecisions.AsNoTracking()
                .SingleOrDefaultAsync(x => x.ReturnRequestId == returnRequestId, cancellationToken)
                ?? throw new ContractOperationException(ReturnsErrorCodes.Stale);
        }
    }

    /// <summary>True when the recorded decision is the one this caller is trying to perform.</summary>
    private static bool AnswersDecision(ReturnDecisionRecord decision, ReturnDecisionKind kind) =>
        string.Equals(
            decision.Decision,
            kind == ReturnDecisionKind.Approve
                ? nameof(ReturnDecisionKind.Approve)
                : nameof(ReturnDecisionKind.Reject),
            StringComparison.Ordinal);

    private static async Task CommitDecisionAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction,
        bool ownsTransaction,
        CancellationToken cancellationToken)
    {
        if (ownsTransaction && transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }
}
