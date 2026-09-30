using Tooba.BuildingBlocks.Grid;
using Tooba.Story.Application;
using Tooba.Story.Domain;
using Tooba.Story.Infrastructure.Persistence;

namespace Tooba.Story.Infrastructure.Adapters;

/// <summary>Application port over admin story grid engine + Story-owned normalize policy.</summary>
public sealed class AdminStoryGridAdapter(StoryDbContext db) : IAdminStoryGridPort
{
    /// <inheritdoc />
    public Task<GridPageResponse<AdminStorySnapshot>> QueryAsync(
        Guid tenantId,
        StoryReviewStatus? reviewStatus,
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = Grid.StoryAdminGridPolicies.Normalize(request);
        return new Grid.AdminStoryGridQueryEngine(db).QueryAsync(tenantId, reviewStatus, q, cancellationToken);
    }
}
