using Microsoft.EntityFrameworkCore;
using Tooba.Inventory.Contracts;
using Tooba.Inventory.Infrastructure.Persistence;

namespace Tooba.Inventory.Infrastructure.Adapters;

/// <summary>Applies Inventory schema migrations without exposing InventoryDbContext to Host callers.</summary>
public sealed class InventorySchemaMigrator(InventoryDbContext db) : IInventorySchemaMigrator
{
    /// <inheritdoc />
    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        db.Database.MigrateAsync(cancellationToken);
}
