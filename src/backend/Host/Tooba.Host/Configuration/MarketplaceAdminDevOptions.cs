namespace Tooba.Host.Configuration;

/// <summary>
/// Opt-in provisioning of ONE already normally registered Development administrator into the
/// existing Marketplace platform tenant membership. Default disabled; it never creates an Identity
/// principal, never resets credentials and never supplies a password.
/// </summary>
internal sealed class MarketplaceAdminDevOptions
{
    /// <summary>
    /// Section name: <c>Tooba:Development:MarketplaceAdmin</c>.
    /// </summary>
    public const string SectionName = "Tooba:Development:MarketplaceAdmin";

    /// <summary>
    /// Explicit opt-in. Checked-in default is <c>false</c>; only local Development configuration may enable it.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Email identifier of an EXISTING normally registered Identity account. Blank/unknown refuses provisioning.
    /// </summary>
    public string Identifier { get; set; } = "";
}
