namespace Tooba.Host.Configuration;

/// <summary>
/// TB240-R2: opt-in Development commissioning that binds the TWO real Marketplace seller identities to their
/// own seller-owner scope, which the existing access-control bootstrap creates but never assigns.
/// </summary>
/// <remarks>
/// Default disabled and only honoured in a real Development Marketplace (Tenant-NULL) deployment. It creates
/// no role catalog, grants no Platform permission to a seller, and never fabricates a business tenant.
/// </remarks>
internal sealed class MarketplaceSellerAccessOptions
{
    /// <summary>Section name: <c>Tooba:Development:MarketplaceSellerAccess</c>.</summary>
    public const string SectionName = "Tooba:Development:MarketplaceSellerAccess";

    /// <summary>Explicit opt-in; checked-in default is <c>false</c>.</summary>
    public bool Enabled { get; set; }
}
