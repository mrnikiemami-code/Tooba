using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>Counts Catalog products for the cross-module Admin dashboard through a Contracts boundary.</summary>
public sealed class CatalogAdminProductCountGateway(CatalogDbContext db) : ICatalogAdminProductCountGateway
{
    /// <inheritdoc />
    public Task<int> CountPublishedProductsAsync(CancellationToken cancellationToken) =>
        db.Products.AsNoTracking()
            .CountAsync(x => x.Status == CatalogPublicationStatus.Published, cancellationToken);
}
