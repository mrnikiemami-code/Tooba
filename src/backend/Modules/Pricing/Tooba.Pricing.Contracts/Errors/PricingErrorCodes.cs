namespace Tooba.Pricing.Contracts.Errors;

/// <summary>
/// Stable Pricing semantic error codes for HTTP/use-case outcomes. Identity is the code itself —
/// never a message string and never localized prose. Values are the machine codes emitted by the
/// Pricing Domain/Infrastructure and mapped by the composed error catalog; they must never be
/// renamed or repurposed.
/// <para>
/// This is the single canonical home for Pricing stable-code identity. Ownership is unique: every
/// code below is Pricing-owned, registered exactly once by <c>PricingErrorCatalogContributor</c>
/// and localized by <c>PricingErrorResourceSet</c>. Pricing never re-registers a foreign-owned
/// descriptor and no other module registers a Pricing code.
/// </para>
/// </summary>
public static class PricingErrorCodes
{
    private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        AmountInvalid,
        Overlap,
        OfferMissing,
        CampaignRequired,
        ValidityInverted,
        RetiredReactivate,
        RetiredImmutable,
        CurrencyChangeForbidden,
        MarketInvalid,
        CurrencyInvalid,
        CurrencyDisplayUnit,
    };

    /// <summary>
    /// True when <paramref name="code"/> is a stable code this module's contract boundary is allowed to
    /// surface as a use-case fault. Used by <c>PricingOperation</c> so Pricing faults map to
    /// <c>Result</c> while codes owned by another module (or an unexpected fault) propagate untouched to
    /// the canonical global exception boundary. Classification is by typed code only — never by message text.
    /// </summary>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code) && KnownCodes.Contains(code);

    /// <summary>The authored amount is invalid.</summary>
    public const string AmountInvalid = "pricing.amount.invalid";

    /// <summary>More than one active price overlaps the selection key.</summary>
    public const string Overlap = "pricing.overlap";

    /// <summary>The offer was not found through the Offer lookup contract.</summary>
    public const string OfferMissing = "pricing.offer.missing";

    /// <summary>A campaign price requires a campaign id.</summary>
    public const string CampaignRequired = "pricing.campaign.required";

    /// <summary>The validity window ends at or before it starts.</summary>
    public const string ValidityInverted = "pricing.validity.inverted";

    /// <summary>A retired price cannot be activated again.</summary>
    public const string RetiredReactivate = "pricing.retired.reactivate";

    /// <summary>A retired price cannot be edited.</summary>
    public const string RetiredImmutable = "pricing.retired.immutable";

    /// <summary>Changing currency requires a new authored price.</summary>
    public const string CurrencyChangeForbidden = "pricing.currency.change_forbidden";

    /// <summary>The market code is empty or not a stable token.</summary>
    public const string MarketInvalid = "pricing.market.invalid";

    /// <summary>The currency code is empty or not a three-letter ISO code.</summary>
    public const string CurrencyInvalid = "pricing.currency.invalid";

    /// <summary>A display unit such as toman was sent instead of the stored currency.</summary>
    public const string CurrencyDisplayUnit = "pricing.currency.display_unit";
}
