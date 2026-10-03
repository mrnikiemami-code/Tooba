using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.OperatorProfile.Contracts.Errors;

/// <summary>Resource manager marker for OperatorProfileErrors.resx.</summary>
public static class OperatorProfileErrorResources
{
    /// <summary>ResourceManager for OperatorProfile error resources.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.OperatorProfile.Contracts.Resources.OperatorProfileErrors", typeof(OperatorProfileErrorResources).Assembly);
}

/// <summary>OperatorProfile-owned error resource set for the <c>operator.profile.</c> key space.</summary>
public sealed class OperatorProfileErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("operator.profile.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        OperatorProfileErrorResources.Manager.GetString(localizationKey, culture);
}
