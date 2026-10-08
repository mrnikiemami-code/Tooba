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

    /// <summary>The admin grid query body must be present.</summary>
    public const string GridRequestRequired = "returns.validation.grid_request_required";
}
