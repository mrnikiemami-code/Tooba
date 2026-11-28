namespace Tooba.Inventory.Contracts.Seller;

/// <summary>Inventory-owned reason identifiers for system-authored stock changes, not errors.</summary>
public static class InventoryAdjustmentReasons
{
    /// <summary>First physical initialization of an untouched position.</summary>
    public const string InitialStock = "inventory.initial_stock";
}
