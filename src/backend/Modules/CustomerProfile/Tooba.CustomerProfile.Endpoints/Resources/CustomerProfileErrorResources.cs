using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.CustomerProfile.Endpoints.Resources;

/// <summary>Marker for the CustomerProfile error ResourceManager.</summary>
public static class CustomerProfileErrorResources
{
    /// <summary>ResourceManager for CustomerProfileErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.CustomerProfile.Endpoints.Resources.CustomerProfileErrors", typeof(CustomerProfileErrorResources).Assembly);
}

/// <summary>
/// CustomerProfile error resource set — ownership is limited to the descriptive-profile fault space
/// (<c>customer.profile.</c>) so it never collides with another module's error set. The shared
/// <c>customer.session.</c> code stays with the Foundation resource set.
/// </summary>
public sealed class CustomerProfileErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("customer.profile.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        CustomerProfileErrorResources.Manager.GetString(localizationKey, culture);
}
