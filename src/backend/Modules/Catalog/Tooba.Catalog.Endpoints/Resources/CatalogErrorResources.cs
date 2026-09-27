using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Catalog.Endpoints.Resources;

/// <summary>Resource marker for CatalogErrors.resx.</summary>
public static class CatalogErrorResources
{
    public static ResourceManager Manager { get; } =
        new("Tooba.Catalog.Endpoints.Resources.CatalogErrors", typeof(CatalogErrorResources).Assembly);
}

/// <summary>Catalog error resource set — owns quantity.* / catalog.* keys used by Catalog endpoints.</summary>
public sealed class CatalogErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("quantity.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("catalog.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        CatalogErrorResources.Manager.GetString(localizationKey, culture);
}
