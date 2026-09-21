namespace Tooba.Returns.Contracts.Errors;

/// <summary>Stable semantic error codes owned by Returns.</summary>
public static class ReturnsErrorCodes
{
    /// <summary>Return request missing.</summary>
    public const string Missing = "return.missing";

    /// <summary>Generic return rejection.</summary>
    public const string Rejected = "return.rejected";

    /// <summary>Return window expired.</summary>
    public const string Expired = "return.expired";

    /// <summary>Non-returnable.</summary>
    public const string NonReturnable = "return.non_returnable";

    /// <summary>Quantity exceeded.</summary>
    public const string QuantityExceeded = "return.quantity_exceeded";

    /// <summary>Quantity invalid.</summary>
    public const string QuantityInvalid = "return.quantity_invalid";

    /// <summary>Not delivered.</summary>
    public const string NotDelivered = "return.not_delivered";

    /// <summary>Not paid.</summary>
    public const string NotPaid = "return.not_paid";

    /// <summary>Fulfillment missing for return.</summary>
    public const string FulfillmentMissing = "return.fulfillment_missing";

    /// <summary>Stale transition.</summary>
    public const string Stale = "return.stale";

    /// <summary>Already approved.</summary>
    public const string AlreadyApproved = "return.already_approved";

    /// <summary>Already rejected.</summary>
    public const string AlreadyRejected = "return.already_rejected";

    /// <summary>Line missing.</summary>
    public const string LineMissing = "return.line_missing";

    /// <summary>Not owner.</summary>
    public const string NotOwner = "return.not_owner";

    /// <summary>Idempotency required.</summary>
    public const string IdempotencyRequired = "return.idempotency_required";

    /// <summary>Invalid refund destination.</summary>
    public const string RefundDestinationInvalid = "refund.destination.invalid";

    /// <summary>Retry invalid state.</summary>
    public const string RefundRetryInvalidState = "refund.retry.invalid_state";

    /// <summary>Refund already started.</summary>
    public const string RefundAlreadyStarted = "refund.already_started";

    /// <summary>Refund already completed.</summary>
    public const string RefundAlreadyCompleted = "refund.already_completed";

    /// <summary>Payment missing for refund.</summary>
    public const string RefundPaymentMissing = "refund.payment_missing";
}
