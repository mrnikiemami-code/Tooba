namespace Tooba.Reviews.Application;

/// <summary>ردیف امن صف مدیریت Reviews با عنوان واقعی Product.</summary>
public sealed record AdminReviewItem(
    Guid ReviewId,
    string ProductTitle,
    string AuthorDisplayName,
    int Rating,
    string? Title,
    string Body,
    string Status,
    bool VerifiedPurchase,
    DateTimeOffset CreatedAt);
