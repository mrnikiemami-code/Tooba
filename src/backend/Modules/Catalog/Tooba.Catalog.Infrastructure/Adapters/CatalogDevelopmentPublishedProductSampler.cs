using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Adapters;

/// <summary>Development sampler: distinct-category Published products from Catalog persistence.</summary>
public sealed class CatalogDevelopmentPublishedProductSampler(CatalogDbContext catalog)
    : ICatalogDevelopmentPublishedProductSampler
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> TakePublishedProductIdsFromDistinctCategoriesAsync(
        int take,
        CancellationToken cancellationToken)
    {
        if (take <= 0)
        {
            return Array.Empty<Guid>();
        }

        var links = await catalog.ProductCategories.AsNoTracking()
            .Where(link => catalog.Products.Any(product =>
                product.ProductId == link.ProductId && product.Status == CatalogPublicationStatus.Published))
            .OrderBy(link => link.CategoryId).ThenBy(link => link.ProductId)
            .ToListAsync(cancellationToken);

        return links
            .GroupBy(x => x.CategoryId)
            .Select(x => x.First().ProductId)
            .Take(take)
            .ToList();
    }
}
