using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Settlement.Contracts.Errors;

/// <summary>Resource manager marker for the Settlement bilingual error resources.</summary>
public static class SettlementErrorResources
{
    /// <summary>ResourceManager for SettlementErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Settlement.Contracts.Resources.SettlementErrors", typeof(SettlementErrorResources).Assembly);
}

/// <summary>
/// Settlement-owned error resource set for the <c>settlement.</c> / <c>payout.</c> keyspace. Settlement
/// owns the user-facing text for its own stable codes so the module can be extracted as an isolated
/// microservice without leaving its copy behind in another module. Locale selection stays in the central
/// localizer.
/// </summary>
public sealed class SettlementErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("settlement.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("payout.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        SettlementErrorResources.Manager.GetString(localizationKey, culture);
}
