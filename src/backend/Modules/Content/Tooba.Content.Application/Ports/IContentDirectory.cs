using Tooba.Content.Application.Models;
using Tooba.Content.Domain;

namespace Tooba.Content.Application.Ports;

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

