namespace Tooba.Reviews.Contracts.Storefront;

/// <summary>Published review summary for storefront product cards / PDP.</summary>
public sealed record StorefrontProductReviewSummaryDto(
    Guid ProductId,
    long ReviewCount,
    decimal? AverageRating);

/// <summary>Recent published review for home / landing rails.</summary>
public sealed record StorefrontFeaturedReviewDto(
    Guid ReviewId,
    string AuthorDisplayName,
    int Rating,
    string? Title,
    string Body,
    bool IsVerifiedPurchase,
    DateTimeOffset CreatedAt,
    Guid ProductId,
    string ProductTitle,
    string ProductSlug);

/// <summary>
/// Narrow Contracts port for storefront review enrichment (no Domain/Application leakage).
/// </summary>
public interface IReviewsStorefrontLookup
{
    /// <summary>Batch published summaries for product card composition.</summary>
    Task<IReadOnlyDictionary<Guid, StorefrontProductReviewSummaryDto>> GetPublishedSummariesAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);

    /// <summary>Newest published reviews for the home rail.</summary>
    Task<IReadOnlyList<StorefrontFeaturedReviewDto>> GetRecentPublishedForHomeAsync(
        int limit,
        CancellationToken cancellationToken);
}
