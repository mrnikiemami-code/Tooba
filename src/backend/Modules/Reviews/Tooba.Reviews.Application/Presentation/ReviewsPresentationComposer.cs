using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Offer.Contracts.Ports;
using Tooba.Reviews.Application.Models;
using Tooba.Reviews.Domain;

namespace Tooba.Reviews.Application.Presentation;

/// <summary>ترکیب use-case برای مسیرهای عمومی، مشتری، فروشنده و مدیریتی Reviews.</summary>
public sealed class ReviewsPresentationComposer
{
    private readonly IReviewDirectory _reviews;
    private readonly IAdminReviewGridPort _grid;
    private readonly ICatalogAdminProductTitleIdLookup _titles;
    private readonly IOfferSellerProductIdLookup _sellerProducts;

    /// <summary>دایرکتوری، گرید و پورت‌های Contracts را تزریق می‌کند.</summary>
    public ReviewsPresentationComposer(
        IReviewDirectory reviews,
        IAdminReviewGridPort grid,
        ICatalogAdminProductTitleIdLookup titles,
        IOfferSellerProductIdLookup sellerProducts)
    {
        _reviews = reviews;
        _grid = grid;
        _titles = titles;
        _sellerProducts = sellerProducts;
    }

    /// <summary>صفحهٔ عمومی Published.</summary>
    public async Task<PublicReviewsResponse?> GetPublishedAsync(
        string slug, int page, int pageSize, CancellationToken cancellationToken)
    {
        var result = await _reviews.GetPublishedAsync(slug, page, pageSize, cancellationToken);
        if (result is null) return null;
        return new PublicReviewsResponse(
            result.Summary.Count == 0 ? null : result.Summary.Average,
            result.Summary.Count,
            result.Summary.Distribution,
            result.Items.Select(x => new PublicReviewItem(
                x.ReviewId, x.AuthorDisplayName, x.Rating, x.Title, x.Body,
                x.IsVerifiedPurchase, x.CreatedAt)).ToList(),
            result.Page,
            result.PageSize,
            result.Summary.Count);
    }

    /// <summary>ثبت نظر مشتری.</summary>
    public Task<Guid> SubmitAsync(Guid actorUserId, SubmitProductReview body, CancellationToken cancellationToken) =>
        _reviews.SubmitAsync(actorUserId, body, cancellationToken);

    /// <summary>فهرست فروشنده روی محصولات Offerهای خودش.</summary>
    public async Task<SellerReviewsResponse> ListSellerAsync(
        Guid sellerPartyId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var productIds = await _sellerProducts.ListDistinctProductIdsForSellerAsync(sellerPartyId, cancellationToken);
        var statusFilter = ParseSellerStatus(status);
        var scoped = await _reviews.ListForProductsAsync(productIds, statusFilter, page, pageSize, cancellationToken);
        var titles = await _titles.GetProductTitlesByIdsAsync(
            scoped.Items.Select(x => x.ProductId).Distinct().ToArray(),
            cancellationToken);
        return new SellerReviewsResponse(
            scoped.Items.Select(x => new SellerReviewItem(
                x.ReviewId,
                titles.GetValueOrDefault(x.ProductId) ?? "محصول",
                x.AuthorDisplayName,
                x.Rating,
                x.Title,
                x.Body,
                MapSellerStatusLabel(x.Status),
                x.Status.ToString(),
                x.IsVerifiedPurchase,
                x.CreatedAt)).ToList(),
            scoped.Page,
            scoped.PageSize,
            scoped.TotalCount,
            scoped.PublishedCount,
            scoped.PendingCount,
            scoped.RejectedCount,
            SellerResponseSupported: false);
    }

    /// <summary>صف Pending مدیر.</summary>
    public async Task<AdminReviewsResponse> GetPendingAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var pending = await _reviews.GetPendingAsync(page, pageSize, cancellationToken);
        var titles = await _titles.GetProductTitlesByIdsAsync(
            pending.Items.Select(x => x.ProductId).Distinct().ToArray(),
            cancellationToken);
        return new AdminReviewsResponse(
            pending.Items.Select(x => new AdminReviewItem(
                x.ReviewId,
                titles.GetValueOrDefault(x.ProductId) ?? "محصول",
                x.AuthorDisplayName,
                x.Rating,
                x.Title,
                x.Body,
                x.Status.ToString(),
                x.IsVerifiedPurchase,
                x.CreatedAt)).ToList(),
            pending.Page,
            pending.PageSize,
            pending.TotalCount);
    }

    /// <summary>گرید Pending مدیر.</summary>
    public Task<GridPageResponse<AdminReviewItem>> QueryPendingGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken) =>
        _grid.QueryAsync(request, cancellationToken);

    /// <summary>انتشار.</summary>
    public Task PublishAsync(Guid reviewId, Guid moderatorUserId, CancellationToken cancellationToken) =>
        _reviews.PublishAsync(reviewId, moderatorUserId, cancellationToken);

    /// <summary>رد.</summary>
    public Task RejectAsync(Guid reviewId, Guid moderatorUserId, string reason, CancellationToken cancellationToken) =>
        _reviews.RejectAsync(reviewId, moderatorUserId, reason, cancellationToken);

    private static ReviewStatus? ParseSellerStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            return null;

        if (string.Equals(status, "Published", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "تایید شده", StringComparison.Ordinal))
            return ReviewStatus.Published;

        if (string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "در انتظار", StringComparison.Ordinal))
            return ReviewStatus.Pending;

        if (string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "رد شده", StringComparison.Ordinal))
            return ReviewStatus.Rejected;

        return null;
    }

    private static string MapSellerStatusLabel(ReviewStatus status) => status switch
    {
        ReviewStatus.Published => "تایید شده",
        ReviewStatus.Pending => "در انتظار",
        ReviewStatus.Rejected => "رد شده",
        _ => status.ToString(),
    };
}
