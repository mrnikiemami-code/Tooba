using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.ProductSeo.Ports;

/// <summary>Catalog-owned port for Admin product SEO read, update, and readiness.</summary>
public interface IProductSeoDirectory
{
    /// <summary>SEO detail for a product locale (SlugSeam + localized title/description).</summary>
    Task<Result<ProductSeoDetail>> GetAsync(
        Guid productId,
        string? locale,
        CancellationToken cancellationToken);

    /// <summary>Update SEO atomically (localized fields + slug + history + UpdatedAt).</summary>
    Task<Result<ProductSeoDetail>> UpdateAsync(
        Guid productId,
        ProductSeoUpdateInput input,
        CancellationToken cancellationToken);

    /// <summary>SEO readiness snapshot for a product locale.</summary>
    Task<Result<ProductSeoReadiness>> GetReadinessAsync(
        Guid productId,
        string? locale,
        CancellationToken cancellationToken);
}
