using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Inventory.Contracts.Errors;

/// <summary>Resource manager marker for InventoryErrors.resx.</summary>
public static class InventoryErrorResources
{
    /// <summary>ResourceManager for Inventory error resources.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Inventory.Contracts.Resources.InventoryErrors", typeof(InventoryErrorResources).Assembly);
}

/// <summary>
/// Inventory-owned error resource set. Owns the <c>inventory.</c> key space so the canonical
/// localizer resolves Inventory boundary titles without ad-hoc culture branching.
/// </summary>
public sealed class InventoryErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("inventory.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        InventoryErrorResources.Manager.GetString(localizationKey, culture);
}
