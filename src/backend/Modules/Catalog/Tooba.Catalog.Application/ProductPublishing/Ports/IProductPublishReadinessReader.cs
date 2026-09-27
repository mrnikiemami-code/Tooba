using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.ProductPublishing.Ports;

/// <summary>Catalog-owned focused read seam for Admin product publish readiness.</summary>
public interface IProductPublishReadinessReader
{
    /// <summary>
    /// Catalog-only publish readiness (category/identity/attributes/variants/media/seo).
    /// Missing product yields <c>workspace.product.missing</c>.
    /// </summary>
    Task<Result<ProductPublishReadiness>> GetAsync(
        Guid productId,
        string? locale,
        CancellationToken cancellationToken);
}
