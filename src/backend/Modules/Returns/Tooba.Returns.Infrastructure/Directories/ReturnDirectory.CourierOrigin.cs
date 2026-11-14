using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Infrastructure.Directories;

/// <summary>
/// TB-265: courier-origin Returns cases. The physical origin receipt and the exclusive source-quantity claim belong
/// to Fulfillment; the case, its phases, the approval closure and the money step belong here.
/// </summary>
public sealed partial class ReturnDirectory
{
    /// <summary>
    /// Creates a courier-origin case. Order of operations is deliberate: the quantity is claimed in Fulfillment
    /// BEFORE any Returns row exists, so the same received units can never simultaneously fund a resend, a damaged
    /// replacement and a case. The claim is then linked to the created case exactly once.
    /// </summary>
    public async Task<(ReturnSnapshot? Snapshot, Guid ClaimId, bool AlreadyExisted, string? Conflict)> CreateCourierOriginAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        string idempotencyKey,
        string occurrenceKey,
        string? reason,
        IReadOnlyList<(Guid OrderLineId, decimal Quantity, ReturnItemCondition Condition)> items,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(occurrenceKey))
        {
            throw new ContractOperationException(ReturnsErrorCodes.IdempotencyRequired);
        }

        if (items is null || items.Count == 0 || items.Any(x => x.Quantity <= 0m))
        {
            throw new ContractOperationException(ReturnsErrorCodes.QuantityExceeded);
        }

        // TB-265-R1: the paid/ownership/line preflight happens BEFORE a durable claim exists. A case that can never
        // be created must not strand the source capacity of a physically received parcel.
        var fulfillmentSnapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new ContractOperationException(ReturnsErrorCodes.FulfillmentMissing);

        var orderContext = await _orders.GetReturnContextAsync(fulfillmentSnapshot.SellerOrderId, cancellationToken)
            ?? throw new ContractOperationException(ReturnsErrorCodes.Missing);

        if (!orderContext.IsPaid)
        {
            // A courier-origin case needs a real paid order: no fake delivery, no unpaid refund.
            throw new ContractOperationException(ReturnsErrorCodes.NotPaid);
        }

        if (orderContext.PlacedByUserId == Guid.Empty)
        {
            throw new ContractOperationException(ReturnsErrorCodes.NotOwner);
        }

        var lineSnapshots = new List<(Guid OrderLineId, decimal Quantity, decimal UnitPriceSnapshot, Guid? ReservationId, ReturnItemCondition Condition)>();
        foreach (var item in items)
        {
            var orderLine = orderContext.Lines.SingleOrDefault(x => x.OrderLineId == item.OrderLineId)
                ?? throw new ContractOperationException(ReturnsErrorCodes.LineMissing);
            if (item.Quantity > orderLine.Quantity)
            {
                throw new ContractOperationException(ReturnsErrorCodes.QuantityExceeded);
            }

            lineSnapshots.Add((
                item.OrderLineId,
                item.Quantity,
                orderLine.UnitPriceSnapshot,
                orderLine.ReservationId,
                item.Condition));
        }

        var claimResult = await _courierSource.ClaimAsync(
            new CreateCourierReturnClaimCommand(
                fulfillmentId,
                shipmentId,
                actorUserId,
                occurrenceKey,
                items.Select(x => new CourierReturnClaimLine(
                        x.OrderLineId,
                        x.Quantity,
                        x.Condition == ReturnItemCondition.Damaged
                            ? CourierReturnClaimCondition.Damaged
                            : CourierReturnClaimCondition.Usable))
                    .ToList()),
            cancellationToken);

        if (claimResult.Claim is null)
        {
            return (null, Guid.Empty, false, claimResult.ConflictCode ?? "return.courier_origin.conflict");
        }

        var claim = claimResult.Claim;

        // A recorded claim may already answer this exact creation (lost response / retry after link failure).
        if (claim.ReturnRequestId is { } linkedCaseId)
        {
            var linked = await GetAsync(linkedCaseId, cancellationToken);
            return (linked, claim.ClaimId, true, null);
        }

        var existingByKey = await _db.ReturnRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existingByKey is not null)
        {
            if (existingByKey.SourceClaimId == claim.ClaimId)
            {
                // TB-265-R1: the case was created but the claim link did not land (crash/lost response). An exact
                // retry REPAIRS the missing link instead of returning a case that is forever unlinked.
                await _courierSource.LinkCaseAsync(
                    claim.ClaimId, existingByKey.ReturnRequestId, actorUserId, cancellationToken);
                var repaired = await LoadMutableAsync(existingByKey.ReturnRequestId, cancellationToken);
                return (await MapSnapshotAsync(repaired, cancellationToken), claim.ClaimId, true, null);
            }

            return (null, claim.ClaimId, false, "return.courier_origin.idempotency_conflict");
        }

        var now = _clock.UtcNow;
        var request = ReturnRequest.CreateCourierOrigin(
            _ids.NewId(),
            () => _ids.NewId(),
            orderContext.SellerOrderId,
            orderContext.CheckoutId,
            orderContext.SellerPartyId,
            orderContext.PlacedByUserId,
            fulfillmentId,
            shipmentId,
            claim.ShipmentReturnReceiptId,
            claim.ClaimId,
            occurrenceKey,
            idempotencyKey,
            reason,
            orderContext.Currency,
            lineSnapshots,
            now);

        _db.ReturnRequests.Add(request);
        _db.ReturnItems.AddRange(request.Items);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordCreated();

        await _courierSource.LinkCaseAsync(claim.ClaimId, request.ReturnRequestId, actorUserId, cancellationToken);
        return (await MapSnapshotAsync(request, cancellationToken), claim.ClaimId, false, null);
    }

    /// <summary>Read-only exact-intent reconciliation; it never creates a case.</summary>
    public async Task<(ReturnSnapshot? Snapshot, Guid ClaimId, bool AlreadyExisted, string? Conflict)> ReconcileCourierOriginAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        string occurrenceKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(occurrenceKey))
        {
            return (null, Guid.Empty, false, "return.courier_origin.occurrence_required");
        }

        var claims = await _courierSource.ListForShipmentAsync(shipmentId, cancellationToken);
        var claim = claims.FirstOrDefault(x => x.FulfillmentId == fulfillmentId
                                               && string.Equals(x.OccurrenceKey, occurrenceKey.Trim(), StringComparison.Ordinal));
        if (claim is null)
        {
            return (null, Guid.Empty, true, null);
        }

        if (claim.ReturnRequestId is { } caseId)
        {
            return (await GetAsync(caseId, cancellationToken), claim.ClaimId, true, null);
        }

        return (null, claim.ClaimId, false, "return.courier_origin.pending_link");
    }

    /// <summary>
    /// Approval closure for a courier-origin case: order-line obligation first, source branch + once-only held
    /// release second, money last. Each phase is durable, so a retry resumes the exact remaining step.
    /// </summary>
    private async Task EnsureCourierOriginClosureAsync(
        ReturnRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        if (request.ClosurePhase < CourierOriginClosurePhase.ObligationClosed || request.ObligationClosedAt is null)
        {
            foreach (var group in request.Items.GroupBy(x => x.OrderLineId))
            {
                var closure = await _obligationClosure.CloseLineObligationAsync(
                    new Tooba.Order.Contracts.Returns.OrderLineObligationClosureRequest(
                        request.SellerOrderId,
                        request.CheckoutId,
                        group.Key,
                        group.Sum(x => x.Quantity),
                        request.ReturnRequestId,
                        request.SourceClaimId ?? Guid.Empty,
                        actorUserId),
                    cancellationToken);

                if (!closure.Closed)
                {
                    throw new ContractOperationException(closure.BlockerCode ?? "order.return.closure_refused");
                }
            }

            request.MarkObligationClosed(now);
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (request.ClosurePhase < CourierOriginClosurePhase.StockSettled || request.StockSettledAt is null)
        {
            if (request.SourceClaimId is not { } claimId)
            {
                throw new ContractOperationException("return.courier_origin.claim_missing");
            }

            var settlement = await _courierSource.SettleApprovedAsync(claimId, request.ReturnRequestId, actorUserId, cancellationToken);
            if (!settlement.Settled)
            {
                // Refused (for example an already dispatched replacement) is a pending, reconcilable fact — never a
                // silent refund over live stock.
                throw new ContractOperationException(settlement.BlockerCode ?? "return.courier_origin.settlement_refused");
            }

            request.MarkStockSettled(settlement.ReleasedHeldQuantity, now);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Records the physical disposition of every received line exactly once. A damaged unit is recorded as
    /// non-saleable and is NEVER passed to the unconditional saleable restock path.
    /// </summary>
    private async Task SettlePhysicalDispositionAsync(ReturnRequest request, CancellationToken cancellationToken)
    {
        foreach (var item in request.Items)
        {
            if (item.Disposition != ReturnItemDisposition.None)
            {
                // Already disposed by a previous durable pass: a retried/uncertain money answer can never restock
                // the same physical units twice.
                continue;
            }

            if (item.Condition == ReturnItemCondition.Damaged)
            {
                var entry = _db.Entry(item);
                item.MarkDisposition(ReturnItemDisposition.DamagedNonSaleable);
                try
                {
                    await _db.SaveChangesAsync(cancellationToken);
                }
                catch
                {
                    entry.State = EntityState.Unchanged;
                    throw;
                }

                continue;
            }

            // TB-265-R1: the owning restock is called BEFORE the disposition is marked, with the SAME per-item
            // identity on every retry. A failed restock therefore leaves the item explicitly pending instead of
            // persisting a false SaleableRestock.
            if (item.ReservationId is { } reservationId)
            {
                await _inventory.RestockConsumedReservationAsync(
                    reservationId,
                    item.Quantity,
                    $"return-restock-{request.ReturnRequestId:N}-{item.ReturnItemId:N}",
                    cancellationToken);
            }

            var itemEntry = _db.Entry(item);
            item.MarkDisposition(ReturnItemDisposition.SaleableRestock);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                itemEntry.State = EntityState.Unchanged;
                throw;
            }
        }
    }

    /// <summary>Releases ONLY the source-branch claim of a rejected/cancelled courier-origin case.</summary>
    private async Task ReleaseCourierOriginClaimAsync(
        ReturnRequest request,
        string state,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (request.Origin != ReturnOrigin.CourierOrigin || request.SourceClaimId is not { } claimId)
        {
            return;
        }

        await _courierSource.ReleaseAsync(claimId, actorUserId, state, cancellationToken);
    }
}
