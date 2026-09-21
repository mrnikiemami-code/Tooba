using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Offer.Endpoints.Resources;

/// <summary>نشانگر منبع خطاهای Offer.</summary>
public static class OfferErrorResources
{
    /// <summary>ResourceManager برای OfferErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Offer.Endpoints.Resources.OfferErrors", typeof(OfferErrorResources).Assembly);
}

/// <summary>مجموعهٔ منبع Offer — بدون شاخهٔ en/fa دستی.</summary>
public sealed class OfferErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("offer.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        OfferErrorResources.Manager.GetString(localizationKey, culture);
}
