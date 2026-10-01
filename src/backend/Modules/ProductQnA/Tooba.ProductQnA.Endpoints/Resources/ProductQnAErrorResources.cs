using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.ProductQnA.Endpoints.Resources;

/// <summary>نشانگر منبع خطاهای ProductQnA.</summary>
public static class ProductQnAErrorResources
{
    /// <summary>ResourceManager.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.ProductQnA.Endpoints.Resources.ProductQnAErrors", typeof(ProductQnAErrorResources).Assembly);
}

/// <summary>مجموعهٔ منبع ProductQnA.</summary>
public sealed class ProductQnAErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("product_qna.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        ProductQnAErrorResources.Manager.GetString(localizationKey, culture);
}
