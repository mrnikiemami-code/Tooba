namespace Tooba.Host.Configuration;

/// <summary>
/// Opt-in Development commissioning of the reference data a Marketplace installation needs before any
/// shipping method can be authored: the canonical Localization language set plus ONE bounded
/// Geography seed (Iran / Tehran province / Tehran city) owned by the Geography module.
/// </summary>
/// <remarks>
/// Default disabled. It commissions reference data only — no business entity, price, coverage, owner or
/// activation is created here, and the owning modules keep all persistence authority.
/// </remarks>
internal sealed class MarketplaceReferenceDataOptions
{
    /// <summary>Section name: <c>Tooba:Development:MarketplaceReferenceData</c>.</summary>
    public const string SectionName = "Tooba:Development:MarketplaceReferenceData";

    /// <summary>Explicit opt-in; checked-in default is <c>false</c>.</summary>
    public bool Enabled { get; set; }
}
