using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.PageComposition.Contracts.Errors;

/// <summary>Resource manager marker for PageCompositionErrors.resx.</summary>
public static class PageCompositionErrorResources
{
    /// <summary>ResourceManager for PageComposition error resources.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.PageComposition.Contracts.Resources.PageCompositionErrors", typeof(PageCompositionErrorResources).Assembly);
}

/// <summary>PageComposition-owned error resource set for the <c>page-composition.</c> key space.</summary>
public sealed class PageCompositionErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("page-composition.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        PageCompositionErrorResources.Manager.GetString(localizationKey, culture);
}
