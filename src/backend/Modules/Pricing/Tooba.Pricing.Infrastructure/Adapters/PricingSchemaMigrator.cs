using Microsoft.EntityFrameworkCore;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Infrastructure.Persistence;

namespace Tooba.Pricing.Infrastructure.Adapters;

/// <summary>Applies Pricing schema migrations without exposing PricingDbContext to Host callers.</summary>
public sealed class PricingSchemaMigrator(PricingDbContext db) : IPricingSchemaMigrator
{
    /// <inheritdoc />
    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        db.Database.MigrateAsync(cancellationToken);
}
