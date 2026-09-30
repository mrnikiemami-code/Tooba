using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.PageComposition.Endpoints.Resources;

/// <summary>نشانگر منبع خطاهای PageComposition.</summary>
public static class PageCompositionErrorResources
{
    public static ResourceManager Manager { get; } =
        new("Tooba.PageComposition.Endpoints.Resources.PageCompositionErrors", typeof(PageCompositionErrorResources).Assembly);
}

/// <summary>مجموعهٔ منبع PageComposition — مالکیت <c>page-composition.</c>.</summary>
public sealed class PageCompositionErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("page-composition.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        PageCompositionErrorResources.Manager.GetString(localizationKey, culture);
}
