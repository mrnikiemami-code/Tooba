namespace Tooba.Fulfillment.Domain.ValueObjects;

/// <summary>TB-265: durable lifecycle of one exclusive courier-return source claim.</summary>
public enum CourierReturnClaimStatus
{
    /// <summary>The Returns case requested this quantity; the claim holds the capacity.</summary>
    Requested = 0,

    /// <summary>The owning Returns case was approved; the branch is final.</summary>
    Approved = 1,

    /// <summary>The owning Returns case was rejected; only its branch claim was released.</summary>
    Rejected = 2,

    /// <summary>The owning Returns case was cancelled; only its branch claim was released.</summary>
    Cancelled = 3,
}

/// <summary>TB-265: condition of one claimed quantity, mirroring the physical origin receipt.</summary>
public enum CourierReturnClaimCondition
{
    /// <summary>Received and physically usable.</summary>
    Usable = 0,

    /// <summary>Received but damaged / not resellable.</summary>
    Damaged = 1,
}

/// <summary>TB-265: one claimed line/quantity/condition of a returned parcel.</summary>
public sealed record CourierReturnClaimLine(
    Guid OrderLineId,
    decimal Quantity,
    CourierReturnClaimCondition Condition);
