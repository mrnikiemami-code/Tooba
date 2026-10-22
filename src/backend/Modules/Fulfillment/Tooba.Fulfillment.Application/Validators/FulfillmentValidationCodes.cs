namespace Tooba.Fulfillment.Application.Validators;

/// <summary>
/// Stable machine codes for Fulfillment FluentValidation transport/input shape.
/// Codes are never localized identity and never encode business semantics.
/// </summary>
public static class FulfillmentValidationCodes
{
    /// <summary>Fulfillment identifier must be provided.</summary>
    public const string FulfillmentIdRequired = "fulfillment.validation.fulfillment_id_required";

    /// <summary>Checkout identifier must be provided.</summary>
    public const string CheckoutIdRequired = "fulfillment.validation.checkout_id_required";

    /// <summary>Shipping service identifier must be provided.</summary>
    public const string ShippingServiceIdRequired = "fulfillment.validation.shipping_service_id_required";

    /// <summary>Seller permission snapshot must be supplied.</summary>
    public const string SellerPermissionRequired = "fulfillment.validation.seller_permission_required";

    /// <summary>Shipment identifier must be non-empty when supplied.</summary>
    public const string ShipmentIdShape = "fulfillment.validation.shipment_id_shape";

    /// <summary>Carrier display name must not be blank when supplied.</summary>
    public const string CarrierDisplayNameShape = "fulfillment.validation.carrier_display_name_shape";

    /// <summary>Tracking reference must not be blank when supplied.</summary>
    public const string TrackingReferenceShape = "fulfillment.validation.tracking_reference_shape";

    /// <summary>Shipping method code must not be blank when supplied.</summary>
    public const string ShippingMethodCodeShape = "fulfillment.validation.shipping_method_code_shape";

    /// <summary>Shipment lines collection must not contain null entries when supplied.</summary>
    public const string ShipmentLinesNoNullItems = "fulfillment.validation.shipment_lines_no_null_items";

    /// <summary>Shipment line order-line identifier must be provided.</summary>
    public const string ShipmentLineOrderLineIdRequired = "fulfillment.validation.shipment_line_order_line_id_required";

    /// <summary>Shipment line quantity must be greater than zero.</summary>
    public const string ShipmentLineQuantityPositive = "fulfillment.validation.shipment_line_quantity_positive";

    // ---- Operational shipping type catalog (TB-196) -------------------------------------
    /// <summary>Shipping type write model envelope is missing.</summary>
    public const string ShippingTypeModelRequired = "fulfillment.validation.shipping_type_model_required";

    /// <summary>Shipping type identifier must be non-empty.</summary>
    public const string ShippingTypeIdRequired = "fulfillment.validation.shipping_type_id_required";

    /// <summary>Shipping type code violates the canonical ASCII shape or length.</summary>
    public const string ShippingTypeCodeShape = "fulfillment.validation.shipping_type_code_shape";

    /// <summary>Shipping type sort order is outside the supported bounds.</summary>
    public const string ShippingTypeSortOrderShape = "fulfillment.validation.shipping_type_sort_order_shape";

    /// <summary>Shipping type expected revision must be greater than zero.</summary>
    public const string ShippingTypeRevisionShape = "fulfillment.validation.shipping_type_revision_shape";

    /// <summary>Shipping type translations collection is missing.</summary>
    public const string ShippingTypeTranslationsRequired = "fulfillment.validation.shipping_type_translations_required";

    /// <summary>A shipping type translation entry is null or has an unusable shape.</summary>
    public const string ShippingTypeTranslationShape = "fulfillment.validation.shipping_type_translation_shape";

    /// <summary>A shipping type translation language id is repeated in one request.</summary>
    public const string ShippingTypeTranslationDuplicateLanguage =
        "fulfillment.validation.shipping_type_translation_duplicate_language";

    /// <summary>Bulk request envelope must be supplied.</summary>
    public const string BulkRequestRequired = "fulfillment.validation.bulk_request_required";

    /// <summary>Admin grid query envelope must be supplied.</summary>
    public const string WorkQueueRequestRequired = "fulfillment.validation.work_queue_request_required";

    /// <summary>Shipping service write model must be supplied.</summary>
    public const string ShippingServiceModelRequired = "fulfillment.validation.shipping_service_model_required";

    /// <summary>One-level shipping method code must not be blank when supplied.</summary>
    public const string ShippingMethodCodeRequired = "fulfillment.validation.shipping_method_code_required";
}
