using System.Data;

using Microsoft.EntityFrameworkCore;
using Npgsql;

using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Infrastructure.Directories;

/// <summary>
/// TB-262: the durable damaged-goods resolution path and the linked healthy-stock replacement.
///
/// Damaged units are never usable resend capacity and never become saleable stock. Each damaged unit is resolved by
/// exactly ONE durable, source-scoped decision so one unit can never fund both a replacement and a refund/another
/// replacement. A replacement is funded by its OWN new purpose-bound Inventory reservation, keeps the accepted group
/// and method of the damaged source, and moves through the ordinary tracking/dispatch/delivery path.
///
/// Failure windows are recovered by occurrence identity, never by scanning:
/// <list type="number">
/// <item>decision recorded, reserve not attempted → the next retry reserves with the same decision key;</item>
/// <item>reserved, link not persisted → the same key returns the SAME hold and the link is persisted;</item>
/// <item>linked, parcel not persisted → the same decision creates the parcel;</item>
/// <item>parcel committed but the response was lost → the same occurrence replays that exact parcel.</item>
/// </list>
/// </summary>
public sealed partial class FulfillmentDirectory
{
    private const int DamagedResolutionAttempts = 2;

    /// <inheritdoc />
    public async Task<FulfillmentDamagedResolution> ResolveDamagedGoodsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        Guid shipmentId,
        Guid orderLineId,
        decimal quantity,
        DamagedResolutionOutcome outcome,
        string? occurrenceKey,
        string? note,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);

        // 1) The durable decision comes FIRST and is committed before anything is reserved, so a decision can never be
        //    lost because stock happened to be unavailable, and an exact retry can never record a second decision.
        var (unit, resolutionId) = await RecordDamagedDecisionAsync(
            fulfillmentId,
            actorUserId,
            shipmentId,
            orderLineId,
            quantity,
            outcome,
            occurrenceKey,
            note,
            cancellationToken);

        var resolution = unit.FindDamagedResolution(resolutionId)
            ?? throw new ContractOperationException(FulfillmentErrorCodes.DamagedResolutionNotFound);

        if (resolution.Status == DamagedResolutionStatus.Cancelled)
        {
            return await ResultAsync(
                unit,
                resolution,
                FulfillmentErrorCodes.DamagedResolutionCancelled,
                cancellationToken);
        }

        if (resolution.Outcome == DamagedResolutionOutcome.Referral)
        {
            // The item stays a pending customer-service / financial referral. Nothing was allocated and nothing is
            // claimed as resolved: only the owning resolution can close it later.
            _telemetry.RecordTransition("damaged_goods_referral_recorded");
            return await ResultAsync(unit, resolution, blockerCode: null, cancellationToken);
        }

        if (resolution.Status == DamagedResolutionStatus.Fulfilled
            && resolution.ReplacementShipmentId is not null)
        {
            return await ResultAsync(unit, resolution, blockerCode: null, cancellationToken);
        }

        // 2) The replacement's OWN healthy-stock reservation. The offer identity comes only from the committed sale
        //    reservation of the ordered line, never from the client.
        if (resolution.ReplacementReservationId is null)
        {
            var originalReservationId = unit.Items
                .SingleOrDefault(x => x.OrderLineId == resolution.OrderLineId)?.ReservationId;
            if (originalReservationId is null)
            {
                return await ResultAsync(
                    unit,
                    resolution,
                    FulfillmentErrorCodes.DamagedOriginalReservationMissing,
                    cancellationToken);
            }

            try
            {
                var reserved = await _inventory.ReserveReplacementReservationAsync(
                    originalReservationId.Value,
                    resolution.Quantity,
                    resolution.ReservationKey,
                    $"damaged-resolution:{resolution.DamagedGoodsResolutionId:N}",
                    _clock.UtcNow.AddDays(7),
                    cancellationToken);
                // RETENTION RULE: this hold belongs to the ACTIVE paid order, so its TTL is cleared (ExpiresAt=null)
                // exactly like the other paid-order reservations. A hold timeout can therefore never release it while
                // the order is active; only the owning order cancellation releases it, once.
                await _inventory.CommitReservationForPaidOrderAsync(reserved.ReservationId, cancellationToken);
                await AttachDamagedReservationAsync(
                    fulfillmentId,
                    resolution.DamagedGoodsResolutionId,
                    reserved.ReservationId,
                    cancellationToken);
                unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
                resolution = unit.FindDamagedResolution(resolution.DamagedGoodsResolutionId)!;
            }
            catch (ContractOperationException ex) when (IsSupplyUnavailable(ex))
            {
                // Precise, leak-free blocker: the decision stays recorded, NO parcel exists and NO hold was left
                // behind, so the same unresolved intent can be retried safely when healthy stock arrives.
                return await ResultAsync(
                    unit,
                    resolution,
                    FulfillmentErrorCodes.DamagedReplacementUnavailable,
                    cancellationToken);
            }
        }
        else
        {
            // RETENTION RULE: a cancelled parcel allocation never releases the order-bound hold, so a retry REBINDS
            // that exact allocation instead of reserving a second unit. A hold that is genuinely gone is a typed,
            // honest blocker — never a silent second reservation for the same damaged units.
            var retained = await _inventory.FindReplacementReservationAsync(
                resolution.ReplacementReservationId.Value,
                cancellationToken);
            if (retained is null
                || !string.Equals(retained.Status, "Held", StringComparison.OrdinalIgnoreCase))
            {
                return await ResultAsync(
                    unit,
                    resolution,
                    FulfillmentErrorCodes.DamagedReservationConflict,
                    cancellationToken);
            }
        }

        // 3) The linked replacement parcel, created exactly once per occurrence.
        unit = await CreateReplacementParcelAsync(
            fulfillmentId,
            resolution.DamagedGoodsResolutionId,
            actorUserId,
            cancellationToken);
        resolution = unit.FindDamagedResolution(resolution.DamagedGoodsResolutionId)!;
        _telemetry.RecordShipmentCreated();
        return await ResultAsync(unit, resolution, blockerCode: null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentDamagedIntentRecord> ReconcileDamagedResolutionAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid orderLineId,
        decimal quantity,
        DamagedResolutionOutcome outcome,
        string? occurrenceKey,
        CancellationToken cancellationToken)
    {
        if (fulfillmentId == Guid.Empty || shipmentId == Guid.Empty)
        {
            return new FulfillmentDamagedIntentRecord(FulfillmentDamagedIntentRecord.NotRecorded);
        }

        // READ-ONLY: no mutate guard, no SaveChanges, no transaction.
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        var recorded = unit.ResolveRecordedDamagedResolution(
            occurrenceKey,
            shipmentId,
            orderLineId,
            quantity,
            outcome);
        var state = recorded.State switch
        {
            FulfillmentUnit.RecordedDamagedResolutionState.Recorded =>
                FulfillmentDamagedIntentRecord.Recorded,
            FulfillmentUnit.RecordedDamagedResolutionState.ResolvedCancelled =>
                FulfillmentDamagedIntentRecord.ResolvedCancelled,
            FulfillmentUnit.RecordedDamagedResolutionState.Conflict =>
                FulfillmentDamagedIntentRecord.Conflict,
            _ => FulfillmentDamagedIntentRecord.NotRecorded,
        };
        if (recorded.Resolution is null)
        {
            return new FulfillmentDamagedIntentRecord(
                state,
                ConflictCode: state == FulfillmentDamagedIntentRecord.Conflict
                    ? FulfillmentErrorCodes.DamagedIntentConflict
                    : null);
        }

        var resolution = recorded.Resolution;
        return new FulfillmentDamagedIntentRecord(
            state,
            await MapSnapshotAsync(unit, cancellationToken),
            new DamagedGoodsResolutionRecord(
                resolution.DamagedGoodsResolutionId,
                resolution.ShipmentId,
                resolution.ShipmentReturnReceiptId,
                resolution.OrderLineId,
                resolution.Quantity,
                resolution.Outcome,
                resolution.Status,
                resolution.DecidedByUserId,
                resolution.CreatedAt,
                resolution.UpdatedAt,
                resolution.ReplacementReservationId,
                resolution.ReplacementShipmentId,
                resolution.Note),
            state == FulfillmentDamagedIntentRecord.Conflict ? FulfillmentErrorCodes.DamagedIntentConflict : null);
    }

    private async Task<FulfillmentDamagedResolution> ResultAsync(
        FulfillmentUnit unit,
        DamagedGoodsResolution resolution,
        string? blockerCode,
        CancellationToken cancellationToken) =>
        new(
            await MapSnapshotAsync(unit, cancellationToken),
            resolution.DamagedGoodsResolutionId,
            resolution.Status,
            resolution.ReplacementReservationId,
            resolution.ReplacementShipmentId,
            blockerCode);

    private async Task<(FulfillmentUnit Unit, Guid ResolutionId)> RecordDamagedDecisionAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        Guid shipmentId,
        Guid orderLineId,
        decimal quantity,
        DamagedResolutionOutcome outcome,
        string? occurrenceKey,
        string? note,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < DamagedResolutionAttempts; attempt++)
        {
            // Serializable is the resolution guard: two operators may both read the same outstanding damaged units,
            // but only ONE decision can commit — the other gets a typed conflict instead of resolving twice.
            await using var transaction = await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
            var existingIds = unit.DamagedResolutions.Select(x => x.DamagedGoodsResolutionId).ToHashSet();

            var resolution = unit.RecordDamagedResolution(
                _ids.NewId(),
                shipmentId,
                orderLineId,
                quantity,
                outcome,
                occurrenceKey,
                actorUserId,
                note,
                _clock.UtcNow);

            if (existingIds.Contains(resolution.DamagedGoodsResolutionId))
            {
                // An exact replay of a recorded decision: nothing new is written.
                await transaction.RollbackAsync(cancellationToken);
                return (unit, resolution.DamagedGoodsResolutionId);
            }

            _db.DamagedResolutions.Add(resolution);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsDamagedOccurrenceUniqueViolation(ex))
            {
                await SafeRollbackAsync(transaction);
                _db.ChangeTracker.Clear();
                continue;
            }
            catch (Exception ex) when (IsDamagedSerializationFailure(ex))
            {
                await SafeRollbackAsync(transaction);
                _db.ChangeTracker.Clear();
                throw new ContractOperationException(FulfillmentErrorCodes.DamagedConcurrencyConflict, ex);
            }

            _telemetry.RecordTransition("damaged_goods_resolution_recorded");
            return (unit, resolution.DamagedGoodsResolutionId);
        }

        throw new ContractOperationException(FulfillmentErrorCodes.DamagedConcurrencyConflict);
    }

    private async Task AttachDamagedReservationAsync(
        Guid fulfillmentId,
        Guid resolutionId,
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        unit.AttachDamagedResolutionReservation(resolutionId, reservationId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<FulfillmentUnit> CreateReplacementParcelAsync(
        Guid fulfillmentId,
        Guid resolutionId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < DamagedResolutionAttempts; attempt++)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
            var before = unit.Shipments.Select(x => x.ShipmentId).ToHashSet();
            var shipment = unit.CreateReplacementShipment(
                _ids.NewId(),
                () => _ids.NewId(),
                resolutionId,
                actorUserId,
                string.Empty,
                _clock.UtcNow);
            if (before.Contains(shipment.ShipmentId))
            {
                // The recorded occurrence already owns its replacement parcel: replay it unchanged.
                await transaction.RollbackAsync(cancellationToken);
                return unit;
            }

            _db.Shipments.Add(shipment);
            _db.ShipmentItems.AddRange(shipment.Items);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsResendDispatchKeyUniqueViolation(ex))
            {
                // A concurrent operator committed the same occurrence's parcel first; the retry reads it back.
                await SafeRollbackAsync(transaction);
                _db.ChangeTracker.Clear();
                continue;
            }
            catch (Exception ex) when (IsDamagedSerializationFailure(ex))
            {
                await SafeRollbackAsync(transaction);
                _db.ChangeTracker.Clear();
                throw new ContractOperationException(FulfillmentErrorCodes.DamagedConcurrencyConflict, ex);
            }

            return await LoadMutableAsync(fulfillmentId, cancellationToken);
        }

        throw new ContractOperationException(FulfillmentErrorCodes.DamagedConcurrencyConflict);
    }

    private static bool IsDamagedSerializationFailure(Exception exception)
    {
        // EF's execution strategy wraps the provider failure in an InvalidOperationException, so the serialization
        // failure has to be recognised ANYWHERE in the chain — otherwise two operators resolving the same damaged unit
        // would surface a raw infrastructure exception instead of the typed conflict.
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure })
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSupplyUnavailable(ContractOperationException exception) =>
        string.Equals(
            exception.Code,
            Tooba.Inventory.Contracts.Errors.InventoryErrorCodes.SupplyUnavailable,
            StringComparison.Ordinal);

    private static bool IsDamagedOccurrenceUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_damaged_goods_resolutions_fulfillment_occurrence",
        };
}
