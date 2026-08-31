using Tooba.BuildingBlocks.Grid;
using Tooba.Catalog.Application;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Grid;
using Tooba.Reviews.Infrastructure.Persistence;

namespace Tooba.Host.Reviews;

/// <summary>ترکیب GridQuery برای صف نظرات Admin.</summary>
public sealed class ReviewPanelComposer
{
    private readonly AdminReviewGridQueryEngine _grid;

    /// <summary>سازنده.</summary>
    public ReviewPanelComposer(
        ReviewsDbContext reviews,
        CatalogDbContext catalog,
        ICatalogLookupGateway catalogLookup) =>
        _grid = new AdminReviewGridQueryEngine(reviews, catalog, catalogLookup);

    /// <summary>صفحه‌بندی server-side گرید نظرات در انتظار Admin (DB-native).</summary>
    public Task<GridPageResponse<AdminReviewItem>> QueryPendingGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = AdminListGridPolicies.Reviews.Normalize(request);
        return _grid.QueryAsync(q, cancellationToken);
    }
}
