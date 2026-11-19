using Tooba.BuildingBlocks.Grid;
using Tooba.Party.Contracts.Ports;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Domain.Enums;
using Tooba.Story.Infrastructure.Persistence;

namespace Tooba.Story.Infrastructure.Adapters;

/// <summary>
/// پیاده‌سازی درز گرید استوری Admin روی موتور گرید داخلی ماژول و سیاست normalize مالک Story.
/// این Adapter تنها لایهٔ اتصال Application به Infrastructure است و هیچ منطق HTTP یا Endpoint در آن نیست.
/// </summary>
public sealed class AdminStoryGridAdapter(StoryDbContext db, IPartyAdminSellerReadGateway? parties = null)
    : IAdminStoryGridPort
{
    /// <inheritdoc />
    public Task<GridPageResponse<AdminStorySnapshot>> QueryAsync(
        Guid tenantId,
        StoryReviewStatus? reviewStatus,
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = Grid.StoryAdminGridPolicies.Normalize(request);
        return new Grid.AdminStoryGridQueryEngine(db, parties).QueryAsync(tenantId, reviewStatus, q, cancellationToken);
    }
}
