using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Identity.Contracts.Problems;

/// <summary>Resource manager marker for IdentityErrors.resx.</summary>
public static class IdentityErrorResources
{
    /// <summary>ResourceManager for Identity error resources.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Identity.Contracts.Resources.IdentityErrors", typeof(IdentityErrorResources).Assembly);
}

/// <summary>
/// Identity-owned error resource set. Owns the <c>identity.</c> key space so the canonical
/// localizer resolves Identity boundary titles without ad-hoc culture branching.
/// </summary>
public sealed class IdentityErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("identity.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        IdentityErrorResources.Manager.GetString(localizationKey, culture);
}
