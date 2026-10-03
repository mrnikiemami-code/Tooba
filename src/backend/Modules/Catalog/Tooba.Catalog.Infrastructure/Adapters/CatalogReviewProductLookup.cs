using Tooba.Catalog.Application;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;

namespace Tooba.Catalog.Infrastructure.Adapters;

/// <summary>Contracts adapter over Catalog lookup for Reviews product reads.</summary>
public sealed class CatalogReviewProductLookup(ICatalogLookupGateway catalog) : ICatalogReviewProductLookup
{
    /// <inheritdoc />
    public async Task<CatalogReviewableProductDto?> FindByIdAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await catalog.FindReviewableProductByIdAsync(productId, cancellationToken);
        return product is null ? null : Map(product);
    }

    /// <inheritdoc />
    public async Task<CatalogReviewableProductDto?> FindBySlugAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var product = await catalog.FindReviewableProductBySlugAsync(slug, cancellationToken);
        return product is null ? null : Map(product);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, CatalogReviewableProductDto>> GetByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var products = await catalog.GetReviewableProductsByIdsAsync(productIds, cancellationToken);
        return products.ToDictionary(x => x.Key, x => Map(x.Value));
    }

    private static CatalogReviewableProductDto Map(ReviewableProductReference product) =>
        new(product.ProductId, product.Slug, product.Title, product.Status.ToString(), product.VariantIds);
}
