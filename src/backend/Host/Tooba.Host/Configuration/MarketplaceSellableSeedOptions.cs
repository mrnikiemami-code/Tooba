namespace Tooba.Host.Configuration;

/// <summary>
/// Opt-in commissioning of the EXISTING Catalog-owned Development sellable fixture
/// (live workspace product + two real demo sellers with offers, prices, tax, locations and stock)
/// on a Development Marketplace target. Default disabled; it seeds nothing by itself and owns no
/// business data — it only asks the owning module to run its own documented seam.
/// </summary>
internal sealed class MarketplaceSellableSeedOptions
{
    /// <summary>
    /// Section name: <c>Tooba:Development:MarketplaceSellableSeed</c>.
    /// </summary>
    public const string SectionName = "Tooba:Development:MarketplaceSellableSeed";

    /// <summary>
    /// Explicit opt-in. Checked-in default is <c>false</c>; only local Development configuration may enable it.
    /// </summary>
    public bool Enabled { get; set; }
}
