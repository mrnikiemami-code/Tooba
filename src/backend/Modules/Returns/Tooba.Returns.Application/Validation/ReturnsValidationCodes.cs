namespace Tooba.Returns.Application.Validation;

/// <summary>
/// Stable machine codes for Returns transport-shape FluentValidation rules. These are NOT localized
/// identity and are deliberately never registered as catalogued HTTP descriptors: they travel inside the
/// canonical <c>validation.failed</c> envelope's per-property <c>validationErrors</c> map. Only transport
/// shape lives here — every business/domain rule (ownership, eligibility window, remaining quantity,
/// payment/refund state, lifecycle) stays in the Returns Domain/Application and is never duplicated into
/// a validator.
/// </summary>
public static class ReturnsValidationCodes
{
    /// <summary>A seller order identifier must not be the empty GUID.</summary>
    public const string SellerOrderIdRequired = "returns.validation.seller_order_id_required";

    /// <summary>The idempotency key must be present.</summary>
    public const string IdempotencyKeyRequired = "returns.validation.idempotency_key_required";

    /// <summary>The idempotency key must not exceed the persisted length.</summary>
    public const string IdempotencyKeyTooLong = "returns.validation.idempotency_key_too_long";

    /// <summary>The return line collection must be present.</summary>
    public const string ItemsRequired = "returns.validation.items_required";

    /// <summary>The return line collection must contain at least one line.</summary>
    public const string ItemsEmpty = "returns.validation.items_empty";

    /// <summary>Every return line order-line identifier must not be the empty GUID.</summary>
    public const string OrderLineIdRequired = "returns.validation.order_line_id_required";

    /// <summary>Every return line quantity must be greater than zero.</summary>
    public const string QuantityInvalid = "returns.validation.quantity_invalid";

    /// <summary>A route/command return identifier must not be the empty GUID.</summary>
    public const string ReturnRequestIdRequired = "returns.validation.return_request_id_required";

    /// <summary>The reason text must not exceed the persisted length.</summary>
    public const string ReasonTooLong = "returns.validation.reason_too_long";

    /// <summary>The refund destination token must be a known destination when supplied.</summary>
    public const string RefundDestinationInvalid = "returns.validation.refund_destination_invalid";

    /// <summary>An ordinary creation named the same order line more than once (list shape, not a quantity rule).</summary>
    public const string DuplicateOrderLine = "returns.validation.duplicate_order_line";

    /// <summary>
    /// A return line quantity is not exactly representable at the persisted six-decimal scale, so accepting it would
    /// store a different number of physical units than the caller submitted.
    /// </summary>
    public const string QuantityPrecision = "returns.validation.quantity_precision";

    /// <summary>The admin grid query body must be present.</summary>
    public const string GridRequestRequired = "returns.validation.grid_request_required";

    /// <summary>TB-273: the explicit recovery kind must be one of the defined intents.</summary>
    public const string RecoveryKindInvalid = "returns.validation.recovery_kind_invalid";

    /// <summary>TB-273: a money-related recovery must name the exact stored refund attempt.</summary>
    public const string RecoveryAttemptRequired = "returns.validation.recovery_attempt_required";

    /// <summary>TB-274: a customer receipt must name its occurrence identity.</summary>
    public const string ReceiptOccurrenceRequired = "returns.validation.receipt_occurrence_required";

    /// <summary>TB-274: the receipt occurrence identity must not exceed the persisted length.</summary>
    public const string ReceiptOccurrenceTooLong = "returns.validation.receipt_occurrence_too_long";

    /// <summary>TB-274: the receipt line collection must contain at least one inspected line.</summary>
    public const string ReceiptLinesEmpty = "returns.validation.receipt_lines_empty";

    /// <summary>TB-274: every receipt line must name a real return item.</summary>
    public const string ReceiptItemIdRequired = "returns.validation.receipt_item_id_required";

    /// <summary>TB-274: every receipt quantity must be positive and exactly representable at the persisted scale.</summary>
    public const string ReceiptQuantityInvalid = "returns.validation.receipt_quantity_invalid";

    /// <summary>TB-274: the inspected condition must be a defined condition.</summary>
    public const string ReceiptConditionInvalid = "returns.validation.receipt_condition_invalid";

    /// <summary>TB-274: one receipt occurrence cannot name the same (item, condition) twice.</summary>
    public const string ReceiptDuplicateLine = "returns.validation.receipt_duplicate_line";

    /// <summary>TB-279: an audited correction must name the reviewing operator.</summary>
    public const string CorrectionActorRequired = "returns.validation.correction_actor_required";

    /// <summary>TB-279: an audited correction must state its reviewed occurrence identity.</summary>
    public const string CorrectionOccurrenceRequired = "returns.validation.correction_occurrence_required";

    /// <summary>TB-279: an audited correction must state its reviewed reason.</summary>
    public const string CorrectionReasonRequired = "returns.validation.correction_reason_required";

    /// <summary>TB-279: a reviewed resolution must name the reviewing operator.</summary>
    public const string ResolutionActorRequired = "returns.validation.resolution_actor_required";

    /// <summary>TB-279: a reviewed resolution must carry at least one line.</summary>
    public const string ResolutionLinesEmpty = "returns.validation.resolution_lines_empty";

    /// <summary>TB-279: a resolution line names the same item twice.</summary>
    public const string ResolutionDuplicateLine = "returns.validation.resolution_duplicate_line";

    /// <summary>TB-279: a resolution line names a non-positive or over-precise split.</summary>
    public const string ResolutionQuantityInvalid = "returns.validation.resolution_quantity_invalid";

    /// <summary>TB-279: the reviewed resolution version must be present and non-negative.</summary>
    public const string ResolutionVersionInvalid = "returns.validation.resolution_version_invalid";

    /// <summary>TB-279: a custody fact must name its reviewed occurrence identity.</summary>
    public const string CustodyOccurrenceRequired = "returns.validation.custody_occurrence_required";

    /// <summary>TB-279: a custody fact must name the receiving customer.</summary>
    public const string CustodyRecipientRequired = "returns.validation.custody_recipient_required";

    /// <summary>TB-279: a custody fact must carry its handback proof reference.</summary>
    public const string CustodyProofRequired = "returns.validation.custody_proof_required";

    /// <summary>TB-279: a custody quantity is non-positive or over-precise.</summary>
    public const string CustodyQuantityInvalid = "returns.validation.custody_quantity_invalid";

    /// <summary>TB-279: a custody inspection is not a defined condition.</summary>
    public const string CustodyConditionInvalid = "returns.validation.custody_condition_invalid";
}
