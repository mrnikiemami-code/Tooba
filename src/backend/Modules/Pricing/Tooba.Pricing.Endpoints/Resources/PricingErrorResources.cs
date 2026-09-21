using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Pricing.Endpoints.Resources;

/// <summary>Resource marker for Pricing errors.</summary>
public static class PricingErrorResources
{
    /// <summary>ResourceManager for PricingErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Pricing.Endpoints.Resources.PricingErrors", typeof(PricingErrorResources).Assembly);
}

/// <summary>Pricing error resources. Locale selection stays in the central localizer.</summary>
public sealed class PricingErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("pricing.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        PricingErrorResources.Manager.GetString(localizationKey, culture);
}
