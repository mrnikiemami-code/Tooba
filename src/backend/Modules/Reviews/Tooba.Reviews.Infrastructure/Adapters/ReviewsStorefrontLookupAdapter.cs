using Tooba.Reviews.Application;
using Tooba.Reviews.Contracts.Storefront;

namespace Tooba.Reviews.Infrastructure.Adapters;

/// <summary>Adapts Application review directory to Contracts storefront lookup.</summary>
public sealed class ReviewsStorefrontLookupAdapter : IReviewsStorefrontLookup
{
    private readonly IReviewDirectory _reviews;

    /// <summary>Creates the adapter.</summary>
    public ReviewsStorefrontLookupAdapter(IReviewDirectory reviews) => _reviews = reviews;

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, StorefrontProductReviewSummaryDto>> GetPublishedSummariesAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var summaries = await _reviews.GetPublishedSummariesAsync(productIds, cancellationToken);
        return summaries.ToDictionary(
            kv => kv.Key,
            kv => new StorefrontProductReviewSummaryDto(kv.Value.ProductId, kv.Value.ReviewCount, kv.Value.AverageRating));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StorefrontFeaturedReviewDto>> GetRecentPublishedForHomeAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var reviews = await _reviews.GetRecentPublishedForHomeAsync(limit, cancellationToken);
        return reviews.Select(r => new StorefrontFeaturedReviewDto(
            r.ReviewId,
            r.AuthorDisplayName,
            r.Rating,
            r.Title,
            r.Body,
            r.IsVerifiedPurchase,
            r.CreatedAt,
            r.ProductId,
            r.ProductTitle,
            r.ProductSlug)).ToList();
    }
}
