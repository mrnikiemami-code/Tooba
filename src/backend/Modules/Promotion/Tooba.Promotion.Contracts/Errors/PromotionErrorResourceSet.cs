using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Promotion.Contracts.Errors;

/// <summary>Resource manager marker for the Promotion bilingual error resources.</summary>
public static class PromotionErrorResources
{
    /// <summary>ResourceManager for PromotionErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Promotion.Contracts.Resources.PromotionErrors", typeof(PromotionErrorResources).Assembly);
}

/// <summary>
/// Promotion-owned error resource set for the <c>promotion.</c> / <c>merchandising.</c> /
/// <c>campaign.</c> keyspace. Promotion owns the user-facing text for its own stable codes so the module
/// can be extracted as an isolated microservice without leaving its copy behind in another module.
/// Locale selection stays in the central localizer.
/// </summary>
public sealed class PromotionErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("promotion.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("merchandising.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("campaign.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        PromotionErrorResources.Manager.GetString(localizationKey, culture);
}
