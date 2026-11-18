using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tooba.BuildingBlocks;
using Tooba.Order.Contracts.Returns;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Infrastructure.Directories;

/// <summary>
/// TB-272 / TB-272-R1: ordinary (customer-delivered) creation. Authority first, then ONE classified decision per
/// seller order.
/// <para>
/// The previous shape answered a globally matched idempotency key <b>before</b> it validated the submitted order and
/// before it compared what the stored case was actually created for, so a replay could hand back a case belonging to
/// another customer/order, and a changed request could be answered as if it were the original. The order of
/// operations here is therefore deliberate:
/// </para>
/// <list type="number">
/// <item><description>the caller's mutation gate, the submitted order binding and (customer path) ownership;</description></item>
/// <item><description>the list-shape and persisted-precision refusals (nothing is rounded, nothing is replayed);</description></item>
/// <item><description>one normalized creation intent (lines as a set, trimmed reason, destination, checkout, seller);</description></item>
/// <item><description>the relational critical section of the canonical seller order (serializes the quantity read);</description></item>
/// <item><description>exact replay answered only for the identical intent, otherwise a typed conflict with zero writes;</description></item>
/// <item><description>eligibility read AFTER the lock, then the insert.</description></item>
/// </list>
/// <para>
/// TB-272-R1 also bounds the failure recovery: a key collision removes only the entries this failed creation added and,
/// when the caller owns the transaction, restores a savepoint instead of rolling the caller's transaction back or
/// clearing the tracker, so unrelated caller work keeps its identity, state and transaction.
/// </para>
/// </summary>
public sealed partial class ReturnDirectory
{
    private async Task<ReturnSnapshot> CreateInternalAsync(
        CreateReturnCommand command,
        bool requireOwner,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            throw new ContractOperationException(ReturnsErrorCodes.IdempotencyRequired);
        }

        // TB-272 (A): the immutable binding is loaded and validated FIRST. Nothing below may answer a stored
        // snapshot before this caller is proven to address this order (and, on the customer path, to own it).
        var orderContext = await _orders.GetReturnContextAsync(command.SellerOrderId, cancellationToken)
            ?? throw new ContractOperationException(ReturnsErrorCodes.Missing);

        if (requireOwner && orderContext.PlacedByUserId != command.ActorUserId)
        {
            throw new ContractOperationException(ReturnsErrorCodes.NotOwner);
        }

        // TB-272 (B): one ordinary request line per order line — refused here (endpoint-reachable typed fault) and
        // again by the aggregate so no future caller can bypass it. Courier-origin creation is unaffected.
        if (ReturnRequest.FindRepeatedOrderLine(command.Items.Select(x => x.OrderLineId)) is not null)
        {
            throw new ContractOperationException(ReturnsErrorCodes.DuplicateLine);
        }

        // TB-272-R1 (A): the persisted quantity is numeric(18,6). A quantity with more fractional digits would be
        // stored as a DIFFERENT number of physical units, and two different raw quantities could persist identically
        // and then compare equal. It is refused BEFORE the stored-key read, so an invalid request can never be handed
        // an existing snapshot as if it had succeeded.
        foreach (var item in command.Items)
        {
            if (!PersistedReturnQuantity.IsExactlyRepresentable(item.Quantity))
            {
                throw new ContractOperationException(ReturnsErrorCodes.QuantityPrecision);
            }
        }

        var intent = OrdinaryCreationIntent.Capture(orderContext, command);
        var idempotencyKey = command.IdempotencyKey.Trim();

        // TB-272 (D): the whole decision (quantity read → insert) is one critical section per seller order.
        var relational = _db.Database.IsRelational();
        var ownsTransaction = relational && _db.Database.CurrentTransaction is null;
        await using var ownedTransaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        // A caller that already opened a transaction stays its owner: this method never commits or rolls it back.
        var callerTransaction = ownedTransaction is null ? _db.Database.CurrentTransaction : null;
        HashSet<object>? trackedBefore = null;

        try
        {
            if (relational)
            {
                await ReturnCreationSerialization.AcquireAsync(
                    _db.Database, orderContext.SellerOrderId, cancellationToken);
            }

            var stored = await _db.ReturnRequests.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
            if (stored is not null)
            {
                var replay = await AnswerStoredCreationAsync(stored, intent, cancellationToken);
                await CommitCreationAsync(ownedTransaction, cancellationToken);
                return replay;
            }

            // The quantity authority is read AFTER the lock, so it observes every creation that committed while this
            // request waited. Nothing external (money/stock/provider) runs inside this transaction.
            var eligibility = await _eligibility.EvaluateAsync(command.SellerOrderId, cancellationToken);
            if (!eligibility.Eligible)
            {
                throw new ContractOperationException(ReturnEligibilityReasonCodes.ToErrorCode(eligibility.ReasonCode));
            }

            var remainingByLine = eligibility.Lines.ToDictionary(x => x.OrderLineId, x => x.RemainingReturnableQuantity);
            var lineSnapshots = new List<(Guid OrderLineId, decimal Quantity, decimal UnitPriceSnapshot, Guid? ReservationId)>();
            foreach (var item in command.Items)
            {
                var orderLine = orderContext.Lines.SingleOrDefault(x => x.OrderLineId == item.OrderLineId)
                    ?? throw new ContractOperationException(ReturnsErrorCodes.LineMissing);
                remainingByLine.TryGetValue(item.OrderLineId, out var remaining);
                if (item.Quantity <= 0 || item.Quantity > remaining)
                {
                    throw new ContractOperationException(ReturnsErrorCodes.QuantityExceeded);
                }

                // TB-278 (B): the requested quantity must be a whole number of the ORDERING UNIT of this line. A
                // whole-piece line refuses 0.5 before any row is written, and an unanswered owning precision never
                // grants a fractional claim. The exact stored-intent replay above is answered before this gate, so a
                // committed occurrence is never re-described as invalid.
                if (!OwningUnitQuantity.IsCreationQuantityAcceptable(
                        item.Quantity, orderLine.QuantityDecimalPlacesSnapshot))
                {
                    throw new ContractOperationException(ReturnsErrorCodes.QuantityUnitInvalid);
                }

                lineSnapshots.Add((item.OrderLineId, item.Quantity, orderLine.UnitPriceSnapshot, orderLine.ReservationId));
            }

            var now = _clock.UtcNow;
            var request = ReturnRequest.Create(
                _ids.NewId(),
                () => _ids.NewId(),
                orderContext.SellerOrderId,
                orderContext.CheckoutId,
                orderContext.SellerPartyId,
                orderContext.PlacedByUserId,
                idempotencyKey,
                command.Reason,
                orderContext.Currency,
                lineSnapshots,
                now,
                command.RefundDestination);

            // TB-272-R1 (B): remember exactly what the tracker held BEFORE this creation. Recovery detaches only the
            // entries this attempt added, so unrelated caller work keeps its identity, state and values.
            trackedBefore = SnapshotTrackedEntities();
            string? savepointName = null;
            if (callerTransaction is not null)
            {
                savepointName = ReturnCreationSerialization.CallerSavepointName;
                await callerTransaction.CreateSavepointAsync(savepointName, cancellationToken);
            }

            _db.ReturnRequests.Add(request);
            _db.ReturnItems.AddRange(request.Items);

            // TB-279 (D): the NEW claim is bound to the exact owning delivered slices, under the seller-order
            // serialization already held above, so two sibling cases can never claim the same slice twice and an old
            // return can never consume a newer delivery's window.
            _db.ReturnDeliveryAllocations.AddRange(BuildDeliveryAllocations(request, eligibility));
            await _db.SaveChangesAsync(cancellationToken);
            if (callerTransaction is not null && savepointName is not null)
            {
                await callerTransaction.ReleaseSavepointAsync(savepointName, cancellationToken);
            }

            await CommitCreationAsync(ownedTransaction, cancellationToken);
            _telemetry.RecordCreated();
            return await MapSnapshotAsync(request, cancellationToken);
        }
        catch (DbUpdateException ex) when (ReturnCreationSerialization.IsIdempotencyConflict(ex))
        {
            // ONLY the idempotency unique index reaches here (a genuine same-key collision that did not serialize, for
            // example a writer holding an outer transaction). The winner is re-read and compared with the SAME
            // authorization + exact-intent rule; a different intent stays a typed conflict. Anything else propagates.
            if (ownedTransaction is not null)
            {
                // This attempt owns the transaction, so ending it is this attempt's own decision.
                await ownedTransaction.RollbackAsync(cancellationToken);
            }
            else if (callerTransaction is not null)
            {
                // The caller owns the transaction: restore the savepoint so it stays usable, and never commit or roll
                // it back. If savepoint recovery is impossible the honest provider failure propagates to the caller.
                await callerTransaction.RollbackToSavepointAsync(
                    ReturnCreationSerialization.CallerSavepointName, cancellationToken);
            }

            DetachFailedCreation(trackedBefore);
            var winner = await _db.ReturnRequests.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken)
                ?? throw new ContractOperationException(ReturnsErrorCodes.Stale);
            return await AnswerStoredCreationAsync(winner, intent, cancellationToken);
        }
    }

    /// <summary>
    /// Answers an already-stored case for this key ONLY when the stored row is the same immutable creation intent
    /// (seller order, checkout, seller party, owning customer, origin, destination, reason and the exact
    /// line/quantity set). A changed intent is a typed conflict, never a borrowed snapshot and never a mutation.
    /// </summary>
    private async Task<ReturnSnapshot> AnswerStoredCreationAsync(
        ReturnRequest stored,
        OrdinaryCreationIntent intent,
        CancellationToken cancellationToken)
    {
        var snapshot = await MapSnapshotAsync(stored, cancellationToken);
        if (!intent.Matches(stored, snapshot))
        {
            throw new ContractOperationException(ReturnsErrorCodes.IdempotencyConflict);
        }

        return snapshot;
    }

    private static async Task CommitCreationAsync(
        IDbContextTransaction? ownedTransaction,
        CancellationToken cancellationToken)
    {
        if (ownedTransaction is not null)
        {
            await ownedTransaction.CommitAsync(cancellationToken);
        }
    }

    /// <summary>Identity of every entity the tracker held before this creation attempt added its own rows.</summary>
    private HashSet<object> SnapshotTrackedEntities() =>
        _db.ChangeTracker.Entries()
            .Select(x => x.Entity)
            .ToHashSet(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// TB-272-R1 (B): removes ONLY the entries this failed attempt introduced (the request, its items and anything a
    /// save interceptor attached for them). Entries that were already tracked keep their entity instance, state and
    /// values, so pending caller work is neither detached nor re-saved.
    /// </summary>
    private void DetachFailedCreation(HashSet<object>? trackedBefore)
    {
        if (trackedBefore is null)
        {
            // The attempt never reached its own inserts, so it introduced nothing to remove.
            return;
        }

        foreach (var entry in _db.ChangeTracker.Entries().ToList())
        {
            if (entry.State == EntityState.Detached)
            {
                continue;
            }

            if (trackedBefore.Contains(entry.Entity))
            {
                continue;
            }

            entry.State = EntityState.Detached;
        }
    }

    /// <summary>
    /// The normalized creation intent of ONE ordinary request. Comparison is deliberately ordered: a different item
    /// ORDER is the same intent, while a different line set, quantity, destination, reason, order, checkout, seller
    /// party or owning customer is a different request. Quantities are compared at the persisted scale — since every
    /// accepted input is already exactly representable there, that alignment can never hide a changed quantity.
    /// </summary>
    private sealed record OrdinaryCreationIntent(
        Guid SellerOrderId,
        Guid CheckoutId,
        Guid SellerPartyId,
        Guid RequestedByUserId,
        RefundDestination RefundDestination,
        string ReasonKey,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity)> Lines)
    {
        public static OrdinaryCreationIntent Capture(
            OrderReturnContextSnapshot orderContext,
            CreateReturnCommand command) =>
            new(
                orderContext.SellerOrderId,
                orderContext.CheckoutId,
                orderContext.SellerPartyId,
                orderContext.PlacedByUserId,
                command.RefundDestination,
                NormalizeReason(command.Reason),
                command.Items
                    .Select(x => (x.OrderLineId, NormalizeQuantity(x.Quantity)))
                    .OrderBy(x => x.OrderLineId)
                    .ToArray());

        public bool Matches(ReturnRequest stored, ReturnSnapshot snapshot)
        {
            if (stored.SellerOrderId != SellerOrderId
                || stored.CheckoutId != CheckoutId
                || stored.SellerPartyId != SellerPartyId
                || stored.Origin != ReturnOrigin.CustomerDelivered
                || stored.RequestedByUserId != RequestedByUserId
                || stored.RefundDestination != RefundDestination
                || !string.Equals(NormalizeReason(stored.Reason), ReasonKey, StringComparison.Ordinal))
            {
                return false;
            }

            var storedLines = snapshot.Items
                .Select(x => (x.OrderLineId, NormalizeQuantity(x.Quantity)))
                .OrderBy(x => x.OrderLineId)
                .ToArray();
            return storedLines.Length == Lines.Count && storedLines.SequenceEqual(Lines);
        }

        private static string NormalizeReason(string? reason) => (reason ?? string.Empty).Trim();

        private static decimal NormalizeQuantity(decimal quantity) =>
            decimal.Round(quantity, PersistedReturnQuantity.Scale, MidpointRounding.AwayFromZero);
    }
}
