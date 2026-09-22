using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;
using Tooba.Order.Application.Storefront;

namespace Tooba.Order.Endpoints.Resources;

/// <summary>نشانگر منبع خطاهای Order.</summary>
public static class OrderErrorResources
{
    /// <summary>ResourceManager برای OrderErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Order.Endpoints.Resources.OrderErrors", typeof(OrderErrorResources).Assembly);
}

/// <summary>مجموعهٔ منبع Order — بدون شاخهٔ en/fa دستی.</summary>
public sealed class OrderErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("order.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("checkout.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("shipping.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("pending.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.Equals(StorefrontOrderErrors.PaymentMissing, StringComparison.OrdinalIgnoreCase)
        || localizationKey.Equals(StorefrontOrderErrors.PaymentRejected, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        OrderErrorResources.Manager.GetString(localizationKey, culture);
}
