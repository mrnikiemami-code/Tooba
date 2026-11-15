using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Domain.Aggregates;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Admin.Operations;

/// <summary>
/// TB-270-R2: Order-owned persistence of confirmed per-parcel dispatch plans.
/// </summary>
/// <remarks>
/// The immutable row is the only write. A concurrent confirmation of the SAME parcel is arbitrated by the unique
/// <c>shipment_id</c> index: the loser re-reads the winner and answers exact-replay when the intent matches, or a
/// typed conflict when it does not. Rows are never updated or deleted here.
/// </remarks>
public sealed class OrderShipmentDispatchPlanStore : IOrderShipmentDispatchPlanStore
{
    private readonly OrderDbContext _db;

    /// <summary>سازندهٔ مخزن.</summary>
    public OrderShipmentDispatchPlanStore(OrderDbContext db) => _db = db;

    /// <inheritdoc />
    public Task<OrderShipmentDispatchPlan?> FindAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        _db.ShipmentDispatchPlans
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ShipmentId == shipmentId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrderShipmentDispatchPlan>> ListAsync(
        IReadOnlyList<Guid> shipmentIds,
        CancellationToken cancellationToken)
    {
        if (shipmentIds.Count == 0)
        {
            return [];
        }

        return await _db.ShipmentDispatchPlans
            .AsNoTracking()
            .Where(x => shipmentIds.Contains(x.ShipmentId))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OrderShipmentDispatchPlanInsertOutcome> TryInsertAsync(
        OrderShipmentDispatchPlan plan,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var existing = await FindAsync(plan.ShipmentId, cancellationToken);
        if (existing is not null)
        {
            return existing.HasSameIntent(plan)
                ? OrderShipmentDispatchPlanInsertOutcome.DuplicateExact
                : OrderShipmentDispatchPlanInsertOutcome.DuplicateConflict;
        }

        _db.ShipmentDispatchPlans.Add(plan);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return OrderShipmentDispatchPlanInsertOutcome.Inserted;
        }
        catch (DbUpdateException ex) when (IsUniqueShipmentViolation(ex))
        {
            // The winner committed concurrently; drop our failed insertion and answer its exact recorded fact.
            _db.Entry(plan).State = EntityState.Detached;
            var winner = await FindAsync(plan.ShipmentId, cancellationToken);
            if (winner is null)
            {
                throw;
            }

            return winner.HasSameIntent(plan)
                ? OrderShipmentDispatchPlanInsertOutcome.DuplicateExact
                : OrderShipmentDispatchPlanInsertOutcome.DuplicateConflict;
        }
    }

    private static bool IsUniqueShipmentViolation(DbUpdateException ex)
        => ex.InnerException is Npgsql.PostgresException { SqlState: "23505" };
}

/// <summary>
/// TB-270-R2: reads the accepted evidence binding of the seller's OWN committed shipping group.
/// </summary>
/// <remarks>
/// The values are the immutable captured references the scheduling lookup requires. A group with no accepted
/// evidence (historical row) or with no captured service code yields <c>null</c>/no binding instead of inventing one,
/// so the caller reports a truthful blocker rather than rebinding to a newer draft.
/// </remarks>
public sealed class OrderAcceptedShipmentBindingReader : IOrderAcceptedShipmentBindingReader
{
    private readonly OrderDbContext _db;

    /// <summary>سازندهٔ خوانندهٔ شاهد.</summary>
    public OrderAcceptedShipmentBindingReader(OrderDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<AcceptedShipmentGroupBinding?> GetAsync(
        Guid checkoutId,
        Guid shippingGroupId,
        CancellationToken cancellationToken)
    {
        if (checkoutId == Guid.Empty || shippingGroupId == Guid.Empty)
        {
            return null;
        }

        var row = await _db.OrderShippingGroups
            .AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId
                && x.ShippingGroupId == shippingGroupId
                && x.AcceptedEvidence != null)
            .Select(x => new
            {
                x.CheckoutId,
                x.SellerOrderId,
                x.SellerPartyId,
                x.ShippingGroupId,
                x.MethodCode,
                x.MethodLabel,
                x.ServiceCode,
                x.OptionCode,
                x.AcceptedEvidence!.SnapshotId,
                x.AcceptedEvidence.DraftId,
                x.AcceptedEvidence.SourceRevision,
                x.AcceptedEvidence.FormatVersion,
                x.AcceptedEvidence.SelectionVersion,
                x.AcceptedEvidence.Owner,
                x.AcceptedEvidence.AreaGroupId,
                x.AcceptedEvidence.RateRowId,
                x.AcceptedEvidence.ReceivingDate,
                x.AcceptedEvidence.ReceivingOpenTicks,
                x.AcceptedEvidence.ReceivingCloseTicks,
                x.AcceptedEvidence.ReceivingTimeZone,
                x.AcceptedEvidence.MinimumDeliveryDays,
                x.AcceptedEvidence.MaximumDeliveryDays,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        return new AcceptedShipmentGroupBinding(
            row.CheckoutId,
            row.SellerOrderId,
            row.SellerPartyId,
            row.ShippingGroupId,
            row.MethodCode,
            row.MethodLabel,
            row.ServiceCode ?? string.Empty,
            row.OptionCode,
            row.Owner.Kind,
            row.Owner.TenantKey,
            row.Owner.SellerPartyId,
            row.SnapshotId,
            row.DraftId,
            row.SourceRevision,
            row.FormatVersion,
            row.SelectionVersion,
            row.AreaGroupId,
            row.RateRowId,
            row.ReceivingDate,
            row.ReceivingOpenTicks,
            row.ReceivingCloseTicks,
            row.ReceivingTimeZone,
            row.MinimumDeliveryDays,
            row.MaximumDeliveryDays);
    }
}
