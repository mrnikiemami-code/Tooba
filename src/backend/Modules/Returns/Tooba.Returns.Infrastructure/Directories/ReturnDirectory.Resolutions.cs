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
/// TB-279 (C): the reviewed per-item/condition quantity resolution of an ordinary legacy multi-item case, and the
/// audited physical handback of rejected received goods.
/// <para>
/// The originally requested lines, quantities and amount stay immutable audit facts. A resolution states, for ONE
/// requested line, how much is ACCEPTED (refundable) and how much is REJECTED/withdrawn; the approved money is derived
/// by <see cref="ReturnResolutionPolicy"/> from the accepted quantities and each line's STORED unit price only. The plan
/// is published BEFORE the approval decision, under the case row lock, and is never accepted after money started.
/// </para>
/// </summary>
public sealed partial class ReturnDirectory
{
    /// <inheritdoc />
    public async Task<ReturnSnapshot> PublishResolutionAsync(
        PublishReturnResolutionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await _guard.EnsureCanMutateAsync(cancellationToken);

        var occurrenceKey = NormalizeReviewedOccurrenceKey(command.OccurrenceKey);
        if (command.ActorUserId == Guid.Empty)
        {
            throw new ContractOperationException(ReturnsErrorCodes.ResolutionActorRequired);
        }

        if (command.Lines.Count == 0)
        {
            throw new ContractOperationException(ReturnsErrorCodes.ResolutionIncomplete);
        }

        var relational = _db.Database.IsRelational();
        var ownsTransaction = relational && _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            if (relational)
            {
                await LockOwningSellerOrderAsync(command.ReturnRequestId, cancellationToken);
                await _db.Database.ExecuteSqlAsync(
                    $"SELECT 1 FROM returns.return_requests WHERE return_request_id = {command.ReturnRequestId} FOR UPDATE",
                    cancellationToken);
            }

            var request = await LoadMutableAsync(command.ReturnRequestId, cancellationToken);
            EnsureOwningSellerScope(request, command.SellerPartyId);

            var existing = await _db.ReturnItemResolutions.AsNoTracking()
                .Where(x => x.ReturnRequestId == request.ReturnRequestId)
                .ToListAsync(cancellationToken);

            // An exact replay of the SAME reviewed occurrence answers its stored plan with zero effect.
            var replayVersion = existing
                .Where(x => string.Equals(x.OccurrenceKey, occurrenceKey, StringComparison.Ordinal))
                .Select(x => (int?)x.Version)
                .FirstOrDefault();
            if (replayVersion is not null)
            {
                var stored = existing.Where(x => x.Version == replayVersion.Value).ToArray();
                EnsureSameResolutionPlan(stored, command);
                await CommitCorrectionAsync(transaction, ownsTransaction, cancellationToken);
                return await MapSnapshotAsync(request, cancellationToken);
            }

            var currentVersion = existing.Count == 0 ? 0 : existing.Max(x => x.Version);
            if (command.ExpectedResolutionVersion != currentVersion)
            {
                throw new ContractOperationException(ReturnsErrorCodes.ResolutionVersionStale);
            }

            // The plan is a REVIEW fact: it is published while the case is still in review, before any decision or
            // money/physical effect. A started refund is never re-described by a new plan.
            if (request.Status != ReturnRequestStatus.Requested
                || request.RefundAttempts.Count > 0
                || request.Items.Any(x => x.Disposition != ReturnItemDisposition.None))
            {
                throw new ContractOperationException(ReturnsErrorCodes.ResolutionStateInvalid);
            }

            var receiving = await ReadReceivingAsync(request, cancellationToken);
            if (receiving is not { IsCoherent: true })
            {
                throw new ContractOperationException(ReturnsErrorCodes.RecoveryEvidenceConflict);
            }

            var itemsById = request.Items.ToDictionary(x => x.ReturnItemId);
            var seen = new HashSet<Guid>();
            var version = currentVersion + 1;
            var rows = new List<ReturnItemResolution>(command.Lines.Count);
            foreach (var line in command.Lines)
            {
                if (!itemsById.TryGetValue(line.ReturnItemId, out var item) || !seen.Add(line.ReturnItemId))
                {
                    throw new ContractOperationException(ReturnsErrorCodes.ResolutionItemInvalid);
                }

                if (line.AcceptedQuantity < 0m
                    || line.RejectedQuantity < 0m
                    || !PersistedReturnQuantity.IsExactlyRepresentable(line.AcceptedQuantity)
                    || !PersistedReturnQuantity.IsExactlyRepresentable(line.RejectedQuantity))
                {
                    throw new ContractOperationException(ReturnsErrorCodes.ResolutionInvalid);
                }

                // The whole original claim is accounted for: nothing is silently refunded and nothing is silently kept.
                if (line.AcceptedQuantity + line.RejectedQuantity != item.Quantity)
                {
                    throw new ContractOperationException(ReturnsErrorCodes.ResolutionIncomplete);
                }

                // An accepted quantity must have real physical evidence; an unreceived unit is never refunded.
                var received = receiving.Lines.FirstOrDefault(x => x.ReturnItemId == item.ReturnItemId);
                if (received is null || line.AcceptedQuantity > received.Received)
                {
                    throw new ContractOperationException(ReturnsErrorCodes.ResolutionReceivedExceeded);
                }

                var reason = line.Reason?.Trim();
                if (string.IsNullOrEmpty(reason) || reason.Length > 512)
                {
                    throw new ContractOperationException(ReturnsErrorCodes.RejectionReasonRequired);
                }

                rows.Add(new ReturnItemResolution
                {
                    ReturnRequestId = request.ReturnRequestId,
                    ReturnItemId = item.ReturnItemId,
                    Version = version,
                    AcceptedQuantity = line.AcceptedQuantity,
                    RejectedQuantity = line.RejectedQuantity,
                    Reason = reason,
                    ActorUserId = command.ActorUserId,
                    OccurrenceKey = occurrenceKey,
                    RecordedAt = _clock.UtcNow,
                });
            }

            if (seen.Count != request.Items.Count)
            {
                // A plan that resolves only some lines is not a reviewed whole-case decision.
                throw new ContractOperationException(ReturnsErrorCodes.ResolutionIncomplete);
            }

            _db.ReturnItemResolutions.AddRange(rows);
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

    /// <inheritdoc />
    public async Task<ReturnSnapshot> RecordCustodyAsync(
        RecordReturnCustodyCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await _guard.EnsureCanMutateAsync(cancellationToken);

        var occurrenceKey = NormalizeReviewedOccurrenceKey(command.OccurrenceKey);
        if (command.ActorUserId == Guid.Empty)
        {
            throw new ContractOperationException(ReturnsErrorCodes.CustodyActorRequired);
        }

        var proof = command.ProofReference?.Trim();
        if (string.IsNullOrEmpty(proof) || proof.Length > 256)
        {
            // A tracking number alone is not delivery proof, so a meaningless reference is refused here.
            throw new ContractOperationException(ReturnsErrorCodes.CustodyProofRequired);
        }

        if (command.Condition is not (ReturnItemCondition.Usable or ReturnItemCondition.Damaged))
        {
            throw new ContractOperationException(ReturnsErrorCodes.CustodyQuantityInvalid);
        }

        if (!PersistedReturnQuantity.IsExactlyRepresentable(command.Quantity) || command.Quantity <= 0m)
        {
            throw new ContractOperationException(ReturnsErrorCodes.CustodyQuantityInvalid);
        }

        var relational = _db.Database.IsRelational();
        var ownsTransaction = relational && _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            if (relational)
            {
                await LockOwningSellerOrderAsync(command.ReturnRequestId, cancellationToken);
                await _db.Database.ExecuteSqlAsync(
                    $"SELECT 1 FROM returns.return_requests WHERE return_request_id = {command.ReturnRequestId} FOR UPDATE",
                    cancellationToken);
            }

            var request = await LoadMutableAsync(command.ReturnRequestId, cancellationToken);
            EnsureOwningSellerScope(request, command.SellerPartyId);

            var replay = await _db.ReturnItemCustodies.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.ReturnRequestId == request.ReturnRequestId
                        && x.OccurrenceKey == occurrenceKey,
                    cancellationToken);
            if (replay is not null)
            {
                if (replay.ReturnItemId != command.ReturnItemId
                    || replay.Quantity != command.Quantity
                    || replay.Condition != command.Condition.ToString()
                    || !string.Equals(replay.ProofReference, proof, StringComparison.Ordinal))
                {
                    throw new ContractOperationException(ReturnsErrorCodes.CustodyOccurrenceConflict);
                }

                await CommitCorrectionAsync(transaction, ownsTransaction, cancellationToken);
                return await MapSnapshotAsync(request, cancellationToken);
            }

            var snapshot = await MapSnapshotAsync(request, cancellationToken);
            if (snapshot.Resolution is not { IsComplete: true } resolution)
            {
                throw new ContractOperationException(ReturnsErrorCodes.CustodyStateInvalid);
            }

            var line = resolution.Lines.FirstOrDefault(x => x.ReturnItemId == command.ReturnItemId)
                ?? throw new ContractOperationException(ReturnsErrorCodes.CustodyItemInvalid);
            var owed = ReturnResolutionPolicy.OwedCustody(line, snapshot.Custody);
            if (owed <= 0m)
            {
                throw new ContractOperationException(ReturnsErrorCodes.CustodyOverHanded);
            }

            if (command.Quantity > owed)
            {
                throw new ContractOperationException(ReturnsErrorCodes.CustodyQuantityInvalid);
            }

            if (line.RejectedReceived <= 0m || request.RequestedByUserId == Guid.Empty)
            {
                throw new ContractOperationException(ReturnsErrorCodes.CustodyRecipientInvalid);
            }

            _db.ReturnItemCustodies.Add(new ReturnItemCustody
            {
                ReturnCustodyId = _ids.NewId(),
                ReturnRequestId = request.ReturnRequestId,
                ReturnItemId = command.ReturnItemId,
                Quantity = command.Quantity,
                Condition = command.Condition.ToString(),
                // The owning customer is the ONLY recipient of a handback; the server derives it from the case.
                RecipientUserId = request.RequestedByUserId,
                ProofReference = proof,
                ActorUserId = command.ActorUserId,
                OccurrenceKey = occurrenceKey,
                RecordedAt = _clock.UtcNow,
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
    /// TB-279: the seller-order advisory lock of the SAME critical section creation/correction use, addressed from the
    /// case identity without loading it (the row lock is taken immediately afterwards).
    /// </summary>
    private async Task LockOwningSellerOrderAsync(Guid returnRequestId, CancellationToken cancellationToken)
    {
        var owningSellerOrderId = await _db.ReturnRequests.AsNoTracking()
            .Where(x => x.ReturnRequestId == returnRequestId)
            .Select(x => (Guid?)x.SellerOrderId)
            .SingleOrDefaultAsync(cancellationToken);
        if (owningSellerOrderId is { } sellerOrderId)
        {
            await ReturnCreationSerialization.AcquireAsync(_db.Database, sellerOrderId, cancellationToken);
        }
    }

    private static void EnsureSameResolutionPlan(
        IReadOnlyList<ReturnItemResolution> stored,
        PublishReturnResolutionCommand command)
    {
        if (stored.Count != command.Lines.Count)
        {
            throw new ContractOperationException(ReturnsErrorCodes.ResolutionVersionStale);
        }

        foreach (var line in command.Lines)
        {
            var match = stored.FirstOrDefault(x => x.ReturnItemId == line.ReturnItemId);
            if (match is null
                || match.AcceptedQuantity != line.AcceptedQuantity
                || match.RejectedQuantity != line.RejectedQuantity
                || !string.Equals(match.Reason, line.Reason?.Trim(), StringComparison.Ordinal))
            {
                throw new ContractOperationException(ReturnsErrorCodes.ResolutionVersionStale);
            }
        }
    }
}
