using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;

namespace Tooba.Content.Application.Models;

/// <summary>نتیجهٔ صفحه‌بندی‌شدهٔ عمومی.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount);

/// <summary>DTO عمومی مقالهٔ Published برای ریل خانه و مسیرهای محتوا.</summary>
public sealed record PublishedArticleItem(
    Guid ArticleId,
    string Slug,
    string Title,
    string Excerpt,
    Guid? CoverMediaAssetId,
    DateTimeOffset PublishDate,
    string AuthorDisplayName,
    IReadOnlyList<string> Tags,
    bool IsFeatured,
    string? Body,
    string? SeoTitle,
    string? SeoDescription,
    string? Category,
    Guid? CategoryId,
    Guid? AuthorId,
    string Locale,
    Guid? SeoImageMediaAssetId,
    string? CanonicalPath,
    string? CategorySlug = null,
    string? AuthorSlug = null);

/// <summary>نمای کامل مدیریتی مقاله.</summary>
public sealed record AdminArticleSnapshot(
    Guid ArticleId,
    string Slug,
    string Title,
    string Excerpt,
    string Body,
    string Locale,
    string? SeoTitle,
    string? SeoDescription,
    string? Category,
    Guid? CategoryId,
    Guid? AuthorId,
    Guid? CoverMediaAssetId,
    Guid? SeoImageMediaAssetId,
    string AuthorDisplayName,
    IReadOnlyList<string> Tags,
    bool IsFeatured,
    ContentPublicationStatus Status,
    DateTimeOffset PublishDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>فرمان ایجاد مقالهٔ Draft.</summary>
public sealed record CreateArticleCommand(
    string Slug,
    string Title,
    string Excerpt,
    string Body,
    Guid? CoverMediaAssetId,
    Guid? AuthorId,
    IReadOnlyList<string> Tags,
    bool IsFeatured,
    DateTimeOffset? PublishDate,
    string? Locale,
    string? SeoTitle,
    string? SeoDescription,
    string? Category,
    Guid? CategoryId);

/// <summary>فرمان به‌روزرسانی فیلدهای تحریری مقاله.</summary>
public sealed record UpdateArticleCommand(
    string Title,
    string Excerpt,
    string Body,
    Guid? CoverMediaAssetId,
    Guid? AuthorId,
    IReadOnlyList<string> Tags,
    bool IsFeatured,
    string? Locale,
    string? SeoTitle,
    string? SeoDescription,
    string? Category,
    Guid? CategoryId,
    DateTimeOffset? PublishDate);

/// <summary>پیش‌نمایش Admin مقاله — همان فیلدهای عمومی + پرچم پیش‌نمایش.</summary>
public sealed record ArticlePreviewSnapshot(
    Guid ArticleId,
    string Slug,
    string Title,
    string Excerpt,
    string Body,
    string Locale,
    string? SeoTitle,
    string? SeoDescription,
    string? Category,
    Guid? CategoryId,
    Guid? AuthorId,
    Guid? CoverMediaAssetId,
    Guid? SeoImageMediaAssetId,
    string AuthorDisplayName,
    IReadOnlyList<string> Tags,
    bool IsFeatured,
    ContentPublicationStatus Status,
    DateTimeOffset PublishDate,
    string? CategorySlug,
    string? AuthorSlug,
    string? CanonicalPath,
    bool IsPreview,
    bool RobotsNoIndex);

/// <summary>یک ردیف تاریخچهٔ انسانی مقاله.</summary>
public sealed record ArticleHistoryEntryDto(
    Guid HistoryId,
    Guid ArticleId,
    string EventType,
    string EventLabelFa,
    string EventLabelEn,
    string SummaryFa,
    string SummaryEn,
    string? PreviousState,
    string? NewState,
    Guid? ActorUserId,
    string ActorDisplayName,
    DateTimeOffset OccurredAt);

/// <summary>صفحهٔ تاریخچهٔ مقاله.</summary>
public sealed record ArticleHistoryPage(
    IReadOnlyList<ArticleHistoryEntryDto> Items,
    int TotalCount,
    int Skip,
    int Take);

