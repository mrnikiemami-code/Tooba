using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Inventory.Contracts.Returns;
using Tooba.Order.Contracts.Returns;
using DomainClaimCondition = Tooba.Fulfillment.Domain.ValueObjects.CourierReturnClaimCondition;
using DomainClaimLine = Tooba.Fulfillment.Domain.ValueObjects.CourierReturnClaimLine;
using DomainClaimStatus = Tooba.Fulfillment.Domain.ValueObjects.CourierReturnClaimStatus;

namespace Tooba.Fulfillment.Infrastructure.Returns;

/// <summary>
/// TB-265: Fulfillment-owned authority for courier-origin Returns cases.
/// <para>
/// The physically received quantity of a returned parcel is ONE pool. A resend, a damaged replacement and a Returns
/// case all draw from it, so the case must claim its exact quantity here before it exists. This type is the only
/// writer of that claim, and it releases a claim only for the owning durable decision (reject/cancel) or as part of an
/// approval settlement.
/// </para>
/// <para>
/// TB-265-R1: the claim is stored twice on purpose — as the reviewed payload (replay identity) and as one
/// <see cref="CourierReturnClaimLineRecord"/> per line carrying its own active capacity key, so the database refuses a
/// competing active claim of the same (receipt, line, condition) quantity even from another process. Approval
/// settlement materialises one <see cref="CourierReturnClaimAllocation"/> per claimed quantity BEFORE any parcel or
/// stock effect, so the cancelled replacement and the released hold are always the exact source of that line.
/// </para>
/// </summary>
public sealed class CourierReturnSourceDirectory : IFulfillmentCourierReturnSource
{
    private readonly FulfillmentDbContext _db;
    private readonly IFulfillmentUseCaseGuard _guard;
    private readonly IInventoryReturnObligationRelease _inventoryRelease;
    private readonly IOrderReturnObligationClosure _obligations;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    /// <summary>Wires the directory to the owning Fulfillment schema and the narrow Inventory/Order seams.</summary>
    public CourierReturnSourceDirectory(
        FulfillmentDbContext db,
        IFulfillmentUseCaseGuard guard,
        IInventoryReturnObligationRelease inventoryRelease,
        IOrderReturnObligationClosure obligations,
        IClock clock,
        IIdGenerator ids)
    {
        _db = db;
        _guard = guard;
        _inventoryRelease = inventoryRelease;
        _obligations = obligations;
        _clock = clock;
        _ids = ids;
    }

    /// <inheritdoc />
    public Task<CourierReturnClaimResult> ClaimAsync(
        CreateCourierReturnClaimCommand command,
        CancellationToken cancellationToken) =>
        ExecuteSerializedAsync(
            async () =>
            {
                await _guard.EnsureCanMutateAsync(cancellationToken);
                return await ClaimOnceAsync(command, cancellationToken);
            },
            cancellationToken);

    private async Task<CourierReturnClaimResult> ClaimOnceAsync(
        CreateCourierReturnClaimCommand command,
        CancellationToken cancellationToken)
    {
        // TB-265: capacity is a SHARED pool, so the free-quantity check and the claim insert must be one serialized
        // decision. Serializable isolation is the SAME protocol the resend/replacement writers already use, so a
        // concurrent resend/replacement/dispatch and this claim cannot both commit against the same received units.
        // The row lock additionally serializes two claims of the same unit.
        var ownsTransaction = _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;
        if (ownsTransaction)
        {
            await _db.Database.ExecuteSqlAsync(
                $"SELECT 1 FROM fulfillment.fulfillments WHERE fulfillment_id = {command.FulfillmentId} FOR UPDATE",
                cancellationToken);
        }

        var existing = await _db.CourierReturnClaims
            .SingleOrDefaultAsync(x => x.FulfillmentId == command.FulfillmentId
                                       && x.OccurrenceKey == command.OccurrenceKey, cancellationToken);
        if (existing is not null)
        {
            var replay = existing.AnswersIntent(command.FulfillmentId, command.ShipmentId, ToDomain(command.Lines))
                ? new CourierReturnClaimResult(Map(existing))
                : new CourierReturnClaimResult(null, "fulfillment.returns.claim_intent_conflict");
            await CommitAsync(transaction, ownsTransaction, cancellationToken);
            return replay;
        }

        var unit = await LoadMutableAsync(command.FulfillmentId, cancellationToken);
        var shipment = unit.Shipments.SingleOrDefault(x => x.ShipmentId == command.ShipmentId)
            ?? throw new ContractOperationException("fulfillment.shipment_not_found");

        if (shipment.Status != ShipmentStatus.Returned)
        {
            // Only a parcel that was PHYSICALLY received at origin can fund a courier-origin case; a non-delivery
            // attempt or a parcel still travelling is not a receipt and must never be treated as one.
            await CommitAsync(transaction, ownsTransaction, cancellationToken);
            return new CourierReturnClaimResult(null, "fulfillment.returns.source_not_received");
        }

        var receipt = unit.ReturnReceipts
            .Where(x => x.ShipmentId == shipment.ShipmentId)
            .OrderByDescending(x => x.ReceivedAt)
            .FirstOrDefault()
            ?? throw new ContractOperationException("fulfillment.returns.source_receipt_missing");

        foreach (var line in command.Lines)
        {
            if (!receipt.Items.Any(x => x.OrderLineId == line.OrderLineId))
            {
                await CommitAsync(transaction, ownsTransaction, cancellationToken);
                return new CourierReturnClaimResult(null, "fulfillment.returns.source_line_missing");
            }

            // The aggregate subtracts every still-binding claim (attached below), so this is the real free
            // quantity of the receipt — never a second, independent tally that could overdraft the same units.
            // A damaged unit already committed to a NOT-YET-DISPATCHED replacement is deliberately switchable:
            // approval closes that branch instead of pretending the unit was never decided.
            var free = (int)line.Condition == (int)Contracts.Returns.CourierReturnClaimCondition.Damaged
                ? unit.OutstandingDamagedQuantity(shipment.ShipmentId, line.OrderLineId)
                  + unit.RedirectableDamagedQuantity(shipment.ShipmentId, line.OrderLineId)
                : unit.ResendableQuantity(shipment.ShipmentId, line.OrderLineId);

            if (free < line.Quantity)
            {
                await CommitAsync(transaction, ownsTransaction, cancellationToken);

                // A damaged unit whose replacement already left the warehouse is a live obligation, not unused held
                // stock: name that durable fact instead of implying the receipt had no capacity at all.
                if ((int)line.Condition == (int)Contracts.Returns.CourierReturnClaimCondition.Damaged
                    && unit.DamagedCommittedToDispatchedReplacement(shipment.ShipmentId, line.OrderLineId) > 0m)
                {
                    return new CourierReturnClaimResult(
                        null, "fulfillment.returns.replacement_already_dispatched");
                }

                return new CourierReturnClaimResult(null, "fulfillment.returns.claim_quantity_exceeded");
            }
        }

        var now = _clock.UtcNow;
        var claim = CourierReturnClaim.Create(
            _ids.NewId(),
            command.FulfillmentId,
            shipment.ShipmentId,
            receipt.ShipmentReturnReceiptId,
            command.ActorUserId,
            command.OccurrenceKey,
            ToDomain(command.Lines),
            now);
        _db.CourierReturnClaims.Add(claim);

        // TB-265-R1: one durable line row per claimed (line, condition). Its active key collides in the database if
        // another still-open claim already holds that exact received quantity.
        foreach (var line in claim.Lines)
        {
            _db.CourierReturnClaimLines.Add(CourierReturnClaimLineRecord.Create(
                _ids.NewId(),
                claim.CourierReturnClaimId,
                claim.ShipmentReturnReceiptId,
                line.OrderLineId,
                line.Condition,
                line.Quantity,
                now));
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, ownsTransaction, cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (ownsTransaction && transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            // The unique occurrence/active-key guards are the real concurrency safety: a racing context loses here
            // and reads the winning durable fact instead of overdrawing the same received quantity.
            _db.ChangeTracker.Clear();
            var winner = await _db.CourierReturnClaims.AsNoTracking()
                .SingleOrDefaultAsync(x => x.FulfillmentId == command.FulfillmentId
                                           && x.OccurrenceKey == command.OccurrenceKey, cancellationToken);
            if (winner is not null)
            {
                return winner.AnswersIntent(command.FulfillmentId, command.ShipmentId, ToDomain(command.Lines))
                    ? new CourierReturnClaimResult(Map(winner))
                    : new CourierReturnClaimResult(null, "fulfillment.returns.claim_intent_conflict");
            }

            var orderLineIds = command.Lines.Select(l => l.OrderLineId).ToArray();
            var capacityConflict = await _db.CourierReturnClaimLines.AsNoTracking()
                .AnyAsync(x => x.ActiveKey != null && orderLineIds.Contains(x.OrderLineId), cancellationToken);
            return capacityConflict
                ? new CourierReturnClaimResult(null, "fulfillment.returns.claim_quantity_exceeded")
                : new CourierReturnClaimResult(null, "fulfillment.returns.claim_concurrency_conflict");
        }

        return new CourierReturnClaimResult(Map(claim));
    }

    /// <summary>
    /// TB-265-R2: one serialized decision may lose a 40001 read/write-dependency race before it ever commits. The
    /// decision is idempotent, so it is re-evaluated ONCE against the committed state (where the winner's row is now
    /// visible) instead of surfacing a raw engine error to the operator.
    /// </summary>
    private async Task<T> ExecuteSerializedAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception exception) when (attempt == 0 && IsSerializationFailure(exception))
            {
                _db.ChangeTracker.Clear();
            }
        }
    }

    private static bool IsSerializationFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is Npgsql.PostgresException postgres
                && (postgres.SqlState == Npgsql.PostgresErrorCodes.SerializationFailure
                    || postgres.SqlState == Npgsql.PostgresErrorCodes.DeadlockDetected))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task CommitAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction,
        bool ownsTransaction,
        CancellationToken cancellationToken)
    {
        if (!ownsTransaction || transaction is null)
        {
            return;
        }

        try
        {
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == Npgsql.PostgresErrorCodes.SerializationFailure)
        {
            // A concurrent resend/replacement/dispatch committed against the same received units first. Nothing of
            // this intent was written, so the caller gets a typed conflict instead of a half-claimed capacity.
            await transaction.RollbackAsync(cancellationToken);
            throw new ContractOperationException("fulfillment.returns.claim_concurrency_conflict", ex);
        }
    }

    /// <inheritdoc />
    public async Task<CourierReturnClaimSnapshot?> GetClaimAsync(Guid claimId, CancellationToken cancellationToken)
    {
        var claim = await _db.CourierReturnClaims.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CourierReturnClaimId == claimId, cancellationToken);
        return claim is null ? null : Map(claim);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CourierReturnClaimSnapshot>> ListForShipmentAsync(
        Guid shipmentId,
        CancellationToken cancellationToken)
    {
        var claims = await _db.CourierReturnClaims.AsNoTracking()
            .Where(x => x.ShipmentId == shipmentId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return claims.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task<CourierReturnClaimSnapshot> LinkCaseAsync(
        Guid claimId,
        Guid returnRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var claim = await LoadClaimAsync(claimId, cancellationToken);
        claim.LinkCase(returnRequestId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(claim);
    }

    /// <inheritdoc />
    public async Task<CourierReturnClaimResult> ReleaseAsync(
        Guid claimId,
        Guid actorUserId,
        string state,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var status = string.Equals(state, nameof(DomainClaimStatus.Rejected), StringComparison.OrdinalIgnoreCase)
            ? DomainClaimStatus.Rejected
            : string.Equals(state, nameof(DomainClaimStatus.Cancelled), StringComparison.OrdinalIgnoreCase)
                ? DomainClaimStatus.Cancelled
                : throw new ContractOperationException("fulfillment.returns.claim_release_state_invalid");

        var claim = await LoadClaimAsync(claimId, cancellationToken);
        var now = _clock.UtcNow;
        claim.Release(status, now);

        // TB-265-R1: releasing the branch frees the per-line capacity keys in the same durable unit as the claim
        // status, so the same received quantity may lawfully be claimed by a new case afterwards.
        var lines = await _db.CourierReturnClaimLines
            .Where(x => x.ClaimId == claimId)
            .ToListAsync(cancellationToken);
        foreach (var line in lines)
        {
            line.ReleaseCapacity(now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new CourierReturnClaimResult(Map(claim));
    }

    /// <inheritdoc />
    public Task<CourierReturnSettlement> EnsureAllocationsAsync(
        Guid claimId,
        Guid returnRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        ExecuteSerializedAsync(
            async () =>
            {
                _ = returnRequestId;
                _ = actorUserId;
                await _guard.EnsureCanMutateAsync(cancellationToken);
                return await EnsureAllocationsOnceAsync(claimId, cancellationToken);
            },
            cancellationToken);

    private async Task<CourierReturnSettlement> EnsureAllocationsOnceAsync(
        Guid claimId,
        CancellationToken cancellationToken)
    {
        // Same serialized protocol as the claim/settlement: the exact allocation is a fact about this unit, so it is
        // recorded under the unit row lock and committed BEFORE the caller performs an irreversible closure.
        var ownsTransaction = _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;

        var claim = await LoadClaimAsync(claimId, cancellationToken);
        if (ownsTransaction)
        {
            await _db.Database.ExecuteSqlAsync(
                $"SELECT 1 FROM fulfillment.fulfillments WHERE fulfillment_id = {claim.FulfillmentId} FOR UPDATE",
                cancellationToken);
        }

        var allocations = await _db.CourierReturnClaimAllocations
            .Where(x => x.ClaimId == claimId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.AllocationId)
            .ToListAsync(cancellationToken);

        if (claim.SettledAt is not null)
        {
            await CommitAsync(transaction, ownsTransaction, cancellationToken);
            return BuildSettlement(claim, allocations, blocker: null, settled: true);
        }

        if (allocations.Count == 0)
        {
            var unit = await LoadMutableAsync(claim.FulfillmentId, cancellationToken);
            List<CourierReturnClaimAllocation> planned;
            try
            {
                planned = Plan(unit, claim, _clock.UtcNow);
            }
            catch (ContractOperationException refusal)
            {
                // A genuinely unrunnable branch is DATA, not a half-closed obligation: zero writes, so the approved
                // case stays resumable once the blocker is resolved.
                await CommitAsync(transaction, ownsTransaction, cancellationToken);
                return BuildSettlement(claim, [], refusal.Code, settled: false);
            }

            _db.CourierReturnClaimAllocations.AddRange(planned);
            await _db.SaveChangesAsync(cancellationToken);
            allocations = planned;
        }

        await CommitAsync(transaction, ownsTransaction, cancellationToken);
        return BuildSettlement(claim, allocations, blocker: null, settled: false);
    }

    /// <inheritdoc />
    public Task<CourierReturnSettlement> SettleApprovedAsync(
        Guid claimId,
        Guid returnRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        ExecuteSerializedAsync(
            async () =>
            {
                _ = actorUserId;
                await _guard.EnsureCanMutateAsync(cancellationToken);
                return await SettleApprovedOnceAsync(claimId, returnRequestId, cancellationToken);
            },
            cancellationToken);

    private async Task<CourierReturnSettlement> SettleApprovedOnceAsync(
        Guid claimId,
        Guid returnRequestId,
        CancellationToken cancellationToken)
    {
        // TB-265-R1: settlement mutates the aggregate (cancelling the replacement parcel) and releases stock, so it
        // must be serialized with a competing claim/dispatch of the same unit and be one durable unit itself.
        var ownsTransaction = _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;

        var claim = await LoadClaimAsync(claimId, cancellationToken);
        if (ownsTransaction)
        {
            await _db.Database.ExecuteSqlAsync(
                $"SELECT 1 FROM fulfillment.fulfillments WHERE fulfillment_id = {claim.FulfillmentId} FOR UPDATE",
                cancellationToken);
        }

        var allocations = await _db.CourierReturnClaimAllocations
            .Where(x => x.ClaimId == claimId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.AllocationId)
            .ToListAsync(cancellationToken);

        // Durable replay: a settled claim answers its recorded result and writes nothing.
        if (claim.SettledAt is not null)
        {
            await CommitAsync(transaction, ownsTransaction, cancellationToken);
            return BuildSettlement(claim, allocations, blocker: null, settled: true);
        }

        // Order owns the delivery obligation. Fulfillment never decides on its own that a line no longer owes a
        // delivery, and it never closes the obligation implicitly when the case is merely requested.
        foreach (var group in claim.Lines.GroupBy(x => x.OrderLineId))
        {
            var closure = await _obligations.GetClosureAsync(returnRequestId, group.Key, cancellationToken);
            if (closure is null || closure.Quantity < group.Sum(x => x.Quantity))
            {
                await CommitAsync(transaction, ownsTransaction, cancellationToken);
                return BuildSettlement(claim, allocations, "order.return.closure_missing", settled: false);
            }
        }

        var unit = await LoadMutableAsync(claim.FulfillmentId, cancellationToken);
        var now = _clock.UtcNow;

        if (allocations.Count == 0)
        {
            // Record the exact source allocation BEFORE any side effect. A crash after this point resumes with the
            // same parcel/hold identity instead of re-planning against a changed unit.
            List<CourierReturnClaimAllocation> planned;
            try
            {
                planned = Plan(unit, claim, now);
            }
            catch (ContractOperationException refusal)
            {
                await CommitAsync(transaction, ownsTransaction, cancellationToken);
                return BuildSettlement(claim, [], refusal.Code, settled: false);
            }

            _db.CourierReturnClaimAllocations.AddRange(planned);
            await _db.SaveChangesAsync(cancellationToken);
            allocations = planned;
        }

        var pending = allocations.Where(x => x.State == CourierReturnAllocationState.Pending).ToList();

        // (1) Validate EVERY pending branch before any effect. A dispatched replacement refuses the whole settlement
        //     instead of cancelling the branches that were still valid.
        foreach (var allocation in pending)
        {
            if (allocation.CancelledReplacementShipmentId is { } parcelId)
            {
                var replacement = unit.Shipments.SingleOrDefault(x => x.ShipmentId == parcelId);
                if (replacement is null || replacement.Status != ShipmentStatus.Created)
                {
                    // An already dispatched replacement is a live customer obligation, not unused held stock.
                    await CommitAsync(transaction, ownsTransaction, cancellationToken);
                    return BuildSettlement(
                        claim, allocations, "fulfillment.returns.replacement_already_dispatched", settled: false);
                }
            }
        }

        // (2) Close the owning branch exactly. A PARTIAL selection settles only its own quantity into the case and
        //     keeps the unselected remainder as the same live decision with its retained hold; a full selection
        //     settles the decision. The selected Created parcel is then cancelled, because its units are re-prepared
        //     for the remainder instead of dispatching units the case already refunded.
        foreach (var allocation in pending)
        {
            if (allocation.DamagedResolutionId is { } resolutionId
                && unit.FindDamagedResolution(resolutionId) is { } resolution)
            {
                if (resolution.RemainingQuantity > allocation.Quantity)
                {
                    resolution.SettlePartialCase(allocation.Quantity, now);
                }
                else
                {
                    resolution.MarkReturnCaseSettled(now);
                }
            }

            if (allocation.CancelledReplacementShipmentId is { } parcelId)
            {
                unit.CancelShipment(parcelId, now);
                allocation.MarkBranchClosed(parcelId, now);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        // (3) Release the exact unconsumed held quantity of each allocation, once per allocation identity.
        foreach (var allocation in pending)
        {
            decimal released = 0m;
            if (allocation.HeldReservationId is { } reservationId)
            {
                // ONE release per allocation, keyed by the allocation: two claimed lines sharing a reservation each
                // release their OWN amount, and a lost response replays the same recorded amount.
                var result = await _inventoryRelease.ReleaseUnconsumedHeldAsync(
                    reservationId,
                    allocation.Quantity,
                    $"courier-return-hold-release:{allocation.AllocationId:N}",
                    cancellationToken);
                released = result.ReleasedQuantity;
            }

            allocation.MarkReleased(released, now);
        }

        claim.MarkApproved(returnRequestId, now);
        claim.MarkSettled(allocations.Sum(x => x.ReleasedQuantity), now);

        // The approved case permanently consumes the capacity; clearing the per-line keys keeps the rows as durable
        // history without blocking a genuinely new lawful claim of other received units.
        var lineRows = await _db.CourierReturnClaimLines
            .Where(x => x.ClaimId == claimId)
            .ToListAsync(cancellationToken);
        foreach (var line in lineRows)
        {
            line.ReleaseCapacity(now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await CommitAsync(transaction, ownsTransaction, cancellationToken);

        return BuildSettlement(claim, allocations, blocker: null, settled: true);
    }

    /// <summary>
    /// Builds the exact per-quantity allocations of this claim with ZERO writes. Every claimed line is answered by its
    /// OWN source: remaining damaged units go to their own recorded decision (own replacement parcel + own hold), and a
    /// usable unit goes to its own line hold. A decision that already dispatched is refused, never treated as unused.
    /// <para>
    /// TB-265-R2: a decision may be LARGER than the selected portion. The allocation then covers exactly the selected
    /// quantity and the settlement reduces that decision by it, so the unselected remainder keeps its retained hold and
    /// stays a lawful replacement obligation instead of being cancelled, refunded or released by this case.
    /// </para>
    /// </summary>
    private List<CourierReturnClaimAllocation> Plan(FulfillmentUnit unit, CourierReturnClaim claim, DateTimeOffset now)
    {
        var allocations = new List<CourierReturnClaimAllocation>();
        foreach (var line in claim.Lines)
        {
            var remaining = line.Quantity;
            if (line.Condition == DomainClaimCondition.Damaged)
            {
                var resolutions = unit.DamagedResolutions
                    .Where(x => x.ShipmentId == claim.ShipmentId
                                && x.ShipmentReturnReceiptId == claim.ShipmentReturnReceiptId
                                && x.OrderLineId == line.OrderLineId
                                && x.Status != DamagedResolutionStatus.Cancelled)
                    .OrderBy(x => x.CreatedAt)
                    .ToList();

                foreach (var resolution in resolutions)
                {
                    if (remaining <= 0m)
                    {
                        break;
                    }

                    var available = resolution.RemainingQuantity;
                    if (available <= 0m)
                    {
                        continue;
                    }

                    if (resolution.ReplacementShipmentId is { } parcelId)
                    {
                        var parcel = unit.Shipments.SingleOrDefault(x => x.ShipmentId == parcelId);
                        if (parcel is null || parcel.Status != ShipmentStatus.Created)
                        {
                            throw new ContractOperationException("fulfillment.returns.replacement_already_dispatched");
                        }
                    }

                    // Exact selected allocation: the smaller of the case quantity and this decision's OWN remaining
                    // quantity. A partial take is representable now, so no lawful selection is refused after closure.
                    var take = available < remaining ? available : remaining;
                    allocations.Add(CourierReturnClaimAllocation.Plan(
                        _ids.NewId(),
                        claim.CourierReturnClaimId,
                        claim.ShipmentId,
                        claim.ShipmentReturnReceiptId,
                        line.OrderLineId,
                        line.Condition,
                        take,
                        resolution.DamagedGoodsResolutionId,
                        resolution.ReplacementShipmentId,
                        resolution.ReplacementReservationId,
                        now));
                    remaining -= take;
                }

                if (remaining > 0m)
                {
                    // Damaged units with no recorded replacement decision: nothing was reserved and nothing may be
                    // made saleable, so this allocation releases exactly zero and stays an honest physical fact.
                    allocations.Add(CourierReturnClaimAllocation.Plan(
                        _ids.NewId(),
                        claim.CourierReturnClaimId,
                        claim.ShipmentId,
                        claim.ShipmentReturnReceiptId,
                        line.OrderLineId,
                        line.Condition,
                        remaining,
                        damagedResolutionId: null,
                        cancelledReplacementShipmentId: null,
                        heldReservationId: null,
                        now));
                }

                continue;
            }

            // A usable unit that came back: its own line hold is the only hold it may release. After a dispatch that
            // reservation is Consumed, so the owning release answers 0 and never resurrects the original sale.
            var item = unit.Items.SingleOrDefault(x => x.OrderLineId == line.OrderLineId);
            allocations.Add(CourierReturnClaimAllocation.Plan(
                _ids.NewId(),
                claim.CourierReturnClaimId,
                claim.ShipmentId,
                claim.ShipmentReturnReceiptId,
                line.OrderLineId,
                line.Condition,
                line.Quantity,
                damagedResolutionId: null,
                cancelledReplacementShipmentId: null,
                heldReservationId: item?.ReservationId,
                now));
        }

        return allocations;
    }

    private static CourierReturnSettlement BuildSettlement(
        CourierReturnClaim claim,
        IReadOnlyList<CourierReturnClaimAllocation> allocations,
        string? blocker,
        bool settled)
    {
        var released = claim.SettledAt is not null
            ? claim.ReleasedHeldQuantity
            : allocations.Sum(x => x.ReleasedQuantity);

        return new CourierReturnSettlement(
            claim.CourierReturnClaimId,
            settled,
            released,
            allocations.Select(x => x.CancelledReplacementShipmentId).FirstOrDefault(x => x is not null),
            allocations.Select(x => x.DamagedResolutionId).FirstOrDefault(x => x is not null),
            allocations.Select(x => x.HeldReservationId).FirstOrDefault(x => x is not null),
            blocker,
            Map(claim));
    }

    private async Task<CourierReturnClaim> LoadClaimAsync(Guid claimId, CancellationToken cancellationToken) =>
        await _db.CourierReturnClaims.SingleOrDefaultAsync(x => x.CourierReturnClaimId == claimId, cancellationToken)
        ?? throw new ContractOperationException("fulfillment.returns.claim_not_found");

    private async Task<FulfillmentUnit> LoadMutableAsync(Guid fulfillmentId, CancellationToken cancellationToken)
    {
        var unit = await _db.Fulfillments.SingleOrDefaultAsync(x => x.FulfillmentId == fulfillmentId, cancellationToken)
            ?? throw new ContractOperationException("fulfillment.not_found");
        var items = await _db.Items.Where(x => x.FulfillmentId == fulfillmentId).ToListAsync(cancellationToken);
        var shipments = await _db.Shipments.Where(x => x.FulfillmentId == fulfillmentId).ToListAsync(cancellationToken);
        foreach (var shipment in shipments)
        {
            var shipmentItems = await _db.ShipmentItems
                .Where(x => x.ShipmentId == shipment.ShipmentId)
                .ToListAsync(cancellationToken);
            shipment.AttachLoadedItems(shipmentItems);
        }

        var receipts = await _db.ShipmentReturnReceipts
            .Where(x => x.FulfillmentId == fulfillmentId)
            .ToListAsync(cancellationToken);
        foreach (var receipt in receipts)
        {
            var receiptItems = await _db.ShipmentReturnReceiptItems
                .Where(x => x.ShipmentReturnReceiptId == receipt.ShipmentReturnReceiptId)
                .ToListAsync(cancellationToken);
            receipt.AttachLoadedItems(receiptItems);
        }

        unit.AttachLoadedItems(items);
        unit.AttachLoadedShipments(shipments);
        unit.AttachLoadedReturnReceipts(receipts);
        var resolutions = await _db.DamagedResolutions
            .Where(x => x.FulfillmentId == fulfillmentId)
            .ToListAsync(cancellationToken);
        unit.AttachLoadedDamagedResolutions(resolutions);
        // TB-265: a still-binding Returns claim is capacity already promised to a case, never free resend capacity.
        var claims = await _db.CourierReturnClaims.AsNoTracking()
            .Where(x => x.FulfillmentId == fulfillmentId
                        && (x.Status == DomainClaimStatus.Requested || x.Status == DomainClaimStatus.Approved))
            .ToListAsync(cancellationToken);
        unit.AttachLoadedReturnCapacityClaims(claims.SelectMany(x => x.Lines.Select(l =>
            new ReturnedCapacityClaim(x.ShipmentId, l.OrderLineId, l.Quantity, l.Condition))).ToArray());
        // TB-265-R1: the send path needs the source identity of a REQUESTED claim to refuse dispatching a
        // replacement parcel whose damaged units the case is still asking back.
        unit.AttachLoadedCourierReturnClaims(claims.SelectMany(x => x.Lines.Select(l =>
            new CourierReturnClaimBinding(
                x.CourierReturnClaimId,
                x.ShipmentId,
                x.ShipmentReturnReceiptId,
                x.Status == DomainClaimStatus.Approved,
                l.OrderLineId,
                l.Condition,
                l.Quantity))).ToArray());
        return unit;
    }

    private static List<DomainClaimLine> ToDomain(IReadOnlyList<Contracts.Returns.CourierReturnClaimLine> lines) =>
        lines.Select(x => new DomainClaimLine(
                x.OrderLineId,
                x.Quantity,
                (DomainClaimCondition)(int)x.Condition))
            .ToList();

    private static Contracts.Returns.CourierReturnClaimLine ToContract(DomainClaimLine line) =>
        new(line.OrderLineId, line.Quantity, (Contracts.Returns.CourierReturnClaimCondition)(int)line.Condition);

    private static CourierReturnClaimSnapshot Map(CourierReturnClaim claim) =>
        new(
            claim.CourierReturnClaimId,
            claim.FulfillmentId,
            claim.ShipmentId,
            claim.ShipmentReturnReceiptId,
            claim.OccurrenceKey,
            (Contracts.Returns.CourierReturnClaimStatus)(int)claim.Status,
            claim.CreatedByUserId,
            claim.CreatedAt,
            claim.UpdatedAt,
            claim.Lines.Select(ToContract).ToList(),
            claim.ReturnRequestId);
}
