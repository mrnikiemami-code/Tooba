using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Shipping.Endpoints.Resources;

/// <summary>نشانگر منبع خطاهای Shipping.</summary>
public static class ShippingErrorResources
{
    /// <summary>ResourceManager برای ShippingErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Shipping.Endpoints.Resources.ShippingErrors", typeof(ShippingErrorResources).Assembly);
}

/// <summary>
/// مجموعهٔ منبع Shipping — مالکیت صریح کلید <c>shipping.*</c> بدون شاخه‌بندی دستی زبان.
/// پیشوندهای Foundation و سایر ماژول‌ها (مانند <c>shipping_service*</c> که مالکیت Fulfillment است)
/// اینجا ادعا نمی‌شوند.
/// </summary>
public sealed class ShippingErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("shipping.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        ShippingErrorResources.Manager.GetString(localizationKey, culture);
}
