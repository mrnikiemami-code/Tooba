namespace Tooba.Fulfillment.Contracts.Operations;

/// <summary>
/// TB-270-R3: the stable reasons a replacement parcel is (or is not) physically ready for its own dispatch plan.
/// </summary>
/// <remarks>
/// These are OWNING Fulfillment/Inventory facts. Order never reads Fulfillment/Inventory tables and never decides
/// readiness from a projected status: it asks this reader once and reports the exact reason as a typed blocker.
/// </remarks>
public static class FulfillmentReplacementReadinessReasonCodes
{
    /// <summary>The parcel is a linked replacement with a live, held, exactly matching supply reservation.</summary>
    public const string Ready = "fulfillment.replacement_readiness.ready";

    /// <summary>The unit could not be loaded at all (unknown/removed fulfillment).</summary>
    public const string UnitUnavailable = "fulfillment.replacement_readiness.unit_unavailable";

    /// <summary>The named parcel does not belong to the unit or is not a replacement parcel.</summary>
    public const string NotReplacement = "fulfillment.replacement_readiness.not_replacement";

    /// <summary>The replacement parcel does not record its source parcel.</summary>
    public const string SourceMissing = "fulfillment.replacement_readiness.source_missing";

    /// <summary>The source parcel has no damaged-goods decision that produced THIS replacement.</summary>
    public const string DecisionMissing = "fulfillment.replacement_readiness.decision_missing";

    /// <summary>The decision still owes its stock (not <c>Reserved</c>/<c>Fulfilled</c>).</summary>
    public const string DecisionNotReady = "fulfillment.replacement_readiness.decision_not_ready";

    /// <summary>The decision's source parcel / replacement parcel / reservation identity disagrees.</summary>
    public const string DecisionMismatch = "fulfillment.replacement_readiness.decision_mismatch";

    /// <summary>The physical parcel lines are not the decision's exact line/quantity.</summary>
    public const string LineMismatch = "fulfillment.replacement_readiness.line_mismatch";

    /// <summary>Inventory no longer holds the reservation (released or consumed).</summary>
    public const string HoldReleased = "fulfillment.replacement_readiness.hold_released";

    /// <summary>Inventory returned a different quantity for that reservation.</summary>
    public const string HoldQuantityMismatch = "fulfillment.replacement_readiness.hold_quantity_mismatch";
}

/// <summary>
/// TB-270-R3: the owning fact "this replacement parcel is ready for its own dispatch plan, and why".
/// </summary>
/// <param name="FulfillmentId">The owning unit.</param>
/// <param name="ShipmentId">The exact replacement parcel asked about.</param>
/// <param name="IsReady">True only when every owning fact below was verified now.</param>
/// <param name="ReasonCode">One of <see cref="FulfillmentReplacementReadinessReasonCodes"/>.</param>
/// <param name="SourceShipmentId">The origin parcel the replacement compensates.</param>
/// <param name="DamagedResolutionId">The decision that funds this parcel.</param>
/// <param name="ReplacementReservationId">The parcel's own purpose-bound healthy-stock reservation.</param>
/// <param name="OrderLineId">The single order line the decision covers.</param>
/// <param name="Quantity">The exact quantity covered (decision, parcel and live hold agree).</param>
/// <param name="ReadyAtUtc">The decision's own last-changed instant (the readiness anchor, never "now").</param>
public sealed record FulfillmentReplacementReadinessFact(
    Guid FulfillmentId,
    Guid ShipmentId,
    bool IsReady,
    string ReasonCode,
    Guid? SourceShipmentId = null,
    Guid? DamagedResolutionId = null,
    Guid? ReplacementReservationId = null,
    Guid? OrderLineId = null,
    decimal Quantity = 0m,
    DateTimeOffset? ReadyAtUtc = null);

/// <summary>
/// TB-270-R3: read-only owning verification that a replacement parcel's supply is still held.
/// </summary>
public interface IFulfillmentReplacementReadinessReader
{
    /// <summary>Verifies the exact parcel against its decision, its lines and Inventory's CURRENT hold.</summary>
    Task<FulfillmentReplacementReadinessFact> GetAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        CancellationToken cancellationToken);
}
