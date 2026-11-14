using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Inventory.Contracts.Returns;
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
/// </summary>
public sealed class CourierReturnSourceDirectory : IFulfillmentCourierReturnSource
{
    private readonly FulfillmentDbContext _db;
    private readonly IFulfillmentUseCaseGuard _guard;
    private readonly IInventoryReturnObligationRelease _inventoryRelease;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    /// <summary>Wires the directory to the owning Fulfillment schema and the narrow Inventory release seam.</summary>
    public CourierReturnSourceDirectory(
        FulfillmentDbContext db,
        IFulfillmentUseCaseGuard guard,
        IInventoryReturnObligationRelease inventoryRelease,
        IClock clock,
        IIdGenerator ids)
    {
        _db = db;
        _guard = guard;
        _inventoryRelease = inventoryRelease;
        _clock = clock;
        _ids = ids;
    }

    /// <inheritdoc />
    public async Task<CourierReturnClaimResult> ClaimAsync(
        CreateCourierReturnClaimCommand command,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);

        // TB-265: capacity is a SHARED pool, so the free-quantity check and the claim insert must be one serialized
        // decision. The fulfillment row lock makes two independent contexts (or two operators) queue behind each
        // other instead of both passing the check and overdrawing the same received units.
        var ownsTransaction = _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
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

        var claim = CourierReturnClaim.Create(
            _ids.NewId(),
            command.FulfillmentId,
            shipment.ShipmentId,
            receipt.ShipmentReturnReceiptId,
            command.ActorUserId,
            command.OccurrenceKey,
            ToDomain(command.Lines),
            _clock.UtcNow);
        _db.CourierReturnClaims.Add(claim);

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
            _db.Entry(claim).State = EntityState.Detached;
            var winner = await _db.CourierReturnClaims.AsNoTracking()
                .SingleOrDefaultAsync(x => x.FulfillmentId == command.FulfillmentId
                                           && x.OccurrenceKey == command.OccurrenceKey, cancellationToken);
            return winner is not null
                   && winner.AnswersIntent(command.FulfillmentId, command.ShipmentId, ToDomain(command.Lines))
                ? new CourierReturnClaimResult(Map(winner))
                : new CourierReturnClaimResult(null, "fulfillment.returns.claim_concurrency_conflict");
        }

        return new CourierReturnClaimResult(Map(claim));
    }

    private static async Task CommitAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction,
        bool ownsTransaction,
        CancellationToken cancellationToken)
    {
        if (ownsTransaction && transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
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
        claim.Release(status, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return new CourierReturnClaimResult(Map(claim));
    }

    /// <inheritdoc />
    public async Task<CourierReturnSettlement> SettleApprovedAsync(
        Guid claimId,
        Guid returnRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var claim = await LoadClaimAsync(claimId, cancellationToken);

        // Durable replay: a settled claim answers its recorded result and writes nothing.
        if (claim.SettledAt is not null)
        {
            return new CourierReturnSettlement(
                claim.CourierReturnClaimId,
                true,
                claim.ReleasedHeldQuantity,
                Claim: Map(claim));
        }

        var unit = await LoadMutableAsync(claim.FulfillmentId, cancellationToken);
        var now = _clock.UtcNow;

        Guid? cancelledReplacementShipmentId = null;
        Guid? closedReplacementResolutionId = null;
        Guid? heldReservationId = null;

        // A recorded replacement branch for the same returned units must be closed by an explicit owning linkage;
        // an already dispatched/in-transit replacement is NOT unused held stock and is refused instead.
        foreach (var resolution in unit.DamagedResolutions
                     .Where(x => x.Status != DamagedResolutionStatus.Cancelled
                                 && claim.Lines.Any(l => l.Condition == DomainClaimCondition.Damaged
                                                         && l.OrderLineId == x.OrderLineId)))
        {
            if (resolution.ReplacementShipmentId is { } replacementShipmentId)
            {
                var replacement = unit.Shipments.SingleOrDefault(x => x.ShipmentId == replacementShipmentId);
                if (replacement is null || replacement.Status != ShipmentStatus.Created)
                {
                    return new CourierReturnSettlement(
                        claim.CourierReturnClaimId,
                        false,
                        BlockerCode: "fulfillment.returns.replacement_already_dispatched",
                        Claim: Map(claim));
                }

                unit.CancelShipment(replacementShipmentId, now);
                cancelledReplacementShipmentId = replacementShipmentId;
            }

            if (resolution.ReplacementReservationId is { } replacementReservationId)
            {
                heldReservationId ??= replacementReservationId;
                resolution.MarkReturnCaseSettled(now);
            }

            closedReplacementResolutionId ??= resolution.DamagedGoodsResolutionId;
        }

        if (heldReservationId is null)
        {
            // Fall back to the line's own binding; after dispatch this reservation is Consumed, and releasing a
            // consumed reservation is a documented no-op that never resurrects the original sale.
            heldReservationId = unit.Items
                .Where(x => claim.Lines.Any(l => l.OrderLineId == x.OrderLineId))
                .Select(x => x.ReservationId)
                .FirstOrDefault(x => x is not null);
        }

        var requestedReleaseQuantity = claim.Lines.Sum(x => x.Quantity);
        decimal released = 0m;
        if (heldReservationId is { } reservationId && requestedReleaseQuantity > 0m)
        {
            var result = await _inventoryRelease.ReleaseUnconsumedHeldAsync(
                reservationId,
                requestedReleaseQuantity,
                $"courier-return-hold-release:{claim.CourierReturnClaimId:N}",
                cancellationToken);
            released = result.ReleasedQuantity;
        }

        claim.MarkApproved(returnRequestId, now);
        claim.MarkSettled(released, now);
        await _db.SaveChangesAsync(cancellationToken);

        return new CourierReturnSettlement(
            claim.CourierReturnClaimId,
            true,
            released,
            cancelledReplacementShipmentId,
            closedReplacementResolutionId,
            heldReservationId,
            Claim: Map(claim));
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
