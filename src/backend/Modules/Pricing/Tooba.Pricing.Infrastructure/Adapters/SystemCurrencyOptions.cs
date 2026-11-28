namespace Tooba.Pricing.Infrastructure.Adapters;

/// <summary>
/// Trusted commercial configuration of the system currency reference (`Pricing:Currencies`).
/// Currency metadata is commercial configuration, not locale or market.
/// </summary>
public sealed class PricingCurrenciesOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Pricing";

    /// <summary>Configured currencies.</summary>
    public List<PricingCurrencyEntry> Currencies { get; set; } = [];
}

/// <summary>One configured system currency.</summary>
public sealed class PricingCurrencyEntry
{
    /// <summary>Three-letter ISO code (IRR, USD, ...).</summary>
    public string Code { get; set; } = "";

    /// <summary>English display name.</summary>
    public string EnglishName { get; set; } = "";

    /// <summary>Native display name.</summary>
    public string NativeName { get; set; } = "";

    /// <summary>Authored precision for this currency (IRR is 0).</summary>
    public int DecimalPlaces { get; set; } = 2;

    /// <summary>Display ordering.</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether the currency may be selected for new authored prices.</summary>
    public bool Active { get; set; } = true;
}
