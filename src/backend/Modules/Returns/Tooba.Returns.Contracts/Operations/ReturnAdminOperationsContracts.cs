namespace Tooba.Returns.Contracts.Operations;

/// <summary>Return request status exposed through the admin operations contract.</summary>
public enum ReturnRequestOperationStatus
{
    /// <summary>Requested and awaiting review.</summary>
    Requested = 0,

    /// <summary>Approved and ready for refund.</summary>
    Approved = 1,

    /// <summary>Rejected.</summary>
    Rejected = 2,

    /// <summary>Refund is processing.</summary>
    RefundProcessing = 3,

    /// <summary>Return and refund completed.</summary>
    Completed = 4,

    /// <summary>Refund failed.</summary>
    RefundFailed = 5,

    /// <summary>Cancelled by the customer.</summary>
    Cancelled = 6,
}

/// <summary>Refund destination exposed through the admin operations contract.</summary>
public enum RefundDestinationOption
{
    /// <summary>Back to the original payment method.</summary>
    OriginalPayment = 0,

    /// <summary>Credited to the customer wallet.</summary>
    Wallet = 1,
}

/// <summary>Refund attempt status exposed through the admin operations contract.</summary>
public enum RefundAttemptOperationStatus
{
    /// <summary>Awaiting the provider response.</summary>
    Pending = 0,

    /// <summary>Succeeded.</summary>
    Succeeded = 1,

    /// <summary>Failed.</summary>
    Failed = 2,
}

/// <summary>Returned quantity of one order line.</summary>
public sealed record ReturnItemSnapshot(
    Guid ReturnItemId,
    Guid OrderLineId,
    decimal Quantity,
    decimal UnitPriceSnapshot,
    string Currency,
    Guid? ReservationId,
    ReturnItemConditionOption Condition = ReturnItemConditionOption.Usable,
    ReturnItemConditionOption? DispositionCondition = null,
    Guid? SourceShipmentId = null,
    Guid? SourceReceiptId = null,
    // TB-274-R1: the EXACT inspected usable quantity this line actually restocked (null when no saleable restock was
    // recorded). A mixed line restocks only this portion, so <see cref="DispositionCondition"/> / the line quantity can
    // never be read as "the whole requested amount went back to saleable stock".
    decimal? SaleableRestockedQuantity = null,
    // TB-277: the accepted Order line's fractional precision for this unit (0 = whole pieces). Null means the owning
    // line could not be answered — a consumer must then refuse to invent a step rather than assume whole units.
    int? UnitDecimalPlaces = null);

/// <summary>One refund attempt of a return request.</summary>
public sealed record RefundAttemptSnapshot(
    Guid RefundAttemptId,
    Guid PaymentId,
    decimal Amount,
    string Currency,
    RefundAttemptOperationStatus Status,
    string IdempotencyKey,
    string? ProviderReference,
    string? FailureCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

/// <summary>Return request projection. Field order mirrors the wire contract.</summary>
public sealed record ReturnSnapshot(
    Guid ReturnRequestId,
    Guid SellerOrderId,
    Guid CheckoutId,
    Guid SellerPartyId,
    Guid RequestedByUserId,
    ReturnRequestOperationStatus Status,
    string? Reason,
    string Currency,
    decimal RefundAmount,
    Guid? PaymentId,
    RefundDestinationOption RefundDestination,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ReturnItemSnapshot> Items,
    IReadOnlyList<RefundAttemptSnapshot> RefundAttempts,
    ReturnOriginOption Origin = ReturnOriginOption.CustomerDelivered,
    Guid? SourceFulfillmentId = null,
    Guid? SourceShipmentId = null,
    Guid? SourceReceiptId = null,
    Guid? SourceClaimId = null,
    string? SourceOccurrenceKey = null,
    string ClosurePhase = "None",
    DateTimeOffset? ObligationClosedAt = null,
    DateTimeOffset? StockSettledAt = null,
    decimal ReleasedHeldQuantity = 0,
    ReturnRecoverySnapshot? Recovery = null,
    // TB-274: the receiving/inspection summary of a delivered-customer case. Null when nothing was received yet;
    // approval, refund attempt and restock are gated on this exact stored fact.
    ReturnReceivingSnapshot? Receiving = null,
    // TB-274: the reason recorded with the winning rejection decision (null for an approval).
    string? DecisionReason = null,
    // TB-275: the durable approve/reject decision recorded for this case ("Approve"/"Reject") or null when no decision
    // row exists. The workspace never invents an approval the store did not record.
    string? RecordedDecision = null,
    // TB-275: every recorded customer receipt occurrence, oldest first, so the operator can see the physical history
    // instead of a single summarized number. Empty for a courier-origin case (Fulfillment owns that receipt).
    IReadOnlyList<ReturnReceiptOccurrenceSnapshot>? Receipts = null,
    // TB-275: the ONE work stage/action description the queue and this workspace share.
    ReturnWorkSnapshot? Work = null,
    // TB-279: the EFFECTIVE decision after the append-only revision history ("Approve"/"Reject"/null when a correction
    // returned the case to review); <see cref="RecordedDecision"/> stays the immutable baseline row.
    string? EffectiveDecision = null,
    // TB-279: the monotonic reviewed version of the effective decision (0 = only the legacy baseline exists).
    int DecisionVersion = 0,
    // TB-279: every appended decision/correction fact, oldest first (the baseline row is not repeated).
    IReadOnlyList<ReturnDecisionRevisionSnapshot>? DecisionHistory = null,
    // TB-279: the reviewed per-item/condition resolution plan and the approved money derived from it.
    ReturnResolutionSnapshot? Resolution = null,
    // TB-279: the frozen approved money (null until frozen; a started refund keeps RefundAmount as the legacy baseline).
    decimal? ApprovedAmount = null,
    // TB-279: the recorded ACTUAL handbacks of rejected received goods to the owning customer.
    IReadOnlyList<ReturnCustodySnapshot>? Custody = null,
    // TB-279: whether an audited correction is available now and the exact typed reason when it is not.
    ReturnCorrectionAssessment? Correction = null);

/// <summary>TB-279: one appended decision/correction fact of the case (the legacy baseline row is version 0).</summary>
public sealed record ReturnDecisionRevisionSnapshot(
    int Version,
    string Decision,
    string? Reason,
    Guid ActorUserId,
    DateTimeOffset RecordedAt);

/// <summary>TB-279: the reviewed accepted/rejected split of ONE requested line.</summary>
public sealed record ReturnResolutionLineSnapshot(
    Guid ReturnItemId,
    decimal Requested,
    decimal Received,
    decimal Usable,
    decimal Damaged,
    decimal Accepted,
    decimal Rejected,
    string? Reason,
    int Version);

/// <summary>
/// TB-279: the effective reviewed resolution plan. <c>IsComplete</c> is a positive fact (the whole original claim is
/// accounted for) and <c>ApprovedAmount</c> is derived from the accepted quantities and the stored unit prices only.
/// </summary>
public sealed record ReturnResolutionSnapshot(
    int Version,
    bool IsComplete,
    decimal ApprovedAmount,
    bool FromReviewedPlan,
    IReadOnlyList<ReturnResolutionLineSnapshot> Lines,
    string? BlockerCode = null);

/// <summary>TB-279: one recorded ACTUAL handback of rejected received goods to the owning customer.</summary>
public sealed record ReturnCustodySnapshot(
    Guid ReturnCustodyId,
    Guid ReturnItemId,
    decimal Quantity,
    ReturnItemConditionOption Condition,
    Guid RecipientUserId,
    string ProofReference,
    DateTimeOffset RecordedAt);

/// <summary>TB-279: whether an audited correction is available and the exact typed reason when it is not.</summary>
public sealed record ReturnCorrectionAssessment(bool CanReopenForReview, string? BlockerCode);

/// <summary>
/// TB-275: the operator work stage of one case, as the wire reports it. <c>CategoryCode</c> is a stable token
/// (<c>receiving</c>/<c>decision</c>/<c>refund</c>/<c>refund_failed</c>/<c>disposition</c>/<c>settled</c>/<c>closed</c>)
/// and <c>ActionCodes</c> names only the intents the module actually offers for the stored facts.
/// </summary>
public sealed record ReturnWorkSnapshot(
    string CategoryCode,
    string? BlockerCode,
    Guid? PendingRefundAttemptId,
    Guid? FailedRefundAttemptId,
    bool CanRecordReceipt,
    bool CanDecide,
    bool CanResumeRefund,
    bool CanRetryRefund,
    bool CanSettlePhysical,
    bool IsOpen,
    bool NeedsOperator,
    // TB-277: an approved case that owns no refund attempt may deliberately start its FIRST refund. Distinct from
    // resume (an unknown pending attempt) and retry (a definite failure). Appended so prior members keep their meaning.
    IReadOnlyList<string> ActionCodes,
    bool CanStartRefund = false);

/// <summary>TB-275: one inspected line inside a recorded customer receipt occurrence.</summary>
public sealed record ReturnReceiptLineSnapshot(
    Guid ReturnItemId,
    decimal Quantity,
    ReturnItemConditionOption Condition);

/// <summary>TB-275: ONE recorded physical customer receipt (header + inspected lines), as an immutable occurrence.</summary>
public sealed record ReturnReceiptOccurrenceSnapshot(
    Guid ReturnReceiptId,
    string OccurrenceKey,
    Guid ReceivedByUserId,
    DateTimeOffset RecordedAt,
    IReadOnlyList<ReturnReceiptLineSnapshot> Lines);

/// <summary>TB-274: one inspected line of the recorded customer receipt coverage.</summary>
public sealed record ReturnReceivingLineSnapshot(
    Guid ReturnItemId,
    decimal Requested,
    decimal Received,
    decimal Usable,
    decimal Damaged);

/// <summary>
/// TB-274: the additive read description of what the customer physically returned. <c>IsComplete</c> is true only
/// when every requested unit was received and <c>IsCoherent</c> is false whenever the stored rows contradict the
/// request (over-receipt or a foreign line).
/// </summary>
public sealed record ReturnReceivingSnapshot(
    IReadOnlyList<ReturnReceivingLineSnapshot> Lines,
    bool IsComplete,
    bool IsCoherent);


/// <summary>Remaining returnable quantity of one order line.</summary>
public sealed record ReturnLineEligibility(
    Guid OrderLineId,
    decimal DeliveredQuantity,
    decimal AlreadyReturnedQuantity,
    decimal RemainingReturnableQuantity);

/// <summary>Authoritative return eligibility of one seller order.</summary>
public sealed record ReturnEligibilityResult(
    Guid SellerOrderId,
    Guid CheckoutId,
    bool Eligible,
    string ReasonCode,
    DateTimeOffset? EligibleUntil,
    DateTimeOffset? LastDeliveredAt,
    IReadOnlyList<ReturnLineEligibility> Lines);

/// <summary>Lightweight return status row for the admin orders grid overlay.</summary>
public sealed record ReturnStatusOverlayRow(Guid SellerOrderId, ReturnRequestOperationStatus Status);

/// <summary>One requested return line.</summary>
public sealed record ReturnLineCommand(Guid OrderLineId, decimal Quantity);

/// <summary>Admin-initiated return creation payload.</summary>
public sealed record CreateAdminReturnCommand(
    Guid SellerOrderId,
    Guid ActorUserId,
    string IdempotencyKey,
    string? Reason,
    IReadOnlyList<ReturnLineCommand> Items);

/// <summary>
/// Returns operations required by admin order lifecycle, exposed without owner
/// Application/Domain types. Implemented by Returns.Infrastructure.
/// </summary>
public interface IReturnAdminOperations
{
    /// <summary>Return requests of the given seller orders.</summary>
    Task<IReadOnlyList<ReturnSnapshot>> ListBySellerOrderIdsAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);

    /// <summary>Return status rows for the admin orders grid overlay.</summary>
    Task<IReadOnlyList<ReturnStatusOverlayRow>> ListStatusOverlayAsync(CancellationToken cancellationToken);

    /// <summary>Authoritative return eligibility of one seller order.</summary>
    Task<ReturnEligibilityResult> EvaluateEligibilityAsync(Guid sellerOrderId, CancellationToken cancellationToken);

    /// <summary>Create an admin-initiated return request.</summary>
    Task<ReturnSnapshot> CreateAdminInitiatedAsync(
        CreateAdminReturnCommand command,
        CancellationToken cancellationToken);

    /// <summary>Approve a return request. TB-277: this records the DECISION only and never executes money.</summary>
    Task<ReturnSnapshot> ApproveAsync(Guid returnRequestId, Guid actorUserId, CancellationToken cancellationToken);

    /// <summary>
    /// TB-277: deliberately execute the FIRST refund of an approved case that owns no attempt yet. This is a real
    /// financial action (money moves); it is neither the unknown-pending resume nor the failed-attempt retry.
    /// </summary>
    Task<ReturnSnapshot> StartFirstRefundAsync(Guid returnRequestId, Guid actorUserId, CancellationToken cancellationToken);

    /// <summary>Reject a return request.</summary>
    Task<ReturnSnapshot> RejectAsync(
        Guid returnRequestId,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken);

    /// <summary>Retry a failed refund.</summary>
    Task<ReturnSnapshot> RetryRefundAsync(Guid returnRequestId, Guid actorUserId, CancellationToken cancellationToken);

    /// <summary>
    /// TB-273: explicitly recover ONE already-approved case. <paramref name="kind"/> is the reviewed intent
    /// (<c>resume_pending_refund</c> repeats the exact stored pending attempt; <c>settle_physical_disposition</c>
    /// performs the physical phase only) and <paramref name="expectedRefundAttemptId"/> is the exact stored attempt the
    /// caller reviewed. It never mints a new attempt, changes a destination/amount or approves a case.
    /// </summary>
    Task<ReturnSnapshot> RecoverRefundAsync(
        Guid returnRequestId,
        Guid actorUserId,
        ReturnRecoveryKindOption kind,
        Guid? expectedRefundAttemptId,
        CancellationToken cancellationToken);

    /// <summary>
    /// TB-265: create a courier-origin case from an already physically received returned parcel. The received
    /// quantity is claimed durably in Fulfillment FIRST (exclusive across resend/replacement/refund), the receipt is
    /// reused, and the case starts Requested with NO money and NO stock movement.
    /// </summary>
    Task<CourierOriginReturnResult> CreateCourierOriginAsync(
        CreateCourierOriginReturnCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    /// TB-265: read-only exact-intent reconciliation for a courier-origin creation whose response was lost. It answers
    /// the recorded case or the recorded conflict and never creates a second case.
    /// </summary>
    Task<CourierOriginReturnResult?> ReconcileCourierOriginAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        string occurrenceKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// TB-274: record ONE physical customer receipt occurrence (received + inspected quantities) of a
    /// delivered-customer case. It moves no money/stock, refuses over-receiving and answers an exact replay from the
    /// stored occurrence.
    /// </summary>
    Task<ReturnSnapshot> RecordCustomerReceiptAsync(
        RecordCustomerReceiptCommand command,
        CancellationToken cancellationToken);
}

/// <summary>TB-274: one inspected line of a customer receipt submitted through the admin operations contract.</summary>
public sealed record CustomerReceiptLineCommand(
    Guid ReturnItemId,
    decimal Quantity,
    ReturnItemConditionOption Condition);

/// <summary>
/// TB-274: reviewed receiving intent. <paramref name="OccurrenceKey"/> is the durable identity of the delivery, so a
/// retried/lost-response submission replays the stored receipt instead of receiving the same units twice.
/// </summary>
public sealed record RecordCustomerReceiptCommand(
    Guid ReturnRequestId,
    Guid ActorUserId,
    string OccurrenceKey,
    IReadOnlyList<CustomerReceiptLineCommand> Items);

/// <summary>Stable eligibility reason codes and their stable API error codes.</summary>
public static class ReturnEligibilityReasons
{
    /// <summary>Order is not paid.</summary>
    public const string NotPaid = "not_paid";

    /// <summary>Nothing delivered yet.</summary>
    public const string NotDelivered = "not_delivered";

    /// <summary>Return window expired.</summary>
    public const string WindowExpired = "window_expired";

    /// <summary>Nothing returnable left.</summary>
    public const string NothingReturnable = "nothing_returnable";

    /// <summary>Line policy snapshot forbids returns.</summary>
    public const string NonReturnable = "non_returnable";

    /// <summary>Eligible.</summary>
    public const string Eligible = "eligible";

    /// <summary>Order not found.</summary>
    public const string OrderMissing = "order_missing";

    /// <summary>Fulfillment not found.</summary>
    public const string FulfillmentMissing = "fulfillment_missing";

    /// <summary>
    /// TB-278: the owning order line did not answer a unit precision, so no quantity of this line can be selected or
    /// created lawfully. The line is blocked instead of assuming a default scale.
    /// </summary>
    public const string UnitPrecisionUnavailable = "unit_precision_unavailable";

    /// <summary>Stable API error code of a reason code.</summary>
    public static string ToErrorCode(string reasonCode) => reasonCode switch
    {
        WindowExpired => "return.expired",
        NonReturnable => "return.non_returnable",
        NothingReturnable => "return.quantity_exceeded",
        NotDelivered => "return.not_delivered",
        NotPaid => "return.not_paid",
        OrderMissing => "return.missing",
        FulfillmentMissing => "return.fulfillment_missing",
        UnitPrecisionUnavailable => Errors.ReturnsErrorCodes.QuantityUnitInvalid,
        _ => "return.rejected",
    };
}

// ---------------------------------------------------------------------------------------------------------------
// TB-265: courier-origin cases linked to the EXISTING Returns review/refund/disposition workflow.
//
// Existing rows keep their customer-delivered semantics. A courier-origin case is created from a returned parcel
// whose physical origin receipt already exists: the receipt is REUSED (never received twice), the case starts
// Requested, creation moves no money and releases no stock, and approval closes the corresponding order-line
// delivery obligation before the existing refund path runs.
// ---------------------------------------------------------------------------------------------------------------

/// <summary>TB-265: origin discriminator exposed through the admin operations contract.</summary>
public enum ReturnOriginOption
{
    /// <summary>Customer-delivered RMA (historical semantics).</summary>
    CustomerDelivered = 0,

    /// <summary>Courier-origin receipt (parcel physically back at the seller).</summary>
    CourierOrigin = 1,
}

/// <summary>TB-265: physical condition of a returned line.</summary>
public enum ReturnItemConditionOption
{
    /// <summary>Usable / resellable.</summary>
    Usable = 0,

    /// <summary>Damaged / never restocked as saleable.</summary>
    Damaged = 1,
}

/// <summary>TB-265: one selected received line of a courier-origin case.</summary>
public sealed record CourierOriginReturnLineCommand(
    Guid OrderLineId,
    decimal Quantity,
    ReturnItemConditionOption Condition = ReturnItemConditionOption.Usable);

/// <summary>TB-265: reviewed creation intent for a courier-origin case.</summary>
public sealed record CreateCourierOriginReturnCommand(
    Guid FulfillmentId,
    Guid ShipmentId,
    Guid ActorUserId,
    string IdempotencyKey,
    string OccurrenceKey,
    string? Reason,
    IReadOnlyList<CourierOriginReturnLineCommand> Items);

/// <summary>TB-265: answer of a courier-origin creation/reconciliation attempt.</summary>
public sealed record CourierOriginReturnResult(
    ReturnSnapshot Snapshot,
    Guid SourceClaimId,
    bool AlreadyExisted = false,
    string? ConflictCode = null);
