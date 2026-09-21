using Microsoft.EntityFrameworkCore;
using Tooba.Persistence;
using Tooba.Promotion.Infrastructure.Persistence;

namespace Tooba.Promotion.Infrastructure.Adapters;

/// <summary>Migration tooling entrypoint so Host/MigrationRunner never name PromotionDbContext.</summary>
public static class PromotionModuleMigration
{
    /// <summary>Stable module label for migration registries.</summary>
    public const string Module = "Promotion";

    /// <summary>Promotion PostgreSQL schema name.</summary>
    public static string Schema => PromotionDbContext.Schema;

    /// <summary>Creates a Promotion DbContext for tooling migrations.</summary>
    public static DbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PromotionDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            connectionString,
            PromotionDbContext.Schema,
            typeof(PromotionDbContext));
        return new PromotionDbContext(options.Options);
    }
}
