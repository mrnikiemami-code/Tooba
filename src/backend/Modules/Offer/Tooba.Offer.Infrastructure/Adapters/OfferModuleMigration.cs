using Microsoft.EntityFrameworkCore;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Offer.Infrastructure.Adapters;

/// <summary>
/// Migration tooling entrypoint so Host/MigrationRunner never name OfferDbContext.
/// </summary>
public static class OfferModuleMigration
{
    /// <summary>Stable module label for migration registries.</summary>
    public const string Module = "Offer";

    /// <summary>Offer PostgreSQL schema name.</summary>
    public static string Schema => OfferDbContext.Schema;

    /// <summary>Creates an Offer DbContext for tooling migrations.</summary>
    public static DbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<OfferDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            connectionString,
            OfferDbContext.Schema,
            typeof(OfferDbContext));
        return new OfferDbContext(options.Options);
    }
}
