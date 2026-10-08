using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Returns.Contracts.Errors;

/// <summary>Resource manager marker for the Returns bilingual error resources.</summary>
public static class ReturnsErrorResources
{
    /// <summary>ResourceManager for ReturnsErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Returns.Contracts.Resources.ReturnsErrors", typeof(ReturnsErrorResources).Assembly);
}

/// <summary>
/// Returns-owned error resource set for the <c>return.</c> / <c>refund.</c> keyspace. Returns owns the
/// user-facing text for its own stable codes so the module can be extracted as an isolated microservice
/// without leaving its copy behind in another module. Locale selection stays in the central localizer.
/// </summary>
public sealed class ReturnsErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("return.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("refund.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        ReturnsErrorResources.Manager.GetString(localizationKey, culture);
}
