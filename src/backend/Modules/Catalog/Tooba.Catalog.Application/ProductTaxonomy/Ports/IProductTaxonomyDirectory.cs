using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductTaxonomy.Models;

namespace Tooba.Catalog.Application.ProductTaxonomy.Ports;

/// <summary>Catalog-owned product category/brand taxonomy mutations.</summary>
public interface IProductTaxonomyDirectory
{
    /// <summary>Assigns or replaces the product primary category.</summary>
    Task<Result> AssignPrimaryCategoryAsync(
        Guid productId,
        WorkspaceProductCategoryAssignWriteModel model,
        CancellationToken cancellationToken);

    /// <summary>Adds an additional (non-schema) category link.</summary>
    Task<Result> AddAdditionalCategoryAsync(
        Guid productId,
        WorkspaceProductAdditionalCategoryWriteModel model,
        CancellationToken cancellationToken);

    /// <summary>Removes an additional category link.</summary>
    Task<Result> RemoveAdditionalCategoryAsync(
        Guid productId,
        Guid categoryId,
        DateTimeOffset expectedUpdatedAt,
        CancellationToken cancellationToken);

    /// <summary>Assigns or clears the product brand.</summary>
    Task<Result> AssignBrandAsync(
        Guid productId,
        WorkspaceProductBrandAssignWriteModel model,
        CancellationToken cancellationToken);
}
