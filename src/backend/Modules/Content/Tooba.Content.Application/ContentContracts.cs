namespace Tooba.Content.Application;

/// <summary>DTO عمومی مقالهٔ Published برای ریل خانه.</summary>
public sealed record PublishedArticleItem(
    Guid ArticleId,
    string Slug,
    string Title,
    string Excerpt,
    Guid? CoverMediaAssetId,
    DateTimeOffset PublishDate,
    string AuthorDisplayName,
    IReadOnlyList<string> Tags,
    bool IsFeatured);

/// <summary>قابلیت خواندن مقالات Published برای ویترین.</summary>
public interface IContentDirectory
{
    /// <summary>جدیدترین مقالات Published را برای ریل خانه برمی‌گرداند.</summary>
    Task<IReadOnlyList<PublishedArticleItem>> ListPublishedForHomeAsync(int limit, CancellationToken cancellationToken);
}
