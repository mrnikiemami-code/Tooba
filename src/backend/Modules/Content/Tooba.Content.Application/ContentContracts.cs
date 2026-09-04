using Tooba.Content.Domain;

namespace Tooba.Content.Application;

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

/// <summary>قابلیت خواندن و مدیریت مقالات Content.</summary>
public interface IContentDirectory
{
    /// <summary>صفحهٔ مقالات Published را با فیلتر اختیاری دسته/نویسنده/locale برمی‌گرداند.</summary>
    Task<PagedResult<PublishedArticleItem>> ListPublishedAsync(
        int page,
        int pageSize,
        string? category,
        string? locale,
        Guid? categoryId,
        Guid? authorId,
        CancellationToken cancellationToken);

    /// <summary>جزئیات مقالهٔ Published را با slug و locale اختیاری برمی‌گرداند.</summary>
    Task<PublishedArticleItem?> GetPublishedBySlugAsync(
        string slug,
        string? locale,
        CancellationToken cancellationToken);

    /// <summary>جدیدترین مقالات Published را برای ریل خانه برمی‌گرداند.</summary>
    Task<IReadOnlyList<PublishedArticleItem>> ListPublishedForHomeAsync(
        int limit,
        string? locale,
        CancellationToken cancellationToken);

    /// <summary>صفحهٔ همهٔ مقالات (admin) را برمی‌گرداند.</summary>
    Task<PagedResult<AdminArticleSnapshot>> ListAllAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>مقاله را با شناسه برای admin برمی‌گرداند.</summary>
    Task<AdminArticleSnapshot?> GetByIdAsync(Guid articleId, CancellationToken cancellationToken);

    /// <summary>مقالهٔ Draft جدید می‌سازد.</summary>
    Task<AdminArticleSnapshot> CreateAsync(CreateArticleCommand command, CancellationToken cancellationToken);

    /// <summary>فیلدهای تحریری مقاله را به‌روزرسانی می‌کند.</summary>
    Task<AdminArticleSnapshot> UpdateAsync(Guid articleId, UpdateArticleCommand command, CancellationToken cancellationToken);

    /// <summary>مقاله را منتشر می‌کند.</summary>
    Task<AdminArticleSnapshot> PublishAsync(Guid articleId, CancellationToken cancellationToken);

    /// <summary>مقاله را از انتشار خارج می‌کند.</summary>
    Task<AdminArticleSnapshot> UnpublishAsync(Guid articleId, CancellationToken cancellationToken);

    /// <summary>مقاله را بایگانی می‌کند.</summary>
    Task<AdminArticleSnapshot> ArchiveAsync(Guid articleId, CancellationToken cancellationToken);

    /// <summary>پیش‌نویس را حذف دائمی می‌کند (فقط Draft).</summary>
    Task DeleteDraftAsync(Guid articleId, CancellationToken cancellationToken);

    /// <summary>آمادگی انتشار — همان قوانین دروازهٔ Publish.</summary>
    Task<ArticlePublicationReadiness> GetPublishReadinessAsync(Guid articleId, CancellationToken cancellationToken);

    /// <summary>پیش‌نمایش Admin برای Draft/منتشرنشده — بدون عمومی‌سازی.</summary>
    Task<ArticlePreviewSnapshot?> GetPreviewAsync(Guid articleId, CancellationToken cancellationToken);

    /// <summary>تاریخچهٔ چرخهٔ عمر مقاله (جدیدترین اول).</summary>
    Task<ArticleHistoryPage> ListHistoryAsync(Guid articleId, int skip, int take, CancellationToken cancellationToken);
}

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
