using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Tax.Contracts.Errors;

/// <summary>Resource manager marker for the Tax bilingual error resources.</summary>
public static class TaxErrorResources
{
    /// <summary>ResourceManager for TaxErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Tax.Contracts.Resources.TaxErrors", typeof(TaxErrorResources).Assembly);
}

/// <summary>
/// Tax-owned error resource set for the <c>tax.</c> keyspace. Tax owns the user-facing text for its own
/// stable codes so the module can be extracted as an isolated microservice without leaving its copy
/// behind in another module. Locale selection stays in the central localizer.
/// </summary>
public sealed class TaxErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("tax.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        TaxErrorResources.Manager.GetString(localizationKey, culture);
}
