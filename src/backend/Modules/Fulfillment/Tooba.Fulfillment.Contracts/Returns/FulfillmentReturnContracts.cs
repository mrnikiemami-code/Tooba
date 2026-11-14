namespace Tooba.Fulfillment.Contracts.Returns;

/// <summary>
/// برش تحویل یک خط در یک مرسولهٔ Delivered (ساعت مرجوعی per-slice).
/// </summary>
public sealed record LineDeliverySlice(
    Guid OrderLineId,
    decimal Quantity,
    DateTimeOffset DeliveredAt);

/// <summary>
/// snapshot eligibility مرجوعی از fulfillment.
/// </summary>
public sealed record FulfillmentReturnEligibilitySnapshot(
    Guid SellerOrderId,
    IReadOnlyDictionary<Guid, decimal> DeliveredQuantities,
    DateTimeOffset? LastDeliveredAt,
    IReadOnlyDictionary<Guid, DateTimeOffset>? LineDeliveredAt = null,
    IReadOnlyList<LineDeliverySlice>? DeliverySlices = null);

/// <summary>
/// خواندن evidence تحویل برای Returns بدون cross-DbContext.
/// </summary>
public interface IFulfillmentReturnReader
{
    /// <summary>
    /// snapshot eligibility مرجوعی را برمی‌گرداند.
    /// </summary>
    Task<FulfillmentReturnEligibilitySnapshot?> GetEligibilityAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken);

    /// <summary>آخرین DeliveredAt per SellerOrderId برای گرید مرجوعی.</summary>
    Task<IReadOnlyDictionary<Guid, DateTimeOffset?>> GetLastDeliveredAtBySellerOrderIdsAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);
}

// ---------------------------------------------------------------------------------------------------------------
// TB-265: courier-origin receipt -> existing Returns linkage. Fulfillment owns the physical origin receipt, the
// source-safe resend capacity and the damaged/replacement branches, so it is the ONLY authority that can say how
// much of a returned parcel's received quantity is still free. A Returns case, a resend and a replacement all draw
// from that SAME pool, so a case must claim its quantity durably here BEFORE it exists.
// ---------------------------------------------------------------------------------------------------------------

/// <summary>TB-265: durable lifecycle of one exclusive source-quantity claim.</summary>
public enum CourierReturnClaimStatus
{
    /// <summary>The Returns case requested this quantity; the claim holds the capacity.</summary>
    Requested = 0,

    /// <summary>The owning Returns case was approved; the branch is closed for good.</summary>
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

/// <summary>TB-265: the reviewed exact intent that asks to claim source capacity for a Returns case.</summary>
public sealed record CreateCourierReturnClaimCommand(
    Guid FulfillmentId,
    Guid ShipmentId,
    Guid ActorUserId,
    string OccurrenceKey,
    IReadOnlyList<CourierReturnClaimLine> Lines);

/// <summary>TB-265: durable claim projection.</summary>
public sealed record CourierReturnClaimSnapshot(
    Guid ClaimId,
    Guid FulfillmentId,
    Guid ShipmentId,
    Guid ShipmentReturnReceiptId,
    string OccurrenceKey,
    CourierReturnClaimStatus Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<CourierReturnClaimLine> Lines,
    Guid? ReturnRequestId = null);

/// <summary>TB-265: answer of a claim/link/release attempt. A conflict is typed and writes nothing.</summary>
public sealed record CourierReturnClaimResult(
    CourierReturnClaimSnapshot? Claim = null,
    string? ConflictCode = null);

/// <summary>TB-265: answer of the approval settlement (branch closure + once-only held release).</summary>
public sealed record CourierReturnSettlement(
    Guid ClaimId,
    bool Settled,
    decimal ReleasedHeldQuantity = 0,
    Guid? CancelledReplacementShipmentId = null,
    Guid? ClosedReplacementResolutionId = null,
    Guid? HeldReservationId = null,
    string? BlockerCode = null,
    CourierReturnClaimSnapshot? Claim = null);

/// <summary>
/// TB-265: Fulfillment-owned authority for courier-origin Returns cases. It answers the one question a Returns case
/// must never answer for itself: is this exact received quantity still free, or is it already funding a resend,
/// a replacement or another case?
/// </summary>
public interface IFulfillmentCourierReturnSource
{
    /// <summary>
    /// Claims the selected received quantity durably. An exact replay answers the recorded claim; a different intent
    /// under the same occurrence key is a typed conflict with zero writes; over-claiming the free capacity is refused.
    /// </summary>
    Task<CourierReturnClaimResult> ClaimAsync(
        CreateCourierReturnClaimCommand command,
        CancellationToken cancellationToken);

    /// <summary>Reads one claim (read-only, used by reconciliation and the case detail link).</summary>
    Task<CourierReturnClaimSnapshot?> GetClaimAsync(Guid claimId, CancellationToken cancellationToken);

    /// <summary>Claims recorded for one returned parcel (read-only projection for the operator surface).</summary>
    Task<IReadOnlyList<CourierReturnClaimSnapshot>> ListForShipmentAsync(
        Guid shipmentId,
        CancellationToken cancellationToken);

    /// <summary>Links the created Returns case to its claim exactly once.</summary>
    Task<CourierReturnClaimSnapshot> LinkCaseAsync(
        Guid claimId,
        Guid returnRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases ONLY this branch's claim (Rejected/Cancelled case) so the same received quantity becomes lawfully
    /// available again. It never touches the active order's stock hold.
    /// </summary>
    Task<CourierReturnClaimResult> ReleaseAsync(
        Guid claimId,
        Guid actorUserId,
        string state,
        CancellationToken cancellationToken);

    /// <summary>
    /// Approval settlement: closes the courier branch, cancels an eligible not-yet-dispatched Created replacement
    /// allocation, and releases the unconsumed held quantity of the corresponding line exactly once. Refuses an
    /// already dispatched/in-transit replacement instead of pretending it is unused stock.
    /// </summary>
    Task<CourierReturnSettlement> SettleApprovedAsync(
        Guid claimId,
        Guid returnRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// TB-265-R2: records the exact executable allocation of this claim BEFORE any irreversible closure.
    /// <para>
    /// Returns refuses to close the Order obligation until the source branch is proven runnable (its replacement parcel
    /// is still cancellable, its quantities are exact, a partial selection is representable). A blocker is returned as
    /// data with zero writes, so an accepted claim can never become an unsupported branch after its obligation closed.
    /// The recorded allocation is durable and settlement resumes it verbatim.
    /// </para>
    /// </summary>
    Task<CourierReturnSettlement> EnsureAllocationsAsync(
        Guid claimId,
        Guid returnRequestId,
        Guid actorUserId,
        CancellationToken cancellationToken);
}
