using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Domain.Aggregates;

/// <summary>
/// TB-265-R1: the durable per-quantity allocation of ONE approved courier-origin claim line.
/// <para>
/// Approval settlement must be source-exact: each claimed quantity is bound to its OWN source parcel/receipt, to the
/// owning damaged decision (when there is one), to the replacement parcel it cancels (when there is one) and to the
/// exact held reservation it releases. That is what stops a case for parcel A from cancelling or releasing parcel B's
/// stock, and what stops two lines sharing a hold from being collapsed onto the first reservation.
/// </para>
/// </summary>
public sealed class CourierReturnClaimAllocation
{
    private CourierReturnClaimAllocation()
    {
    }

    /// <summary>Stable identity of this allocation.</summary>
    public Guid AllocationId { get; init; }

    /// <summary>Owning claim.</summary>
    public Guid ClaimId { get; init; }

    /// <summary>Source parcel the quantity physically came back on.</summary>
    public Guid SourceShipmentId { get; init; }

    /// <summary>Single origin receipt that is reused (never re-received).</summary>
    public Guid SourceReceiptId { get; init; }

    /// <summary>Order line.</summary>
    public Guid OrderLineId { get; init; }

    /// <summary>Physical condition of the allocated quantity.</summary>
    public CourierReturnClaimCondition Condition { get; init; }

    /// <summary>Allocated quantity.</summary>
    public decimal Quantity { get; init; }

    /// <summary>Owning damaged decision, if this quantity belongs to one.</summary>
    public Guid? DamagedResolutionId { get; init; }

    /// <summary>The not-yet-dispatched replacement parcel cancelled for this quantity.</summary>
    public Guid? CancelledReplacementShipmentId { get; private set; }

    /// <summary>
    /// The exact held reservation this allocation releases. It is the decision's OWN replacement hold for a damaged
    /// unit and the line's own hold for a usable unit; it is NEVER a sibling line's reservation.
    /// </summary>
    public Guid? HeldReservationId { get; init; }

    /// <summary>Quantity actually released from that hold (0 when nothing was still held).</summary>
    public decimal ReleasedQuantity { get; private set; }

    /// <summary>Durable phase of this allocation.</summary>
    public CourierReturnAllocationState State { get; private set; } = CourierReturnAllocationState.Pending;

    /// <summary>When recorded.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Last change.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Records a planned allocation before any parcel/stock effect.</summary>
    public static CourierReturnClaimAllocation Plan(
        Guid allocationId,
        Guid claimId,
        Guid sourceShipmentId,
        Guid sourceReceiptId,
        Guid orderLineId,
        CourierReturnClaimCondition condition,
        decimal quantity,
        Guid? damagedResolutionId,
        Guid? cancelledReplacementShipmentId,
        Guid? heldReservationId,
        DateTimeOffset now) =>
        new()
        {
            AllocationId = allocationId,
            ClaimId = claimId,
            SourceShipmentId = sourceShipmentId,
            SourceReceiptId = sourceReceiptId,
            OrderLineId = orderLineId,
            Condition = condition,
            Quantity = quantity,
            DamagedResolutionId = damagedResolutionId,
            CancelledReplacementShipmentId = cancelledReplacementShipmentId,
            HeldReservationId = heldReservationId,
            State = CourierReturnAllocationState.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };

    /// <summary>The selected replacement parcel was really cancelled (idempotent).</summary>
    public void MarkBranchClosed(Guid cancelledReplacementShipmentId, DateTimeOffset now)
    {
        CancelledReplacementShipmentId ??= cancelledReplacementShipmentId;
        UpdatedAt = now;
    }

    /// <summary>Records the exact released amount once; the first durable settlement is the answer.</summary>
    public void MarkReleased(decimal releasedQuantity, DateTimeOffset now)
    {
        if (State == CourierReturnAllocationState.Released)
        {
            UpdatedAt = now;
            return;
        }

        ReleasedQuantity = releasedQuantity > 0m ? releasedQuantity : 0m;
        State = CourierReturnAllocationState.Released;
        UpdatedAt = now;
    }
}
