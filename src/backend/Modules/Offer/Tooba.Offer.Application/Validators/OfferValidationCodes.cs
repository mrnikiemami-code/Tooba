namespace Tooba.Offer.Application.Validators;

/// <summary>Stable machine codes for Offer FluentValidation transport input (never localized identity).</summary>
public static class OfferValidationCodes
{
    /// <summary>Offer identifier must be provided.</summary>
    public const string OfferIdRequired = "offer.validation.offer_id_required";

    /// <summary>Seller party identifier must be provided.</summary>
    public const string SellerPartyIdRequired = "offer.validation.seller_party_id_required";

    /// <summary>Catalog variant identifier must be provided.</summary>
    public const string CatalogVariantIdRequired = "offer.validation.catalog_variant_id_required";

    /// <summary>Sales channel must be a defined contract value.</summary>
    public const string ChannelShape = "offer.validation.channel_shape";

    /// <summary>Seller SKU must not be blank when supplied.</summary>
    public const string SellerSkuShape = "offer.validation.seller_sku_shape";

    /// <summary>Status must be one of the supported create values when supplied.</summary>
    public const string CreateStatusShape = "offer.validation.status_shape";

    /// <summary>Status must be one of the supported patch values when supplied.</summary>
    public const string UpdateStatusShape = "offer.validation.patch_status_shape";

    /// <summary>Return policy choice must be one of the supported contract choices when supplied.</summary>
    public const string ReturnPolicyChoiceShape = "offer.validation.return_policy_choice_shape";

    /// <summary>Custom return window must be at least one day when supplied.</summary>
    public const string CustomReturnWindowMin = "offer.validation.custom_return_window_min";

    /// <summary>Minimum order quantity must be positive when supplied.</summary>
    public const string MinimumOrderQuantityMin = "offer.validation.minimum_order_quantity_min";

    /// <summary>Maximum order quantity must be positive when supplied.</summary>
    public const string MaximumOrderQuantityMin = "offer.validation.maximum_order_quantity_min";

    /// <summary>Minimum order quantity must not exceed the maximum.</summary>
    public const string OrderQuantityRange = "offer.validation.order_quantity_range";

    /// <summary>Amount must not be negative.</summary>
    public const string AmountMin = "offer.validation.amount_min";

    /// <summary>Currency must be shaped as a 3-character code when supplied.</summary>
    public const string CurrencyShape = "offer.validation.currency_shape";

    /// <summary>Market must not be blank when supplied.</summary>
    public const string MarketShape = "offer.validation.market_shape";

    /// <summary>On-hand quantity must not be negative.</summary>
    public const string OnHandMin = "offer.validation.on_hand_min";

    /// <summary>Adjustment reason must not be blank when supplied.</summary>
    public const string ReasonShape = "offer.validation.reason_shape";
}
