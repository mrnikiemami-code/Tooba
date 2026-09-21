using Microsoft.EntityFrameworkCore;
using Tooba.Promotion.Contracts;
using Tooba.Promotion.Infrastructure.Persistence;

namespace Tooba.Promotion.Infrastructure.Adapters;

/// <summary>Applies Promotion schema migrations without exposing PromotionDbContext to Host callers.</summary>
public sealed class PromotionSchemaMigrator(PromotionDbContext db) : IPromotionSchemaMigrator
{
    /// <inheritdoc />
    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        db.Database.MigrateAsync(cancellationToken);
}
