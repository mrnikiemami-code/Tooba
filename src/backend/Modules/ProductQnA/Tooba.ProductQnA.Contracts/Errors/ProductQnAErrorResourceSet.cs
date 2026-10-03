using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.ProductQnA.Contracts.Errors;

/// <summary>Resource manager marker for ProductQnAErrors.resx.</summary>
public static class ProductQnAErrorResources
{
    /// <summary>ResourceManager for ProductQnA error resources.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.ProductQnA.Contracts.Resources.ProductQnAErrors", typeof(ProductQnAErrorResources).Assembly);
}

/// <summary>ProductQnA-owned error resource set for the <c>product_qna.</c> key space.</summary>
public sealed class ProductQnAErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("product_qna.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        ProductQnAErrorResources.Manager.GetString(localizationKey, culture);
}
