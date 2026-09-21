using Microsoft.EntityFrameworkCore;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Inventory.Infrastructure.Adapters;

/// <summary>Migration tooling entrypoint so Host/MigrationRunner never name InventoryDbContext.</summary>
public static class InventoryModuleMigration
{
    /// <summary>Stable module label for migration registries.</summary>
    public const string Module = "Inventory";

    /// <summary>Inventory PostgreSQL schema name.</summary>
    public static string Schema => InventoryDbContext.Schema;

    /// <summary>Creates an Inventory DbContext for tooling migrations.</summary>
    public static DbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            connectionString,
            InventoryDbContext.Schema,
            typeof(InventoryDbContext));
        return new InventoryDbContext(options.Options);
    }
}
