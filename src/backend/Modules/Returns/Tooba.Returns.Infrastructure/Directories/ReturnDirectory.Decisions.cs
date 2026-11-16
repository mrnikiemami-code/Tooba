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
    /// <para>
    /// TB-274: a rejection carries the reviewed operator reason in the SAME row, so the winning decision and its
    /// statement can never disagree. An exact replay must match the stored reason; a changed reason is a typed
    /// conflict with zero effect. An approval carries no rejection reason.
    /// </para>
    /// </summary>
    private async Task<ReturnDecisionRecord?> RecordReturnDecisionAsync(
        Guid returnRequestId,
        ReturnDecisionKind kind,
        Guid actorUserId,
        CancellationToken cancellationToken,
        string? reason = null)
    {
        var normalizedReason = kind == ReturnDecisionKind.Reject ? NormalizeDecisionReason(reason) : null;

        // A real row lock on the relational production store; a no-transaction read on a provider that cannot
        // serialize (the same shape the refund-outcome coordination already uses).
        var relational = _db.Database.IsRelational();
        var ownsTransaction = relational && _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        // The case row is the shared protocol: both decisions serialize here, so the read-then-write of the decision
        // cannot interleave. When this adapter owns the transaction it is committed before any external/money work;
        // inside a caller-owned transaction the lock simply lives until that caller commits.
        if (relational)
        {
            await _db.Database.ExecuteSqlAsync(
                $"SELECT 1 FROM returns.return_requests WHERE return_request_id = {returnRequestId} FOR UPDATE",
                cancellationToken);
        }

        var recorded = await _db.ReturnDecisions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReturnRequestId == returnRequestId, cancellationToken);
        if (recorded is not null)
        {
            await CommitDecisionAsync(transaction, ownsTransaction, cancellationToken);

            if (kind == ReturnDecisionKind.Reject
                && AnswersDecision(recorded, ReturnDecisionKind.Reject)
                && !string.Equals(recorded.Reason, normalizedReason, StringComparison.Ordinal))
            {
                // The winning rejection already stated a different reason. Answering this request would rewrite a
                // durable decision, so it is refused with the exact typed conflict and zero effect.
                throw new ContractOperationException(ReturnsErrorCodes.DecisionReasonConflict);
            }

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
            Reason = normalizedReason,
            DecidedByUserId = actorUserId,
            DecidedAt = _clock.UtcNow,
        };

        var trackedBeforeAttempt = _db.ChangeTracker.Entries().Select(x => x.Entity).ToList();
        _db.ReturnDecisions.Add(decision);

        // TB-274-R1 (D): this attempt's insert lives inside its OWN savepoint, so a key collision can be undone
        // without rolling back the caller's transaction and without touching any caller-tracked entity.
        const string decisionSavePoint = "tb274_decision_intent";
        var hasSavePoint = false;
        if (relational && transaction is not null)
        {
            await _db.Database.ExecuteSqlRawAsync($"SAVEPOINT {decisionSavePoint}", cancellationToken);
            hasSavePoint = true;
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            if (hasSavePoint)
            {
                await _db.Database.ExecuteSqlRawAsync($"RELEASE SAVEPOINT {decisionSavePoint}", cancellationToken);
            }

            await CommitDecisionAsync(transaction, ownsTransaction, cancellationToken);
            return decision;
        }
        catch (DbUpdateException ex) when (IsDecisionKeyConflict(ex))
        {
            // TB-274: ONLY the decision table's own primary key means "another decision won". A different unique
            // constraint (or any other provider failure) is a genuine error and surfaces unchanged.
            if (hasSavePoint)
            {
                await _db.Database.ExecuteSqlRawAsync($"ROLLBACK TO SAVEPOINT {decisionSavePoint}", cancellationToken);
                await _db.Database.ExecuteSqlRawAsync($"RELEASE SAVEPOINT {decisionSavePoint}", cancellationToken);
            }
            else if (ownsTransaction && transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            DetachAttemptEntries(trackedBeforeAttempt);

            var winner = await _db.ReturnDecisions.AsNoTracking()
                .SingleOrDefaultAsync(x => x.ReturnRequestId == returnRequestId, cancellationToken)
                ?? throw new ContractOperationException(ReturnsErrorCodes.Stale);

            if (kind == ReturnDecisionKind.Reject
                && AnswersDecision(winner, ReturnDecisionKind.Reject)
                && !string.Equals(winner.Reason, normalizedReason, StringComparison.Ordinal))
            {
                // The concurrent winner stated a different reason; answering would rewrite a durable decision.
                throw new ContractOperationException(ReturnsErrorCodes.DecisionReasonConflict);
            }

            return winner;
        }
    }

    /// <summary>
    /// TB-274-R1 (D): detaches ONLY the entries this attempt added, so a caller's own tracked entities keep their
    /// instance, their state and their values. There is deliberately no global <c>ChangeTracker.Clear()</c>.
    /// </summary>
    private void DetachAttemptEntries(IReadOnlyList<object> trackedBeforeAttempt)
    {
        foreach (var entry in _db.ChangeTracker.Entries().ToList())
        {
            var existedBefore = trackedBeforeAttempt.Any(x => ReferenceEquals(x, entry.Entity));
            if (!existedBefore && entry.State is EntityState.Added)
            {
                entry.State = EntityState.Detached;
            }
        }
    }

    /// <summary>
    /// TB-274: the exact durable identity of the decision row. Only a unique violation on
    /// <c>pk_return_request_decisions</c> may be arbitrated as "the other decision won"; a lookalike constraint, a
    /// same-named constraint on ANOTHER table/schema, or any other provider error is never reinterpreted.
    /// </summary>
    private const string DecisionPrimaryKeyConstraint = "pk_return_request_decisions";

    private const string DecisionTable = "return_request_decisions";

    private static bool IsDecisionKeyConflict(DbUpdateException exception) =>
        exception.InnerException is Npgsql.PostgresException { SqlState: "23505" } postgres
        && string.Equals(postgres.ConstraintName, DecisionPrimaryKeyConstraint, StringComparison.Ordinal)
        && string.Equals(postgres.TableName, DecisionTable, StringComparison.Ordinal)
        && string.Equals(postgres.SchemaName, ReturnsDbContext.Schema, StringComparison.Ordinal);

    /// <summary>
    /// TB-274: a new rejection requires a meaningful reviewed statement (trimmed, bounded), so the durable decision
    /// always explains itself. The same normalization is used on replay, so equal reasons compare equal.
    /// </summary>
    private static string NormalizeDecisionReason(string? reason)
    {
        var trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > 512)
        {
            throw new ContractOperationException(ReturnsErrorCodes.RejectionReasonRequired);
        }

        return trimmed;
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
