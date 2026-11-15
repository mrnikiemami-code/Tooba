using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Fulfillment.Domain.Aggregates;
using DamagedResolutionOutcome = Tooba.Fulfillment.Domain.ValueObjects.DamagedResolutionOutcome;
using DamagedResolutionStatus = Tooba.Fulfillment.Domain.ValueObjects.DamagedResolutionStatus;

namespace Tooba.Fulfillment.Infrastructure.Directories;

/// <summary>
/// TB-270-R3: owning, read-only verification that a replacement parcel's healthy stock is STILL held.
/// </summary>
/// <remarks>
/// <para>
/// The Order-side plan confirmation must never infer readiness from a projected decision status. This reader checks
/// the unit's own decision, the parcel's own line/quantity, and Inventory's CURRENT reservation status, and it writes
/// nothing. A hold that was released or consumed after the parcel was created is a typed refusal, so a plan can never
/// promise a dispatch whose physical supply no longer exists.
/// </para>
/// </remarks>
public sealed partial class FulfillmentDirectory
{
    /// <inheritdoc />
    public async Task<FulfillmentReplacementReadinessFact> GetAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        CancellationToken cancellationToken)
    {
        if (fulfillmentId == Guid.Empty || shipmentId == Guid.Empty)
        {
            return NotReady(fulfillmentId, shipmentId, FulfillmentReplacementReadinessReasonCodes.NotReplacement);
        }

        FulfillmentUnit unit;
        try
        {
            unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        }
        catch (ContractOperationException)
        {
            return NotReady(
                fulfillmentId,
                shipmentId,
                FulfillmentReplacementReadinessReasonCodes.UnitUnavailable);
        }

        var shipment = unit.Shipments.SingleOrDefault(x => x.ShipmentId == shipmentId);
        if (shipment is null || !shipment.IsReplacement)
        {
            return NotReady(fulfillmentId, shipmentId, FulfillmentReplacementReadinessReasonCodes.NotReplacement);
        }

        var sourceId = shipment.ResendOfShipmentId;
        if (sourceId is not { } source || source == Guid.Empty)
        {
            return NotReady(fulfillmentId, shipmentId, FulfillmentReplacementReadinessReasonCodes.SourceMissing);
        }

        var sourceShipment = unit.Shipments.SingleOrDefault(x => x.ShipmentId == source);
        if (sourceShipment is null)
        {
            return NotReady(fulfillmentId, shipmentId, FulfillmentReplacementReadinessReasonCodes.SourceMissing);
        }

        var decision = shipment.DamagedResolutionId is { } decisionId
            ? unit.DamagedResolutions.SingleOrDefault(x => x.DamagedGoodsResolutionId == decisionId)
            : unit.DamagedResolutions.SingleOrDefault(x => x.ReplacementShipmentId == shipmentId);
        if (decision is null)
        {
            return NotReady(fulfillmentId, shipmentId, FulfillmentReplacementReadinessReasonCodes.DecisionMissing);
        }

        if (decision.Outcome != DamagedResolutionOutcome.Replacement
            || decision.Status is not (DamagedResolutionStatus.Reserved or DamagedResolutionStatus.Fulfilled))
        {
            return NotReady(fulfillmentId, shipmentId, FulfillmentReplacementReadinessReasonCodes.DecisionNotReady);
        }

        // The decision is the supply authority: it must point at THIS source, THIS replacement and ONE reservation.
        if (decision.ShipmentId != source
            || decision.ReplacementShipmentId != shipmentId
            || decision.ReplacementReservationId is not { } reservationId
            || reservationId == Guid.Empty
            || shipment.ReplacementReservationId != reservationId)
        {
            return NotReady(fulfillmentId, shipmentId, FulfillmentReplacementReadinessReasonCodes.DecisionMismatch);
        }

        if (shipment.Items.Count != 1)
        {
            return NotReady(fulfillmentId, shipmentId, FulfillmentReplacementReadinessReasonCodes.LineMismatch);
        }

        var line = shipment.Items[0];
        if (line.OrderLineId != decision.OrderLineId || line.Quantity <= 0m)
        {
            return NotReady(fulfillmentId, shipmentId, FulfillmentReplacementReadinessReasonCodes.LineMismatch);
        }

        // Inventory is the owner of the physical hold; a projected decision status is not evidence it still exists.
        var hold = await _inventory.FindReplacementReservationAsync(reservationId, cancellationToken);
        if (hold is null)
        {
            return NotReady(
                fulfillmentId,
                shipmentId,
                FulfillmentReplacementReadinessReasonCodes.HoldReleased,
                source,
                decision.DamagedGoodsResolutionId,
                reservationId,
                line.OrderLineId,
                line.Quantity);
        }

        if (!string.Equals(hold.Status, "Held", StringComparison.OrdinalIgnoreCase))
        {
            return NotReady(
                fulfillmentId,
                shipmentId,
                FulfillmentReplacementReadinessReasonCodes.HoldReleased,
                source,
                decision.DamagedGoodsResolutionId,
                reservationId,
                line.OrderLineId,
                line.Quantity);
        }

        if (hold.Quantity != line.Quantity || hold.ReservationId != reservationId)
        {
            return NotReady(
                fulfillmentId,
                shipmentId,
                FulfillmentReplacementReadinessReasonCodes.HoldQuantityMismatch,
                source,
                decision.DamagedGoodsResolutionId,
                reservationId,
                line.OrderLineId,
                line.Quantity);
        }

        return new FulfillmentReplacementReadinessFact(
            fulfillmentId,
            shipmentId,
            true,
            FulfillmentReplacementReadinessReasonCodes.Ready,
            source,
            decision.DamagedGoodsResolutionId,
            reservationId,
            line.OrderLineId,
            line.Quantity,
            decision.UpdatedAt);
    }

    private static FulfillmentReplacementReadinessFact NotReady(
        Guid fulfillmentId,
        Guid shipmentId,
        string reasonCode,
        Guid? sourceShipmentId = null,
        Guid? damagedResolutionId = null,
        Guid? replacementReservationId = null,
        Guid? orderLineId = null,
        decimal quantity = 0m)
        => new(
            fulfillmentId,
            shipmentId,
            false,
            reasonCode,
            sourceShipmentId,
            damagedResolutionId,
            replacementReservationId,
            orderLineId,
            quantity);
}
