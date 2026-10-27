namespace Tooba.Offer.Contracts.Errors;

/// <summary>Stable semantic error codes owned by Offer.</summary>
public static class OfferErrorCodes
{
    /// <summary>The minimum quantity is invalid.</summary>
    public const string MinQuantityInvalid = "offer.min_quantity.invalid";

    /// <summary>The maximum quantity is invalid.</summary>
    public const string MaxQuantityInvalid = "offer.max_quantity.invalid";

    /// <summary>The minimum exceeds the maximum.</summary>
    public const string MinQuantityExceedsMax = "offer.min_quantity.exceeds_max";

    /// <summary>An archived offer cannot be activated.</summary>
    public const string ArchivedCannotActivate = "offer.archived.cannot_activate";

    /// <summary>The Catalog variant was not found.</summary>
    public const string CatalogVariantMissing = "offer.catalog_variant.missing";

    /// <summary>The seller was not found.</summary>
    public const string SellerMissing = "offer.seller.missing";

    /// <summary>The seller is not an organization.</summary>
    public const string SellerNotOrganization = "offer.seller.not_organization";

    /// <summary>An active listing already exists.</summary>
    public const string DuplicateActiveListing = "offer.listing.duplicate_active";

    /// <summary>The seller SKU already exists.</summary>
    public const string DuplicateSellerSku = "offer.seller_sku.duplicate";

    /// <summary>The return policy cannot be overridden.</summary>
    public const string ReturnPolicyOverrideDenied = "offer.return_policy.override_denied";

    /// <summary>Non-returnable offers are not allowed.</summary>
    public const string NonReturnableDenied = "offer.return_policy.non_returnable_denied";

    /// <summary>A custom return window is required.</summary>
    public const string CustomReturnWindowRequired = "offer.return_policy.custom_window_required";

    /// <summary>The custom return window is out of range.</summary>
    public const string CustomReturnWindowOutOfRange = "offer.return_policy.custom_window_out_of_range";

    /// <summary>The seller-scoped offer was not found.</summary>
    public const string NotFound = "offer.not_found";

    /// <summary>The requested status is unsupported.</summary>
    public const string StatusUnsupported = "offer.status.unsupported";

    /// <summary>An archived offer cannot take a fulfillment assignment.</summary>
    public const string ArchivedCannotAssignFulfillment = "offer.archived.cannot_assign_fulfillment";

    /// <summary>The requested fulfillment responsibility is not one of the two supported families.</summary>
    public const string FulfillmentResponsibilityUnsupported = "offer.fulfillment.responsibility_unsupported";

    /// <summary>A fulfillment shipping type id cannot be stored without an explicit responsibility.</summary>
    public const string FulfillmentTypeWithoutResponsibility = "offer.fulfillment.type_without_responsibility";

    /// <summary>The fulfillment shipping type identity is structurally invalid.</summary>
    public const string FulfillmentTypeInvalid = "offer.fulfillment.type_invalid";

    /// <summary>The referenced Fulfillment shipping type does not exist in the real active taxonomy.</summary>
    public const string FulfillmentTypeUnavailable = "offer.fulfillment.type_unavailable";

    /// <summary>The fulfillment assignment body is empty, malformed or has an unsupported shape.</summary>
    public const string FulfillmentBodyMalformed = "offer.fulfillment.body_malformed";
}
