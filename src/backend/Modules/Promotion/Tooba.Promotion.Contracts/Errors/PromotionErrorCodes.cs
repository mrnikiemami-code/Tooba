namespace Tooba.Promotion.Contracts.Errors;

/// <summary>
/// Stable Promotion semantic error codes for HTTP/use-case outcomes and Domain invariants. Identity is
/// the code itself — never a message string and never localized prose. Values are the machine codes
/// emitted by the Promotion Domain/Infrastructure and mapped by the composed error catalog; they must
/// never be renamed or repurposed.
/// <para>
/// This is the single canonical home for Promotion stable-code identity (mirrors
/// <c>PricingErrorCodes</c>/<c>InventoryErrorCodes</c>/<c>PaymentErrorCodes</c>). Ownership is unique:
/// the <c>promotion.*</c> / <c>merchandising.*</c> / <c>campaign.*</c> keyspace is Promotion-owned and
/// localized by <c>PromotionErrorResourceSet</c>. Promotion never re-registers a foreign-owned
/// descriptor — the cross-cutting <c>seller.authorization.denied</c> / <c>admin.authorization.denied</c>
/// codes belong to Foundation and are deliberately not declared here — and no other module registers a
/// Promotion code.
/// </para>
/// <para>
/// The surface is split by reachability so the AMSC gates stay honest:
/// <list type="bullet">
/// <item><see cref="IsHttpReachable"/> — codes an HTTP client can observe; each has exactly one
/// descriptor registered by <c>PromotionErrorCatalogContributor</c>.</item>
/// <item><see cref="IsDomainInvariant"/> — codes raised by Domain aggregates and Infrastructure
/// directories as typed faults. They are the <c>Result</c>-carried identity of a rejected invariant and
/// are localized, but they are never catalogued with their own HTTP descriptor: the use case maps them
/// onto the stable public outcome code for that operation, so no client-visible code is added.</item>
/// </list>
/// <see cref="IsKnown"/> is the union and is what <c>PromotionOperation</c> filters on, so a Promotion
/// fault maps to <c>Result</c> while a code owned by another module (or an unexpected fault) propagates
/// untouched to the canonical global exception boundary. Classification is by typed code only — never by
/// message text.
/// </para>
/// </summary>
public static class PromotionErrorCodes
{
    private static readonly HashSet<string> HttpReachableCodes = new(StringComparer.Ordinal)
    {
        // Promotion definition use-case outcomes.
        Missing,
        NameRequired,
        CouponRequired,
        MutationRejected,
        ActivateRejected,
        DeactivateRejected,

        // Merchandising campaign Admin outcomes.
        MerchandisingCampaignMissing,
        CampaignValidation,
        CampaignPublish,
        CampaignMember,
        CampaignReorder,
        CampaignPrice,

        // Platform-side outbox translation fault.
        OutboxUnmappedEventType,
    };

    private static readonly HashSet<string> DomainInvariantCodes = new(StringComparer.Ordinal)
    {
        // Promotion definition aggregate invariants.
        DefinitionIdRequired,
        DefinitionNameRequired,
        DefinitionWindowInvalid,
        DefinitionPercentInvalid,
        DefinitionPercentNoFixed,
        DefinitionFixedAmountInvalid,
        DefinitionFixedCurrencyRequired,
        DefinitionFixedNoPercent,
        DefinitionMinQtyInvalid,
        DefinitionMinSubtotalInvalid,
        DefinitionActiveImmutable,

        // Merchandising campaign aggregate invariants.
        CampaignIdRequired,
        CampaignTypeRequired,
        CampaignStoreRequired,
        CampaignStoreMismatch,
        CampaignArchivedCannotPublish,
        CampaignWindowInvalid,
        CampaignOfferIdRequired,
        CampaignOfferRequired,

        // Merchandising promotion type invariants.
        TypeIdRequired,
        TypeCodeRequired,
        TypeNotFound,
        TypeSystemCodeImmutable,
        TypeSystemDeleteForbidden,

        // Merchandising translation invariants.
        TranslationLocaleRequired,
        TranslationTitleRequired,
        TranslationNameRequired,
    };

    /// <summary>The exact set of Promotion codes an HTTP client can observe, each singly catalogued.</summary>
    public static IReadOnlyCollection<string> HttpReachable => HttpReachableCodes;

    /// <summary>The exact set of Promotion Domain/Infrastructure invariant codes.</summary>
    public static IReadOnlyCollection<string> DomainInvariants => DomainInvariantCodes;

    /// <summary>
    /// True when <paramref name="code"/> is a stable code this module's contract boundary is allowed to
    /// surface as a use-case fault. Used by <c>PromotionOperation</c> so Promotion faults map to
    /// <c>Result</c> while codes owned by another module (or an unexpected fault) propagate untouched to
    /// the canonical global exception boundary. Classification is by typed code only — never by message text.
    /// </summary>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code)
        && (HttpReachableCodes.Contains(code) || DomainInvariantCodes.Contains(code));

    /// <summary>True when <paramref name="code"/> is a Promotion code registered as an HTTP descriptor.</summary>
    public static bool IsHttpReachable(string? code) =>
        !string.IsNullOrWhiteSpace(code) && HttpReachableCodes.Contains(code);

    /// <summary>True when <paramref name="code"/> is a Promotion Domain/Infrastructure invariant code.</summary>
    public static bool IsDomainInvariant(string? code) =>
        !string.IsNullOrWhiteSpace(code) && DomainInvariantCodes.Contains(code);

    /// <summary>Promotion was not found (or is not owned by the caller).</summary>
    public const string Missing = "promotion.missing";

    /// <summary>The promotion name is required.</summary>
    public const string NameRequired = "promotion.name.required";

    /// <summary>The promotion coupon code is required.</summary>
    public const string CouponRequired = "promotion.coupon.required";

    /// <summary>The promotion mutation was rejected by the aggregate invariant.</summary>
    public const string MutationRejected = "promotion.mutation.rejected";

    /// <summary>The promotion could not be activated.</summary>
    public const string ActivateRejected = "promotion.activate.rejected";

    /// <summary>The promotion could not be deactivated.</summary>
    public const string DeactivateRejected = "promotion.deactivate.rejected";

    /// <summary>The merchandising campaign was not found in the resolved store.</summary>
    public const string MerchandisingCampaignMissing = "merchandising.campaign.missing";

    /// <summary>The merchandising campaign payload failed a business-shape rule.</summary>
    public const string CampaignValidation = "campaign.validation";

    /// <summary>The merchandising campaign cannot be published in its current state.</summary>
    public const string CampaignPublish = "campaign.publish";

    /// <summary>The merchandising campaign member operation was rejected.</summary>
    public const string CampaignMember = "campaign.member";

    /// <summary>The merchandising campaign member reorder was rejected.</summary>
    public const string CampaignReorder = "campaign.reorder";

    /// <summary>The merchandising campaign member price was rejected.</summary>
    public const string CampaignPrice = "campaign.price";

    /// <summary>The outbox registration has no mapping for the given integration event type.</summary>
    public const string OutboxUnmappedEventType = "promotion.outbox.unmapped_event_type";

    /// <summary>The promotion definition identifier is required.</summary>
    public const string DefinitionIdRequired = "promotion.definition.id_required";

    /// <summary>The promotion definition name is required.</summary>
    public const string DefinitionNameRequired = "promotion.definition.name_required";

    /// <summary>The promotion definition effective window is invalid.</summary>
    public const string DefinitionWindowInvalid = "promotion.definition.window_invalid";

    /// <summary>The percentage rate is out of the (0, 1] range.</summary>
    public const string DefinitionPercentInvalid = "promotion.definition.percent_invalid";

    /// <summary>A percentage promotion must not also carry a fixed amount.</summary>
    public const string DefinitionPercentNoFixed = "promotion.definition.percent_no_fixed";

    /// <summary>The fixed discount amount must be greater than zero.</summary>
    public const string DefinitionFixedAmountInvalid = "promotion.definition.fixed_amount_invalid";

    /// <summary>A fixed-amount promotion requires a currency.</summary>
    public const string DefinitionFixedCurrencyRequired = "promotion.definition.fixed_currency_required";

    /// <summary>A fixed-amount promotion must not also carry a percentage rate.</summary>
    public const string DefinitionFixedNoPercent = "promotion.definition.fixed_no_percent";

    /// <summary>The minimum quantity must be greater than zero when supplied.</summary>
    public const string DefinitionMinQtyInvalid = "promotion.definition.min_qty_invalid";

    /// <summary>The minimum subtotal must not be negative.</summary>
    public const string DefinitionMinSubtotalInvalid = "promotion.definition.min_subtotal_invalid";

    /// <summary>An active promotion cannot be edited.</summary>
    public const string DefinitionActiveImmutable = "promotion.definition.active_immutable";

    /// <summary>The merchandising campaign identifier is required.</summary>
    public const string CampaignIdRequired = "promotion.campaign.id_required";

    /// <summary>The merchandising campaign type is required.</summary>
    public const string CampaignTypeRequired = "promotion.campaign.type_required";

    /// <summary>The merchandising campaign store is required.</summary>
    public const string CampaignStoreRequired = "promotion.campaign.store_required";

    /// <summary>The campaign does not belong to the store the caller resolved.</summary>
    public const string CampaignStoreMismatch = "promotion.campaign.store_mismatch";

    /// <summary>An archived campaign cannot be published.</summary>
    public const string CampaignArchivedCannotPublish = "promotion.campaign.archived_cannot_publish";

    /// <summary>The merchandising campaign window is invalid.</summary>
    public const string CampaignWindowInvalid = "promotion.campaign.window_invalid";

    /// <summary>The merchandising campaign offer identifier is required.</summary>
    public const string CampaignOfferIdRequired = "promotion.campaign_offer.id_required";

    /// <summary>The merchandising campaign offer is required.</summary>
    public const string CampaignOfferRequired = "promotion.campaign_offer.offer_required";

    /// <summary>The merchandising promotion type identifier is required.</summary>
    public const string TypeIdRequired = "promotion.type.id_required";

    /// <summary>The merchandising promotion type code is required.</summary>
    public const string TypeCodeRequired = "promotion.type.code_required";

    /// <summary>The referenced merchandising promotion type does not exist.</summary>
    public const string TypeNotFound = "promotion.type.not_found";

    /// <summary>A system promotion type code is immutable.</summary>
    public const string TypeSystemCodeImmutable = "promotion.type.system_code_immutable";

    /// <summary>A system promotion type cannot be deleted.</summary>
    public const string TypeSystemDeleteForbidden = "promotion.type.system_delete_forbidden";

    /// <summary>The translation locale is required.</summary>
    public const string TranslationLocaleRequired = "promotion.translation.locale_required";

    /// <summary>The campaign translation title is required.</summary>
    public const string TranslationTitleRequired = "promotion.translation.title_required";

    /// <summary>The promotion type translation display name is required.</summary>
    public const string TranslationNameRequired = "promotion.translation.name_required";
}
