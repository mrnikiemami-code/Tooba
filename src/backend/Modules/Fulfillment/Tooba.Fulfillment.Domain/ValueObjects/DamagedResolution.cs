namespace Tooba.Fulfillment.Domain.ValueObjects;


/// <summary>
/// TB-262: the operator's reviewed decision for physically damaged goods received back at origin. Damaged units are
/// never usable resend capacity and never return to saleable stock; they are either replaced from healthy stock or
/// handed to the existing customer-service / financial path as a pending referral.
/// </summary>
public enum DamagedResolutionOutcome
{
    /// <summary>Replace the SAME ordered item from healthy stock under a NEW purpose-bound reservation.</summary>
    Replacement = 0,

    /// <summary>No replacement now; the item stays a pending customer-service / financial referral.</summary>
    Referral = 1,
}

/// <summary>TB-262: durable state of one damaged-goods decision.</summary>
public enum DamagedResolutionStatus
{
    /// <summary>Decision recorded; a replacement still owes healthy stock (or the item is a pending referral).</summary>
    Recorded = 0,

    /// <summary>The replacement's own healthy-stock reservation is linked and held.</summary>
    Reserved = 1,

    /// <summary>The linked replacement parcel exists and is moving through the ordinary dispatch lifecycle.</summary>
    Fulfilled = 2,

    /// <summary>The operator explicitly released the pending replacement before dispatch.</summary>
    Cancelled = 3,
}
