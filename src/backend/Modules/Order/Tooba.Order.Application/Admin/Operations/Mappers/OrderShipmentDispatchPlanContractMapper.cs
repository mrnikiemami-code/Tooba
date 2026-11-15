using Tooba.Order.Contracts.Shipping;
using Tooba.Order.Domain.Aggregates;

namespace Tooba.Order.Application.Admin.Operations.Mappers;

/// <summary>
/// TB-270-R3: ONE domain → Contracts mapping for a confirmed per-parcel dispatch plan.
/// </summary>
/// <remarks>
/// Both the read adapter and the in-module action projection use this so a projected plan and a read plan can never
/// describe different identities. It copies stored values only; nothing is recomputed.
/// </remarks>
public static class OrderShipmentDispatchPlanContractMapper
{
    /// <summary>همان ردیف پایدار در شکل Contracts.</summary>
    public static OrderShipmentDispatchPlanFact ToFact(OrderShipmentDispatchPlan row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new OrderShipmentDispatchPlanFact(
            row.PlanId,
            row.ShipmentId,
            row.CheckoutId,
            row.SellerOrderId,
            row.FulfillmentId,
            row.ShippingGroupId,
            row.AttemptKind,
            row.SourceShipmentId,
            row.DamagedResolutionId,
            row.ReplacementReservationId,
            row.SourceFingerprint,
            row.SnapshotId,
            row.DraftId,
            row.SourceRevision,
            row.FormatVersion,
            row.SelectionVersion,
            row.AreaGroupId,
            row.RateRowId,
            row.MethodCode,
            row.MethodLabel,
            row.ServiceCode,
            row.OptionCode,
            row.CustomerReceivingDate,
            row.CustomerReceivingOpenTicks,
            row.CustomerReceivingCloseTicks,
            row.CustomerReceivingTimeZone,
            row.HandoffDate,
            row.HandoffOpenTicks,
            row.HandoffCloseTicks,
            row.HandoffTimeZone,
            row.PreparationStartUtc,
            row.TransferDepartureUtc,
            row.HandoffDeadlineUtc,
            row.PreparationMinutes,
            row.HandoffMinutes,
            row.TransportWorkingDays,
            row.PolicyVersion,
            row.ReadinessSource,
            row.ReadinessAtUtc,
            row.OccurrenceKey,
            row.ConfirmedByUserId,
            row.ConfirmedAtUtc);
    }
}
