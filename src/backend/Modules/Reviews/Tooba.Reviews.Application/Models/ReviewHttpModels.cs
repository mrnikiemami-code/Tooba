namespace Tooba.Reviews.Application.Models;

/// <summary>درخواست رد مدیریتی با دلیل داخلی.</summary>
public sealed record RejectReviewRequest(string Reason);

/// <summary>پاسخ عمومی صریح و سازگار Reviews.</summary>
public sealed record PublicReviewsResponse(
    decimal? AverageRating,
    long ReviewCount,
    IReadOnlyDictionary<int, long> RatingDistribution,
    IReadOnlyList<PublicReviewItem> Reviews,
    int Page,
    int PageSize,
    long TotalCount);

/// <summary>ردیف عمومی بررسی بدون هویت داخلی.</summary>
public sealed record PublicReviewItem(
    Guid ReviewId,
    string AuthorDisplayName,
    int Rating,
    string? Title,
    string Body,
    bool VerifiedPurchase,
    DateTimeOffset CreatedAt);

/// <summary>صف صریح مدیریت Reviews.</summary>
public sealed record AdminReviewsResponse(IReadOnlyList<AdminReviewItem> Reviews, int Page, int PageSize, long TotalCount);

/// <summary>
/// پاسخ فهرست نظرات فروشنده برای محصولات متعلق به Offerهای خودش؛ بدون پاسخ‌فروشنده و بدون هویت داخلی نویسنده.
/// </summary>
public sealed record SellerReviewsResponse(
    IReadOnlyList<SellerReviewItem> Reviews,
    int Page,
    int PageSize,
    long TotalCount,
    long PublishedCount,
    long PendingCount,
    long RejectedCount,
    bool SellerResponseSupported);

/// <summary>ردیف امن فهرست فروشنده با عنوان محصول واقعی و برچسب وضعیت قابل‌نمایش.</summary>
public sealed record SellerReviewItem(
    Guid ReviewId,
    string ProductTitle,
    string AuthorDisplayName,
    int Rating,
    string? Title,
    string Body,
    string StatusLabel,
    string Status,
    bool VerifiedPurchase,
    DateTimeOffset CreatedAt);
