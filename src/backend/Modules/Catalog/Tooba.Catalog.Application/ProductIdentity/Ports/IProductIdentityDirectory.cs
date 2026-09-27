using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductIdentity.Models;

namespace Tooba.Catalog.Application.ProductIdentity.Ports;

/// <summary>Catalog-owned product identity mutations (create / title / core / quantity policy).</summary>
public interface IProductIdentityDirectory
{
    /// <summary>Creates a draft catalog product and assigns its primary category.</summary>
    Task<Result<Guid>> CreateWorkspaceProductAsync(
        WorkspaceProductCreateWriteModel model,
        CancellationToken cancellationToken);

    /// <summary>Updates a localized catalog title with optimistic concurrency.</summary>
    Task<Result> UpdateCatalogTitleAsync(
        Guid productId,
        WorkspaceProductCatalogTitleWriteModel model,
        CancellationToken cancellationToken);

    /// <summary>Updates product core fields and locale translations.</summary>
    Task<Result> UpdateProductCoreAsync(
        Guid productId,
        WorkspaceProductCoreUpdateWriteModel model,
        CancellationToken cancellationToken);

    /// <summary>Updates product quantity policy with optimistic concurrency.</summary>
    Task<Result> UpdateQuantityPolicyAsync(
        Guid productId,
        WorkspaceProductQuantityPolicyWriteModel model,
        CancellationToken cancellationToken);
}
