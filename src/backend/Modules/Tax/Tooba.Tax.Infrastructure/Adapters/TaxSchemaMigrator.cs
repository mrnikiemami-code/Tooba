using Microsoft.EntityFrameworkCore;
using Tooba.Tax.Contracts;
using Tooba.Tax.Infrastructure.Persistence;

namespace Tooba.Tax.Infrastructure.Adapters;

/// <summary>Applies Tax schema migrations without exposing TaxDbContext to Host callers.</summary>
public sealed class TaxSchemaMigrator(TaxDbContext db) : ITaxSchemaMigrator
{
    /// <inheritdoc />
    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        db.Database.MigrateAsync(cancellationToken);
}
