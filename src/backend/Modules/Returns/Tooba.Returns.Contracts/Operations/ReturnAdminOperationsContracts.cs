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
    Guid? SourceReceiptId = null);

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
    decimal ReleasedHeldQuantity = 0);

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

    /// <summary>Approve a return request and start the refund.</summary>
    Task<ReturnSnapshot> ApproveAsync(Guid returnRequestId, Guid actorUserId, CancellationToken cancellationToken);

    /// <summary>Reject a return request.</summary>
    Task<ReturnSnapshot> RejectAsync(
        Guid returnRequestId,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken);

    /// <summary>Retry a failed refund.</summary>
    Task<ReturnSnapshot> RetryRefundAsync(Guid returnRequestId, Guid actorUserId, CancellationToken cancellationToken);

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
}

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
