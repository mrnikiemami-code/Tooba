namespace Tooba.Inventory.Contracts.Availability;

/// <summary>Inventory-owned schema migration entrypoint so Host bootstraps never type InventoryDbContext.</summary>
public interface IInventorySchemaMigrator
{
    /// <summary>Applies pending Inventory EF migrations.</summary>
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
