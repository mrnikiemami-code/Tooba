using Tooba.BuildingBlocks.Grid;
using Tooba.Reviews.Application;

namespace Tooba.Host.Reviews;

/// <summary>ترکیب GridQuery برای صف نظرات Admin.</summary>
public sealed class ReviewPanelComposer
{
    private readonly IAdminReviewGridPort _grid;

    /// <summary>سازنده.</summary>
    public ReviewPanelComposer(IAdminReviewGridPort grid) => _grid = grid;

    /// <summary>صفحه‌بندی server-side گرید نظرات در انتظار Admin (DB-native).</summary>
    public Task<GridPageResponse<AdminReviewItem>> QueryPendingGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken) =>
        _grid.QueryAsync(request, cancellationToken);
}
