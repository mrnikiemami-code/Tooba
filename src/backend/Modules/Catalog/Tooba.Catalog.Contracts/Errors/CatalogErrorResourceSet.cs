using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Catalog.Contracts.Errors;

/// <summary>Resource marker for CatalogErrors.resx.</summary>
public static class CatalogErrorResources
{
    /// <summary>ResourceManager for CatalogErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Catalog.Contracts.Resources.CatalogErrors", typeof(CatalogErrorResources).Assembly);
}

/// <summary>Catalog error resource set — owns quantity.* / catalog.* keys used by Catalog endpoints.</summary>
public sealed class CatalogErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("quantity.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("unit.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("catalog.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        CatalogErrorResources.Manager.GetString(localizationKey, culture);
}
