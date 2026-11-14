namespace Tooba.Fulfillment.Domain.ValueObjects;

/// <summary>
/// TB-265: an externally-owned claim of one returned parcel line's physically received capacity. A courier-origin
/// Returns case claims the exact quantity it funds in the owning Fulfillment schema, and that quantity must stop
/// being available to a resend or a damaged replacement. The claim rows live in their own aggregate
/// (<see cref="Aggregates.CourierReturnClaim"/>); this value object is the reduced shape the unit needs when it
/// reports or validates remaining capacity, so the aggregate never reaches into another table's lifecycle.
/// </summary>
/// <param name="ShipmentId">The returned source parcel whose receipt quantity is claimed.</param>
/// <param name="OrderLineId">The claimed order line.</param>
/// <param name="Quantity">The claimed quantity (positive).</param>
/// <param name="Condition">Usable claims consume resend capacity; damaged claims consume damaged outstanding.</param>
public readonly record struct ReturnedCapacityClaim(
    Guid ShipmentId,
    Guid OrderLineId,
    decimal Quantity,
    CourierReturnClaimCondition Condition);
