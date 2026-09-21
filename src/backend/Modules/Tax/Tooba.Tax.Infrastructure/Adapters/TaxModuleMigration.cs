using Microsoft.EntityFrameworkCore;
using Tooba.Persistence;
using Tooba.Tax.Infrastructure.Persistence;

namespace Tooba.Tax.Infrastructure.Adapters;

/// <summary>Migration tooling entrypoint so Host/MigrationRunner never name TaxDbContext.</summary>
public static class TaxModuleMigration
{
    /// <summary>Stable module label for migration registries.</summary>
    public const string Module = "Tax";

    /// <summary>Tax PostgreSQL schema name.</summary>
    public static string Schema => TaxDbContext.Schema;

    /// <summary>Creates a Tax DbContext for tooling migrations.</summary>
    public static DbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TaxDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            connectionString,
            TaxDbContext.Schema,
            typeof(TaxDbContext));
        return new TaxDbContext(options.Options);
    }
}
