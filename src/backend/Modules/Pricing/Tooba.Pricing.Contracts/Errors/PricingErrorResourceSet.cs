using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Pricing.Contracts.Errors;

/// <summary>Resource manager marker for the Pricing bilingual error resources.</summary>
public static class PricingErrorResources
{
    /// <summary>ResourceManager for PricingErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Pricing.Contracts.Resources.PricingErrors", typeof(PricingErrorResources).Assembly);
}

/// <summary>
/// Pricing-owned error resource set for the <c>pricing.</c> keyspace. Pricing owns the user-facing
/// text for its own stable codes so the module can be extracted as an isolated microservice without
/// leaving its copy behind in another module. Locale selection stays in the central localizer.
/// </summary>
public sealed class PricingErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("pricing.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        PricingErrorResources.Manager.GetString(localizationKey, culture);
}
