using Microsoft.EntityFrameworkCore;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Infrastructure.Persistence;

namespace Tooba.Offer.Infrastructure.Adapters;

/// <summary>Applies Offer schema migrations without exposing OfferDbContext to Host callers.</summary>
public sealed class OfferSchemaMigrator(OfferDbContext db) : IOfferSchemaMigrator
{
    /// <inheritdoc />
    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        db.Database.MigrateAsync(cancellationToken);
}
