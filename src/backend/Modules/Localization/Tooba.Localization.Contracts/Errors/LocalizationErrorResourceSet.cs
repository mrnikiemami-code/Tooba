using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Localization.Contracts.Errors;

/// <summary>Resource manager marker for LocalizationErrors.resx.</summary>
public static class LocalizationErrorResources
{
    /// <summary>ResourceManager for Localization error resources.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Localization.Contracts.Resources.LocalizationErrors", typeof(LocalizationErrorResources).Assembly);
}

/// <summary>Localization-owned error resource set for the <c>localization.</c> key space.</summary>
public sealed class LocalizationErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("localization.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        LocalizationErrorResources.Manager.GetString(localizationKey, culture);
}
