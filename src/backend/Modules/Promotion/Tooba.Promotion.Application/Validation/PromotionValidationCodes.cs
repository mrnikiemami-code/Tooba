namespace Tooba.Promotion.Application.Validation;

/// <summary>
/// Stable machine codes for Promotion transport-shape FluentValidation rules. These are NOT localized
/// identity and are deliberately never registered as catalogued HTTP descriptors: they travel inside the
/// canonical <c>validation.failed</c> envelope's per-property <c>validationErrors</c> map. Only transport
/// shape lives here — every business/domain rule (name/coupon required, window validity, percentage vs
/// fixed exclusivity, currency rules, ownership, membership, campaign lifecycle, campaign state) stays in
/// the Promotion Domain/Application and is never duplicated into a validator.
/// </summary>
public static class PromotionValidationCodes
{
    /// <summary>A route/command promotion identifier must not be the empty GUID.</summary>
    public const string PromotionIdRequired = "promotion.validation.promotion_id_required";

    /// <summary>The acting seller party identifier must not be the empty GUID.</summary>
    public const string SellerPartyIdRequired = "promotion.validation.seller_party_id_required";

    /// <summary>The promotion mutation body must be present.</summary>
    public const string MutationInputRequired = "promotion.validation.mutation_input_required";

    /// <summary>The merchandising campaign identifier must not be the empty GUID.</summary>
    public const string CampaignIdRequired = "promotion.validation.campaign_id_required";

    /// <summary>The merchandising campaign write body must be present.</summary>
    public const string CampaignBodyRequired = "promotion.validation.campaign_body_required";

    /// <summary>The merchandising campaign update body must be present.</summary>
    public const string CampaignUpdateBodyRequired = "promotion.validation.campaign_update_body_required";

    /// <summary>The seller offer identifier must not be the empty GUID.</summary>
    public const string SellerOfferIdRequired = "promotion.validation.seller_offer_id_required";

    /// <summary>The ordered member collection must be present.</summary>
    public const string MemberOrderRequired = "promotion.validation.member_order_required";

    /// <summary>The ordered member collection must contain at least one identifier.</summary>
    public const string MemberOrderEmpty = "promotion.validation.member_order_empty";

    /// <summary>The ordered member collection must not repeat an identifier.</summary>
    public const string MemberOrderDuplicate = "promotion.validation.member_order_duplicate";

    /// <summary>Every ordered member identifier must not be the empty GUID.</summary>
    public const string MemberOrderEntryRequired = "promotion.validation.member_order_entry_required";

    /// <summary>The member price body must be present.</summary>
    public const string MemberPriceBodyRequired = "promotion.validation.member_price_body_required";

    /// <summary>The paging offset must not be negative.</summary>
    public const string PageOffsetInvalid = "promotion.validation.page_offset_invalid";

    /// <summary>The page size must not be negative.</summary>
    public const string PageSizeInvalid = "promotion.validation.page_size_invalid";

    /// <summary>The requested locale must be a well-formed BCP-47 style tag.</summary>
    public const string LocaleInvalid = "promotion.validation.locale_invalid";

    /// <summary>The campaign lifecycle filter must be one of the known lifecycle names.</summary>
    public const string LifecycleInvalid = "promotion.validation.lifecycle_invalid";

    /// <summary>The runtime-window filter must be one of the known runtime window names.</summary>
    public const string RuntimeWindowInvalid = "promotion.validation.runtime_window_invalid";
}
