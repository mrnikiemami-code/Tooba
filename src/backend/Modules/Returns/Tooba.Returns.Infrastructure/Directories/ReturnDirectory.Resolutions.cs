using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Returns.Application.Composition;
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

                // TB-279-R1 (F2): every portion is REQUIRED. A null portion is refused, never read as zero, because the
                // accepted CONDITION is the operator's decision and can never be derived from the received quantities.
                if (line.AcceptedUsable is not { } acceptedUsable
                    || line.AcceptedDamaged is not { } acceptedDamaged
                    || line.RejectedReceivedUsable is not { } rejectedReceivedUsable
                    || line.RejectedReceivedDamaged is not { } rejectedReceivedDamaged
                    || line.RejectedUnreceived is not { } rejectedUnreceived)
                {
                    throw new ContractOperationException(ReturnsErrorCodes.ResolutionPortionRequired);
                }

                var portions = new[]
                {
                    acceptedUsable,
                    acceptedDamaged,
                    rejectedReceivedUsable,
                    rejectedReceivedDamaged,
                    rejectedUnreceived,
                };
                if (portions.Any(value => value < 0m || !PersistedReturnQuantity.IsExactlyRepresentable(value)))
                {
                    throw new ContractOperationException(ReturnsErrorCodes.ResolutionInvalid);
                }

                var accepted = acceptedUsable + acceptedDamaged;
                var rejectedReceived = rejectedReceivedUsable + rejectedReceivedDamaged;
                var rejected = rejectedReceived + rejectedUnreceived;

                // The whole original claim is accounted for: nothing is silently refunded and nothing is silently kept.
                if (accepted + rejected != item.Quantity)
                {
                    throw new ContractOperationException(ReturnsErrorCodes.ResolutionIncomplete);
                }

                var received = receiving.Lines.FirstOrDefault(x => x.ReturnItemId == item.ReturnItemId);
                if (received is null)
                {
                    throw new ContractOperationException(ReturnsErrorCodes.ResolutionReceivedExceeded);
                }

                // Each condition is bounded by ITS OWN inspected quantity: a usable unit can never stand in for a
                // damaged one, and an accepted unit that never arrived is not a refund.
                if (acceptedUsable > received.Usable
                    || acceptedDamaged > received.Damaged
                    || rejectedReceivedUsable > received.Usable - acceptedUsable
                    || rejectedReceivedDamaged > received.Damaged - acceptedDamaged)
                {
                    throw new ContractOperationException(ReturnsErrorCodes.ResolutionReceivedExceeded);
                }

                if (rejectedUnreceived != item.Quantity - received.Received)
                {
                    // The never-received remainder is a physical fact, not a free choice.
                    throw new ContractOperationException(ReturnsErrorCodes.ResolutionPortionInvalid);
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
                    AcceptedQuantity = accepted,
                    RejectedQuantity = rejected,
                    AcceptedUsableQuantity = acceptedUsable,
                    AcceptedDamagedQuantity = acceptedDamaged,
                    RejectedReceivedUsableQuantity = rejectedReceivedUsable,
                    RejectedReceivedDamagedQuantity = rejectedReceivedDamaged,
                    RejectedUnreceivedQuantity = rejectedUnreceived,
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

        // TB-279-R1 (F3): the evidence behind the handback is explicit. A missing or unknown kind is refused — never
        // defaulted to an operator attestation — so a later reader can tell which proof the custody fact rests on.
        var proofKind = ResolveCustodyProofKind(command.ProofKind);

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
                    || !string.Equals(replay.ProofReference, proof, StringComparison.Ordinal)
                    || !string.Equals(replay.ProofKind, proofKind, StringComparison.Ordinal))
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

            // TB-279-R1 (F3): the ceiling is the outstanding handback of THIS condition. A usable handback can never
            // discharge a damaged obligation, and no handback may exceed what the reviewed plan says was declined.
            var owed = ReturnResolutionPolicy.OwedCustodyOfCondition(line, snapshot.Custody, command.Condition);
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

            var recordedAt = _clock.UtcNow;
            var custodyId = _ids.NewId();
            var custodyFact = new ReturnCustodySnapshot(
                custodyId,
                command.ReturnItemId,
                command.Quantity,
                command.Condition,
                request.RequestedByUserId,
                proof,
                recordedAt,
                proofKind);
            _db.ReturnItemCustodies.Add(new ReturnItemCustody
            {
                ReturnCustodyId = custodyId,
                ReturnRequestId = request.ReturnRequestId,
                ReturnItemId = command.ReturnItemId,
                Quantity = command.Quantity,
                Condition = command.Condition.ToString(),
                // The owning customer is the ONLY recipient of a handback; the server derives it from the case.
                RecipientUserId = request.RequestedByUserId,
                ProofReference = proof,
                ProofKind = proofKind,
                ActorUserId = command.ActorUserId,
                OccurrenceKey = occurrenceKey,
                RecordedAt = recordedAt,
            });

            await _db.SaveChangesAsync(cancellationToken);

            // TB-279-R1 (F3): the handed-back units are no longer held, so the line's delivery claim shrinks by exactly
            // that portion. A partial handback therefore releases only its own part and a fully handed-back line
            // returns the rest to the pool — while the seller-order serialization above keeps this atomic with the
            // custody fact itself.
            var item = request.Items.SingleOrDefault(x => x.ReturnItemId == command.ReturnItemId);
            if (item is not null)
            {
                var custodyAfter = snapshot.Custody.Append(custodyFact).ToArray();
                await TrimActiveAllocationsToHeldAsync(
                    request.ReturnRequestId,
                    item,
                    ReturnResolutionPolicy.OwedCustody(line, custodyAfter),
                    allocateWhenMissing: false,
                    cancellationToken);
            }

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

    /// <summary>
    /// TB-279-R1 (F3): the stored proof kind of a handback. The Application parser already answered the wire token, but
    /// the directory is also reachable from internal adapters, so it normalizes and validates again instead of trusting
    /// the caller. An omitted or unknown kind can never become an operator attestation.
    /// </summary>
    private static string ResolveCustodyProofKind(string? proofKind)
    {
        var token = proofKind?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(token))
        {
            throw new ContractOperationException(ReturnsErrorCodes.CustodyProofKindRequired);
        }

        return token switch
        {
            ReturnCustodyProofKindParser.OperatorAttestation => ReturnCustodyProofKindParser.OperatorAttestation,
            ReturnCustodyProofKindParser.CarrierDeliveredBack => ReturnCustodyProofKindParser.CarrierDeliveredBack,
            _ => throw new ContractOperationException(ReturnsErrorCodes.CustodyProofKindUnavailable),
        };
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
            // TB-279-R1 (F2): the replay compares the SAME per-condition identity that was published, so two different
            // splits under one occurrence key can never answer as "the same reviewed plan".
            var match = stored.FirstOrDefault(x => x.ReturnItemId == line.ReturnItemId);
            if (match is null
                || match.AcceptedUsableQuantity != line.AcceptedUsable
                || match.AcceptedDamagedQuantity != line.AcceptedDamaged
                || match.RejectedReceivedUsableQuantity != line.RejectedReceivedUsable
                || match.RejectedReceivedDamagedQuantity != line.RejectedReceivedDamaged
                || match.RejectedUnreceivedQuantity != line.RejectedUnreceived
                || !string.Equals(match.Reason, line.Reason?.Trim(), StringComparison.Ordinal))
            {
                throw new ContractOperationException(ReturnsErrorCodes.ResolutionVersionStale);
            }
        }
    }
}
