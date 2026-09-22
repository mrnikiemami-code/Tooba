using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;
using Tooba.Cart.Application.Errors;

namespace Tooba.Cart.Endpoints.Resources;

/// <summary>Cart error resource marker.</summary>
public static class CartErrorResources
{
    /// <summary>ResourceManager for CartErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Cart.Endpoints.Resources.CartErrors", typeof(CartErrorResources).Assembly);
}

/// <summary>Cart error resource set for ApiResponseFactory localization.</summary>
public sealed class CartErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("cart.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.Equals(CartErrorCodes.AuthenticationRequired, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        CartErrorResources.Manager.GetString(localizationKey, culture);
}
