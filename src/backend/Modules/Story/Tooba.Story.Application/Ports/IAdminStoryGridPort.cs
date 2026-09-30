using Tooba.BuildingBlocks.Grid;
using Tooba.Story.Domain;

namespace Tooba.Story.Application;

/// <summary>Admin story grid — DB-native paging; Infrastructure implements.</summary>
public interface IAdminStoryGridPort
{
    /// <summary>صفحه‌بندی server-side گرید استوری Admin.</summary>
    Task<GridPageResponse<AdminStorySnapshot>> QueryAsync(
        Guid tenantId,
        StoryReviewStatus? reviewStatus,
        GridQueryRequest request,
        CancellationToken cancellationToken);
}
