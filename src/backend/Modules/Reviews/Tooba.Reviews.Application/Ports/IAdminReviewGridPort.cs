using Tooba.BuildingBlocks.Grid;

namespace Tooba.Reviews.Application;

/// <summary>Admin pending-reviews grid — DB-native paging; Infrastructure implements.</summary>
public interface IAdminReviewGridPort
{
    /// <summary>صفحه‌بندی server-side گرید نظرات Pending Admin.</summary>
    Task<GridPageResponse<AdminReviewItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken);
}
