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
/// TB-279 (B): the ONE append-only decision authority of a return case.
/// <para>
/// The legacy <see cref="ReturnDecisionRecord"/> row stays the immutable FIRST decision (version 0). Every later
/// decision — and every audited correction — is a <see cref="ReturnDecisionRevision"/> appended with a monotonic
/// version, the acting operator, a reviewed reason and the exact reviewed occurrence identity. Nothing is deleted,
/// overwritten or backfilled, and the EFFECTIVE decision is the highest revision (or the baseline when none exists).
/// </para>
/// <para>
/// Reads, the work/capability projection and every decision/start/recovery caller consult this same authority. A
/// reviewed revision is fenced by <c>ExpectedVersion</c>: a stale pre-correction review can never approve, reject or
/// start money against a newer revision, and an exact replay answers its own recorded fact.
/// </para>
/// </summary>
public sealed partial class ReturnDirectory
{
    /// <summary>The stored decision state of one case: baseline row (version 0) plus every appended revision.</summary>
    private sealed record DecisionState(
        string? BaselineDecision,
        string? BaselineReason,
        IReadOnlyList<ReturnDecisionRevision> Revisions)
    {
        public string? EffectiveDecision =>
            Revisions.Count == 0 ? BaselineDecision : Revisions[^1].Decision switch
            {
                ReturnDecisionRevision.ReopenDecision => null,
                var decision => decision,
            };

        public int EffectiveVersion => Revisions.Count == 0 ? 0 : Revisions[^1].Version;

        public string? EffectiveReason =>
            Revisions.Count == 0 ? BaselineReason : Revisions[^1].Reason;

        public IReadOnlyList<ReturnDecisionRevisionSnapshot> ToSnapshots() =>
            Revisions.Select(x => new ReturnDecisionRevisionSnapshot(
                x.Version,
                x.Decision,
                x.Reason,
                x.ActorUserId,
                x.RecordedAt)).ToArray();
    }

    private async Task<DecisionState> ReadDecisionStateAsync(
        Guid returnRequestId,
        CancellationToken cancellationToken)
    {
        var baseline = await _db.ReturnDecisions.AsNoTracking()
            .Where(x => x.ReturnRequestId == returnRequestId)
            .Select(x => new { x.Decision, x.Reason })
            .SingleOrDefaultAsync(cancellationToken);
        var revisions = await _db.ReturnDecisionRevisions.AsNoTracking()
            .Where(x => x.ReturnRequestId == returnRequestId)
            .OrderBy(x => x.Version)
            .ToListAsync(cancellationToken);
        return new DecisionState(baseline?.Decision, baseline?.Reason, revisions);
    }

    /// <summary>
    /// Records ONE reviewed decision under the case row lock, appending a revision when the baseline already exists.
    /// <para>
    /// The rules are deliberate: an exact replay of the CURRENT decision answers its own stored fact; the opposite
    /// decision of the current one is a typed refusal (a decided case is never silently flipped); a stale
    /// <paramref name="expectedVersion"/> refuses a pre-correction review; and a NEW decision over an OPEN (reopened)
    /// case binds the CURRENT revision.
    /// </para>
    /// </summary>
    private async Task RecordReviewedDecisionAsync(
        Guid returnRequestId,
        ReturnDecisionKind kind,
        Guid actorUserId,
        string? reason,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var normalizedReason = kind == ReturnDecisionKind.Reject ? NormalizeDecisionReason(reason) : null;
        var relational = _db.Database.IsRelational();
        var ownsTransaction = relational && _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        if (relational)
        {
            await _db.Database.ExecuteSqlAsync(
                $"SELECT 1 FROM returns.return_requests WHERE return_request_id = {returnRequestId} FOR UPDATE",
                cancellationToken);
        }

        var state = await ReadDecisionStateAsync(returnRequestId, cancellationToken);
        var current = await _db.ReturnRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReturnRequestId == returnRequestId, cancellationToken);
        if (current is null)
        {
            await CommitDecisionAsync(transaction, ownsTransaction, cancellationToken);
            throw new ContractOperationException(ReturnsErrorCodes.Missing);
        }

        var kindToken = kind == ReturnDecisionKind.Approve
            ? ReturnDecisionRevision.ApproveDecision
            : ReturnDecisionRevision.RejectDecision;

        if (state.EffectiveDecision is { } effective)
        {
            if (string.Equals(effective, kindToken, StringComparison.Ordinal))
            {
                // The SAME decision is already the effective one. A changed rejection reason would rewrite a durable
                // decision, so it is the exact typed conflict; otherwise this is an exact replay with zero effect.
                if (kind == ReturnDecisionKind.Reject
                    && !string.Equals(state.EffectiveReason, normalizedReason, StringComparison.Ordinal))
                {
                    await CommitDecisionAsync(transaction, ownsTransaction, cancellationToken);
                    throw new ContractOperationException(ReturnsErrorCodes.DecisionReasonConflict);
                }

                await CommitDecisionAsync(transaction, ownsTransaction, cancellationToken);
                return;
            }

            // The opposite decision is effective. Answering would release/refund capacity another authority owns.
            await CommitDecisionAsync(transaction, ownsTransaction, cancellationToken);
            throw new ContractOperationException(kind == ReturnDecisionKind.Approve
                ? ReturnsErrorCodes.AlreadyRejected
                : ReturnsErrorCodes.AlreadyApproved);
        }

        // An OPEN case (no effective decision: freshly requested or corrected back to review) requires the review to
        // name the revision it looked at. A stale review is refused instead of deciding over a newer revision.
        if (expectedVersion is { } expected && expected != state.EffectiveVersion)
        {
            await CommitDecisionAsync(transaction, ownsTransaction, cancellationToken);
            throw new ContractOperationException(ReturnsErrorCodes.CorrectionVersionStale);
        }

        var conflictsWithCurrentState = kind == ReturnDecisionKind.Approve
            ? current.Status is ReturnRequestStatus.Rejected or ReturnRequestStatus.Cancelled
            : current.Status is ReturnRequestStatus.Approved
                or ReturnRequestStatus.RefundProcessing
                or ReturnRequestStatus.RefundFailed
                or ReturnRequestStatus.Completed;
        if (conflictsWithCurrentState)
        {
            await CommitDecisionAsync(transaction, ownsTransaction, cancellationToken);
            throw new ContractOperationException(kind == ReturnDecisionKind.Approve
                ? ReturnsErrorCodes.AlreadyRejected
                : ReturnsErrorCodes.AlreadyApproved);
        }

        var now = _clock.UtcNow;
        // Only the entries THIS attempt adds may be detached on a collision; a caller's own tracked entities keep
        // their instance/state/values (TB-274-R1 D), so the tracker is never cleared.
        var trackedBeforeAttempt = _db.ChangeTracker.Entries().Select(x => x.Entity).ToList();
        const string decisionSavePoint = "tb279_decision_intent";
        var hasSavePoint = false;
        if (relational && transaction is not null)
        {
            await _db.Database.ExecuteSqlRawAsync($"SAVEPOINT {decisionSavePoint}", cancellationToken);
            hasSavePoint = true;
        }

        try
        {
            if (state.BaselineDecision is null)
            {
                // The FIRST decision of the case is the legacy baseline row (version 0), exactly as shipped.
                _db.ReturnDecisions.Add(new ReturnDecisionRecord
                {
                    ReturnRequestId = returnRequestId,
                    Decision = kindToken,
                    Reason = normalizedReason,
                    DecidedByUserId = actorUserId,
                    DecidedAt = now,
                });
            }
            else
            {
                _db.ReturnDecisionRevisions.Add(new ReturnDecisionRevision
                {
                    ReturnRequestId = returnRequestId,
                    Version = state.EffectiveVersion + 1,
                    Decision = kindToken,
                    Reason = normalizedReason,
                    ActorUserId = actorUserId,
                    OccurrenceKey = BuildDecisionOccurrenceKey(kindToken, expectedVersion, normalizedReason),
                    RecordedAt = now,
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            if (hasSavePoint)
            {
                await _db.Database.ExecuteSqlRawAsync($"RELEASE SAVEPOINT {decisionSavePoint}", cancellationToken);
            }

            await CommitDecisionAsync(transaction, ownsTransaction, cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDecisionKeyConflict(ex) || IsRevisionKeyConflict(ex))
        {
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

            // A concurrent writer won the same version/baseline. Re-read and answer the SAME rules against the
            // recorded fact; a genuinely different state is a typed stale refusal, never a silent second write.
            var winner = await ReadDecisionStateAsync(returnRequestId, cancellationToken);
            if (string.Equals(winner.EffectiveDecision, kindToken, StringComparison.Ordinal))
            {
                if (kind == ReturnDecisionKind.Reject
                    && !string.Equals(winner.EffectiveReason, normalizedReason, StringComparison.Ordinal))
                {
                    throw new ContractOperationException(ReturnsErrorCodes.DecisionReasonConflict);
                }

                return;
            }

            if (winner.EffectiveDecision is not null)
            {
                throw new ContractOperationException(kind == ReturnDecisionKind.Approve
                    ? ReturnsErrorCodes.AlreadyRejected
                    : ReturnsErrorCodes.AlreadyApproved);
            }

            throw new ContractOperationException(ReturnsErrorCodes.CorrectionVersionStale);
        }
    }

    /// <summary>
    /// The reviewed occurrence identity of a decision that must be attributable even though the operator path does not
    /// carry a client key: the exact effective version it was reviewed against, the decision token and the normalized
    /// reason. It is unique per case because the version is monotonic.
    /// </summary>
    private static string BuildDecisionOccurrenceKey(string decision, int? expectedVersion, string? reason) =>
        $"decision:{decision}:{expectedVersion ?? -1}:{(reason is null ? "-" : reason.GetHashCode().ToString("X8"))}";

    private static bool IsRevisionKeyConflict(DbUpdateException exception) =>
        exception.InnerException is Npgsql.PostgresException { SqlState: "23505" } postgres
        && string.Equals(postgres.TableName, "return_request_decision_revisions", StringComparison.Ordinal)
        && string.Equals(postgres.SchemaName, ReturnsDbContext.Schema, StringComparison.Ordinal);

    /// <inheritdoc />
    public async Task<ReturnSnapshot> ReopenForReviewAsync(
        ReopenReturnForReviewCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await _guard.EnsureCanMutateAsync(cancellationToken);

        var occurrenceKey = NormalizeReviewedOccurrenceKey(command.OccurrenceKey);
        var reason = NormalizeCorrectionReason(command.Reason);
        if (command.ActorUserId == Guid.Empty)
        {
            throw new ContractOperationException(ReturnsErrorCodes.CorrectionActorRequired);
        }

        // TB-279 (B): the correction and a concurrent first-refund serialize on the SAME case row and the SAME owning
        // seller order. No external call is made inside this transaction and it commits before anything else runs.
        var relational = _db.Database.IsRelational();
        var ownsTransaction = relational && _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            if (relational)
            {
                // The seller-order serialization is the SAME critical section creation uses; the owning order is read
                // first (without a lock) only to address it, then the case row lock is taken and every fact re-read.
                var owningSellerOrderId = await _db.ReturnRequests.AsNoTracking()
                    .Where(x => x.ReturnRequestId == command.ReturnRequestId)
                    .Select(x => (Guid?)x.SellerOrderId)
                    .SingleOrDefaultAsync(cancellationToken);
                if (owningSellerOrderId is { } sellerOrderId)
                {
                    await ReturnCreationSerialization.AcquireAsync(
                        _db.Database, sellerOrderId, cancellationToken);
                }

                await _db.Database.ExecuteSqlAsync(
                    $"SELECT 1 FROM returns.return_requests WHERE return_request_id = {command.ReturnRequestId} FOR UPDATE",
                    cancellationToken);
            }

            var request = await LoadMutableAsync(command.ReturnRequestId, cancellationToken);
            EnsureOwningSellerScope(request, command.SellerPartyId);

            var state = await ReadDecisionStateAsync(request.ReturnRequestId, cancellationToken);
            var replay = state.Revisions.FirstOrDefault(x =>
                string.Equals(x.OccurrenceKey, occurrenceKey, StringComparison.Ordinal));
            if (replay is not null)
            {
                if (!string.Equals(replay.Decision, ReturnDecisionRevision.ReopenDecision, StringComparison.Ordinal)
                    || !string.Equals(replay.Reason, reason, StringComparison.Ordinal))
                {
                    throw new ContractOperationException(ReturnsErrorCodes.CorrectionOccurrenceConflict);
                }

                await CommitCorrectionAsync(transaction, ownsTransaction, cancellationToken);
                return await MapSnapshotAsync(request, cancellationToken);
            }

            if (command.ExpectedDecisionVersion is { } expected && expected != state.EffectiveVersion)
            {
                throw new ContractOperationException(ReturnsErrorCodes.CorrectionVersionStale);
            }

            var snapshot = await MapSnapshotAsync(request, cancellationToken);
            var assessment = ReturnCorrectionPolicy.Describe(snapshot);
            if (!assessment.CanReopenForReview)
            {
                throw new ContractOperationException(
                    assessment.BlockerCode ?? ReturnsErrorCodes.CorrectionNotApplicable);
            }

            var now = _clock.UtcNow;
            var wasRejected = request.Status == ReturnRequestStatus.Rejected;

            // The request itself goes back to review (its previous decision row/revisons are untouched) ...
            request.ReopenForReview(now);

            // ... and a REJECTED claim re-acquires the original capacity it released, under this lock. Another case
            // may have occupied it meanwhile: that is the exact typed conflict, never a silent overrun.
            await EnsureDeliveryAllocationsAsync(request, release: false, cancellationToken);
            if (wasRejected)
            {
                // The re-acquired capacity is compared against every OTHER case only: this case's own quantity is the
                // capacity being re-acquired, so counting it here would refuse every lawful correction.
                var eligibility = await _eligibility.EvaluateAsync(
                    request.SellerOrderId,
                    cancellationToken,
                    excludeReturnRequestId: request.ReturnRequestId);
                foreach (var item in request.Items)
                {
                    var line = eligibility.Lines.FirstOrDefault(x => x.OrderLineId == item.OrderLineId);
                    if (line is null || line.RemainingReturnableQuantity < item.Quantity)
                    {
                        throw new ContractOperationException(ReturnsErrorCodes.CorrectionCapacityConflict);
                    }
                }
            }

            _db.ReturnDecisionRevisions.Add(new ReturnDecisionRevision
            {
                ReturnRequestId = request.ReturnRequestId,
                Version = state.EffectiveVersion + 1,
                Decision = ReturnDecisionRevision.ReopenDecision,
                Reason = reason,
                ActorUserId = command.ActorUserId,
                OccurrenceKey = occurrenceKey,
                RecordedAt = now,
            });

            await _db.SaveChangesAsync(cancellationToken);
            await CommitCorrectionAsync(transaction, ownsTransaction, cancellationToken);
            return await MapSnapshotAsync(request, cancellationToken);
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

    private static string NormalizeReviewedOccurrenceKey(string? occurrenceKey)
    {
        var trimmed = occurrenceKey?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > 128)
        {
            throw new ContractOperationException(ReturnsErrorCodes.IdempotencyRequired);
        }

        return trimmed;
    }

    private static string NormalizeCorrectionReason(string? reason)
    {
        var trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > 512)
        {
            throw new ContractOperationException(ReturnsErrorCodes.CorrectionReasonRequired);
        }

        return trimmed;
    }

    private static async Task CommitCorrectionAsync(
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
