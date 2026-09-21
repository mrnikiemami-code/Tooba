using Microsoft.EntityFrameworkCore;
using Tooba.Persistence;
using Tooba.Pricing.Infrastructure.Persistence;

namespace Tooba.Pricing.Infrastructure.Adapters;

/// <summary>Migration tooling entrypoint so Host/MigrationRunner never name PricingDbContext.</summary>
public static class PricingModuleMigration
{
    /// <summary>Stable module label for migration registries.</summary>
    public const string Module = "Pricing";

    /// <summary>Pricing PostgreSQL schema name.</summary>
    public static string Schema => PricingDbContext.Schema;

    /// <summary>Creates a Pricing DbContext for tooling migrations.</summary>
    public static DbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PricingDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            connectionString,
            PricingDbContext.Schema,
            typeof(PricingDbContext));
        return new PricingDbContext(options.Options);
    }
}
