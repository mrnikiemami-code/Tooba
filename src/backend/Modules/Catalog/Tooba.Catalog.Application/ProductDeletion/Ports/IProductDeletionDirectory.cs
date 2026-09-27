using Tooba.BuildingBlocks.Results;

namespace Tooba.Catalog.Application.ProductDeletion.Ports;

/// <summary>Catalog-owned hard-delete / soft-archive-on-Offer-reference for Admin products.</summary>
public interface IProductDeletionDirectory
{
    /// <summary>
    /// Hard-deletes the product graph when no Offer references variants;
    /// otherwise archives and fails with <c>workspace.product.delete.referenced</c>.
    /// </summary>
    Task<Result> DeleteOrSoftArchiveAsync(Guid productId, CancellationToken cancellationToken);
}
