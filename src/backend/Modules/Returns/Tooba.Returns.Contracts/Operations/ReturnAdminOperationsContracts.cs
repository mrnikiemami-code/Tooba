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
    Guid? ReservationId);

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
    IReadOnlyList<RefundAttemptSnapshot> RefundAttempts);

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
