using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Party.Contracts.Errors;

/// <summary>Resource manager marker for PartyErrors.resx.</summary>
public static class PartyErrorResources
{
    /// <summary>ResourceManager for Party error resources.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Party.Contracts.Resources.PartyErrors", typeof(PartyErrorResources).Assembly);
}

/// <summary>Party-owned error resource set for seller.settings.* and party.* keys.</summary>
public sealed class PartyErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("seller.settings.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("party.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        PartyErrorResources.Manager.GetString(localizationKey, culture);
}
