using Tooba.BuildingBlocks.Grid;
using Tooba.Catalog.Contracts;
using Tooba.Reviews.Application;
using Tooba.Reviews.Infrastructure.Persistence;

namespace Tooba.Reviews.Infrastructure.Adapters;

/// <summary>Application port over admin review grid engine + Reviews-owned normalize policy.</summary>
public sealed class AdminReviewGridAdapter(
    ReviewsDbContext reviews,
    ICatalogAdminProductTitleIdLookup productTitles) : IAdminReviewGridPort
{
    /// <inheritdoc />
    public Task<GridPageResponse<AdminReviewItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = Grid.ReviewsAdminGridPolicies.Normalize(request);
        return new Grid.AdminReviewGridQueryEngine(reviews, productTitles)
            .QueryAsync(q, cancellationToken);
    }
}
