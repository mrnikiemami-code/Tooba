using Microsoft.EntityFrameworkCore;
using Tooba.Order.Contracts.Shipping;
using Tooba.Order.Domain.Aggregates;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Integrations.DispatchPlan;

/// <summary>
/// TB-270-R2: read-only bridge that exposes the CONFIRMED own dispatch plan of an existing physical parcel.
/// </summary>
/// <remarks>
/// Only real rows are returned. A parcel without a row is absent by design — the consumer must report it as
/// «نیازمند زمان‌بندی» and must never borrow the group's original accepted plan or a sibling's plan.
/// </remarks>
public sealed class OrderShipmentDispatchPlanReader : IOrderShipmentDispatchPlanReader
{
    private readonly OrderDbContext _db;

    /// <summary>Bridges the reader onto Order persistence.</summary>
    public OrderShipmentDispatchPlanReader(OrderDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrderShipmentDispatchPlanFact>> ListForShipmentsAsync(
        IReadOnlyList<Guid> shipmentIds,
        CancellationToken cancellationToken)
    {
        if (shipmentIds.Count == 0)
        {
            return [];
        }

        var rows = await _db.ShipmentDispatchPlans.AsNoTracking()
            .Where(x => shipmentIds.Contains(x.ShipmentId))
            .ToListAsync(cancellationToken);
        return rows.Select(ToFact).ToArray();
    }

    /// <inheritdoc />
    public async Task<OrderShipmentDispatchPlanFact?> GetForShipmentAsync(
        Guid shipmentId,
        CancellationToken cancellationToken)
    {
        var row = await _db.ShipmentDispatchPlans.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ShipmentId == shipmentId, cancellationToken);
        return row is null ? null : ToFact(row);
    }

    /// <summary>همان ردیف پایدار در شکل Contracts؛ هیچ مقداری بازمحاسبه نمی‌شود.</summary>
    public static OrderShipmentDispatchPlanFact ToFact(OrderShipmentDispatchPlan row)
        => new(
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
