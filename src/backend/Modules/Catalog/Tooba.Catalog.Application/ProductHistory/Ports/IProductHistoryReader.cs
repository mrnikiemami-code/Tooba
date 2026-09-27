using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.ProductHistory.Ports;

/// <summary>Catalog-owned focused read seam for Admin product history paging.</summary>
public interface IProductHistoryReader
{
    /// <summary>
    /// Lists product history (newest first) with optional section filter and normalized skip/take.
    /// Missing product yields <c>workspace.product.missing</c>.
    /// </summary>
    Task<Result<ProductHistoryPage>> ListAsync(
        Guid productId,
        string? section,
        int skip,
        int take,
        CancellationToken cancellationToken);
}
