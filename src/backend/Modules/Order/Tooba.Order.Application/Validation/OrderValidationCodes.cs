namespace Tooba.Order.Application.Validation;

/// <summary>Stable machine codes for Order FluentValidation (not localized identity).</summary>
public static class OrderValidationCodes
{
    public const string CheckoutIdRequired = "order.validation.checkout_id_required";
    public const string CartIdRequired = "order.validation.cart_id_required";
    public const string ActorUserIdRequired = "order.validation.actor_user_id_required";
    public const string SellerPartyIdRequired = "order.validation.seller_party_id_required";
    public const string NoteIdRequired = "order.validation.note_id_required";
    public const string NoteBodyRequired = "order.validation.note_body_required";
    public const string NoteBodyTooLong = "order.validation.note_body_too_long";
    public const string IdempotencyKeyRequired = "order.validation.idempotency_key_required";
    public const string IdempotencyKeyTooLong = "order.validation.idempotency_key_too_long";
    public const string PageMin = "order.validation.page_min";
    public const string PageSizeRange = "order.validation.page_size_range";
    public const string GridRequestRequired = "order.validation.grid_request_required";
    public const string OperationCodeRequired = "order.validation.operation_code_required";
    public const string OperationRequestRequired = "order.validation.operation_request_required";
    public const string ExpectedCartVersionMin = "order.validation.expected_cart_version_min";
    public const string SellerOrderIdRequired = "order.validation.seller_order_id_required";
    public const string ReasonRequired = "order.validation.reason_required";
    public const string ReasonTooLong = "order.validation.reason_too_long";
    public const string TakeRange = "order.validation.take_range";
    public const string ShippingBodyRequired = "order.validation.shipping_body_required";
}
