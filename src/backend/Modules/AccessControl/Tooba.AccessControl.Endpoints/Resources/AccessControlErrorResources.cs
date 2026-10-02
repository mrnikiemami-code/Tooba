using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.AccessControl.Endpoints.Resources;

/// <summary>Resource marker for AccessControl errors.</summary>
public static class AccessControlErrorResources
{
    public static ResourceManager Manager { get; } =
        new("Tooba.AccessControl.Endpoints.Resources.AccessControlErrors", typeof(AccessControlErrorResources).Assembly);
}

/// <summary>Owns localization keys under <c>access.</c>.</summary>
public sealed class AccessControlErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("access.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        AccessControlErrorResources.Manager.GetString(localizationKey, culture);
}
