using Tooba.BuildingBlocks.Grid;
using Tooba.Catalog.Application;
using Tooba.Host.Grid;
using Tooba.Reviews.Application;

namespace Tooba.Host.Reviews;

/// <summary>ترکیب GridQuery برای صف نظرات Admin.</summary>
public sealed class ReviewPanelComposer
{
    private readonly IReviewDirectory _reviews;
    private readonly ICatalogLookupGateway _catalog;

    /// <summary>سازنده.</summary>
    public ReviewPanelComposer(IReviewDirectory reviews, ICatalogLookupGateway catalog)
    {
        _reviews = reviews;
        _catalog = catalog;
    }

    /// <summary>صفحه‌بندی server-side گرید نظرات در انتظار Admin.</summary>
    public async Task<GridPageResponse<AdminReviewItem>> QueryPendingGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var pending = await _reviews.GetPendingAsync(1, GridQueryPolicyBase.DefaultMaxPageSize, cancellationToken);
        var titles = await _catalog.GetProductTitlesAsync(
            pending.Items.Select(x => x.ProductId).Distinct().ToArray(),
            cancellationToken);
        var rows = pending.Items.Select(x => new AdminReviewItem(
            x.ReviewId,
            titles.GetValueOrDefault(x.ProductId) ?? "محصول",
            x.AuthorDisplayName,
            x.Rating,
            x.Title,
            x.Body,
            x.Status.ToString(),
            x.IsVerifiedPurchase,
            x.CreatedAt)).ToList();
        return AdminListGridPolicies.Reviews.Execute(rows, request);
    }
}
