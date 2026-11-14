namespace Tooba.Fulfillment.Domain.ValueObjects;

/// <summary>
/// TB-265-R1: durable phase of one courier-origin claim allocation.
/// </summary>
public enum CourierReturnAllocationState
{
    /// <summary>Recorded before any parcel cancellation or stock release (recoverable).</summary>
    Pending = 0,

    /// <summary>The branch is closed and the allocation's own held quantity was released exactly once.</summary>
    Released = 1,
}
