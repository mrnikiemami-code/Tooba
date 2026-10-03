using Tooba.BuildingBlocks.Grid;

namespace Tooba.Catalog.Contracts.Ports;

/// <summary>
/// Catalog-owned Admin ProductWorkspace list/grid read boundary.
/// Returns semantic projections and page IDs — never EF entities or queryables.
/// </summary>
public interface ICatalogAdminProductWorkspaceListGateway
{
    /// <summary>Recent product IDs for the simple Admin list (newest first).</summary>
    Task<IReadOnlyList<Guid>> ListRecentProductIdsAsync(int take, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves server-side grid page product IDs (catalog filters/sorts + commercial metrics via Contracts).
    /// </summary>
    Task<(IReadOnlyList<Guid> PageIds, int TotalCount)> ResolveGridPageProductIdsAsync(
        GridQueryRequest query,
        CancellationToken cancellationToken);

    /// <summary>Catalog slice for list-row composition for the given product IDs (order preserved).</summary>
    Task<IReadOnlyList<CatalogAdminProductListSlice>> LoadListCatalogSlicesAsync(
        IReadOnlyList<Guid> productIds,
        CancellationToken cancellationToken);
}

/// <summary>Catalog-owned fields for Admin product list-row composition.</summary>
public sealed record CatalogAdminProductListSlice(
    Guid ProductId,
    string Title,
    string Status,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<Guid> VariantIds,
    string CategorySummary,
    Guid? PrimaryMediaAssetId,
    Guid? PrimaryCategoryId,
    string BrandName,
    string? PrimaryCategoryName,
    IReadOnlyList<string> AdditionalCategoryNames,
    int AdditionalCategoryCount);
