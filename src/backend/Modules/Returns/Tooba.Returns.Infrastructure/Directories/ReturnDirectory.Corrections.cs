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

    /// <summary>What appending a reviewed decision did: it wrote a NEW decision, or it answered an exact replay.</summary>
    private enum DecisionAppend
    {
        Recorded = 0,
        Replayed = 1,
    }

    /// <summary>
    /// TB-279-R1 (F4): appends ONE reviewed decision INSIDE the caller's already-locked short transaction.
    /// <para>
    /// This method owns no transaction and never commits: the caller persists the case transition, the frozen approved
    /// amount and any capacity change in the SAME transaction, so a decision can never commit and leave its state change
    /// to a later unprotected write. It assumes the case row lock is held and the tracked case was re-read after that
    /// lock.
    /// </para>
    /// <para>
    /// The rules are the shipped ones: an exact replay of the CURRENT decision answers its own stored fact; the opposite
    /// decision of the current one is a typed refusal; a stale <paramref name="expectedVersion"/> refuses a
    /// pre-correction review; and a NEW decision over an OPEN (reopened) case binds the CURRENT revision.
    /// </para>
    /// <para>
    /// TB-274-R1 (D) is preserved: the insert runs inside its own savepoint, so a concurrent winner that already owns
    /// the baseline/version row is arbitrated (rollback to the savepoint, detach ONLY this attempt's entries) instead of
    /// aborting the caller's transaction or clearing its tracker.
    /// </para>
    /// </summary>
    private async Task<DecisionAppend> AppendReviewedDecisionAsync(
        ReturnRequest request,
        ReturnDecisionKind kind,
        Guid actorUserId,
        string? reason,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var normalizedReason = kind == ReturnDecisionKind.Reject ? NormalizeDecisionReason(reason) : null;
        var state = await ReadDecisionStateAsync(request.ReturnRequestId, cancellationToken);
        var existing = ResolveExistingDecision(state, kind, normalizedReason, expectedVersion);
        if (existing is not null)
        {
            return existing.Value;
        }

        var now = _clock.UtcNow;
        var relational = _db.Database.IsRelational();
        const string decisionSavePoint = "tb279_decision_intent";
        var hasSavePoint = false;
        if (relational && _db.Database.CurrentTransaction is not null)
        {
            await _db.Database.ExecuteSqlRawAsync($"SAVEPOINT {decisionSavePoint}", cancellationToken);
            hasSavePoint = true;
        }

        // Only the entries THIS attempt adds may be detached on a collision; a caller's own tracked entities keep their
        // instance/state/values, so the tracker is never cleared.
        var trackedBeforeAttempt = _db.ChangeTracker.Entries().Select(x => x.Entity).ToList();
        try
        {
            if (state.BaselineDecision is null)
            {
                // The FIRST decision of the case is the legacy baseline row (version 0), exactly as shipped.
                _db.ReturnDecisions.Add(new ReturnDecisionRecord
                {
                    ReturnRequestId = request.ReturnRequestId,
                    Decision = DecisionTokenOf(kind),
                    Reason = normalizedReason,
                    DecidedByUserId = actorUserId,
                    DecidedAt = now,
                });
            }
            else
            {
                _db.ReturnDecisionRevisions.Add(new ReturnDecisionRevision
                {
                    ReturnRequestId = request.ReturnRequestId,
                    Version = state.EffectiveVersion + 1,
                    Decision = DecisionTokenOf(kind),
                    Reason = normalizedReason,
                    ActorUserId = actorUserId,
                    OccurrenceKey = BuildDecisionOccurrenceKey(DecisionTokenOf(kind), state.EffectiveVersion, normalizedReason),
                    RecordedAt = now,
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            if (hasSavePoint)
            {
                await _db.Database.ExecuteSqlRawAsync($"RELEASE SAVEPOINT {decisionSavePoint}", cancellationToken);
            }

            return DecisionAppend.Recorded;
        }
        catch (DbUpdateException ex) when (IsDecisionKeyConflict(ex) || IsRevisionKeyConflict(ex))
        {
            if (hasSavePoint)
            {
                await _db.Database.ExecuteSqlRawAsync($"ROLLBACK TO SAVEPOINT {decisionSavePoint}", cancellationToken);
                await _db.Database.ExecuteSqlRawAsync($"RELEASE SAVEPOINT {decisionSavePoint}", cancellationToken);
            }

            DetachAttemptEntries(trackedBeforeAttempt);

            // A concurrent writer won the same version/baseline. Re-read and answer the SAME rules against the recorded
            // fact; a genuinely different state is a typed stale refusal, never a silent second write.
            var winner = await ReadDecisionStateAsync(request.ReturnRequestId, cancellationToken);
            return ResolveExistingDecision(winner, kind, normalizedReason, expectedVersion)
                ?? throw new ContractOperationException(ReturnsErrorCodes.CorrectionVersionStale);
        }
    }

    /// <summary>
    /// The decision rules applied to the ALREADY stored state: an exact replay answers its own fact, the opposite
    /// decision is a typed refusal, a stale reviewed version is refused, and a decided/open state that cannot change is
    /// refused. <c>null</c> means "no decision is recorded yet — write it".
    /// </summary>
    private static DecisionAppend? ResolveExistingDecision(
        DecisionState state,
        ReturnDecisionKind kind,
        string? normalizedReason,
        int? expectedVersion)
    {
        var kindToken = DecisionTokenOf(kind);

        if (state.EffectiveDecision is { } effective)
        {
            if (string.Equals(effective, kindToken, StringComparison.Ordinal))
            {
                // The SAME decision is already the effective one. A changed rejection reason would rewrite a durable
                // decision, so it is the exact typed conflict; otherwise this is an exact replay with zero effect.
                if (kind == ReturnDecisionKind.Reject
                    && !string.Equals(state.EffectiveReason, normalizedReason, StringComparison.Ordinal))
                {
                    throw new ContractOperationException(ReturnsErrorCodes.DecisionReasonConflict);
                }

                return DecisionAppend.Replayed;
            }

            // The opposite decision is effective. Answering would release/refund capacity another authority owns.
            throw new ContractOperationException(kind == ReturnDecisionKind.Approve
                ? ReturnsErrorCodes.AlreadyRejected
                : ReturnsErrorCodes.AlreadyApproved);
        }

        // An OPEN case (no effective decision: freshly requested or corrected back to review) requires the review to
        // name the revision it looked at. A stale review is refused instead of deciding over a newer revision.
        if (expectedVersion is { } expected && expected != state.EffectiveVersion)
        {
            throw new ContractOperationException(ReturnsErrorCodes.CorrectionVersionStale);
        }

        return null;
    }

    private static string DecisionTokenOf(ReturnDecisionKind kind) =>
        kind == ReturnDecisionKind.Approve
            ? ReturnDecisionRevision.ApproveDecision
            : ReturnDecisionRevision.RejectDecision;

    /// <summary>
    /// TB-279-R1 (F4): the reviewed occurrence identity of a decision that must be attributable even though the operator
    /// path does not carry a client key: the exact effective version it was reviewed against, the decision token and the
    /// normalized reason. It is unique per case because the version is monotonic.
    /// <para>
    /// The reason is hashed with SHA-256, never <see cref="object.GetHashCode"/>: the occurrence key is a DURABLE
    /// identity, so it must be identical across process restarts and machines.
    /// </para>
    /// </summary>
    private static string BuildDecisionOccurrenceKey(string decision, int? expectedVersion, string? reason)
    {
        var payload = $"decision|{decision}|{expectedVersion ?? -1}|{reason ?? "-"}";
        var digest = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload));
        return $"decision:{decision}:{expectedVersion ?? -1}:{Convert.ToHexString(digest, 0, 8)}";
    }

    /// <summary>The exact provider collision shape that means "another writer owns this decision version".</summary>
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

    /// <summary>
    /// TB-279-R1 (F4): the ONE short critical section a reviewed decision transition commits in.
    /// <para>
    /// The seller-order serialization is the same critical section creation uses, the case row is locked, and the
    /// TRACKED case is re-read AFTER that lock so a pre-lock tracked value can never overwrite a newer correction. The
    /// body runs inside the caller's transaction: the decision, the case transition, the frozen approved amount and the
    /// capacity release either commit together or not at all. No external port is called from here.
    /// </para>
    /// </summary>
    private async Task<TResult> InLockedDecisionScopeAsync<TResult>(
        ReturnRequest request,
        Func<Task<TResult>> body,
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
                await ReturnCreationSerialization.AcquireAsync(_db.Database, request.SellerOrderId, cancellationToken);
                await _db.Database.ExecuteSqlAsync(
                    $"SELECT 1 FROM returns.return_requests WHERE return_request_id = {request.ReturnRequestId} FOR UPDATE",
                    cancellationToken);
                await _db.Entry(request).ReloadAsync(cancellationToken);
            }

            var result = await body();

            if (ownsTransaction && transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return result;
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
