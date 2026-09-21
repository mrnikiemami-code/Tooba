namespace Tooba.Pricing.Contracts;

/// <summary>Stable semantic error codes owned by Pricing.</summary>
public static class PricingErrorCodes
{
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
