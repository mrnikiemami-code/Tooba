using Tooba.Content.Application.Articles.Commands;
using Tooba.Content.Application.Articles.Models;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;

namespace Tooba.Content.Application.Articles.Ports;

/// <summary>قابلیت خواندن و مدیریت مقالات Content.</summary>
public interface IContentDirectory
{
    Task<PagedResult<PublishedArticleItem>> ListPublishedAsync(
        int page, int pageSize, string? category, string? locale,
        Guid? categoryId, Guid? authorId, CancellationToken cancellationToken);

    Task<PublishedArticleItem?> GetPublishedBySlugAsync(
        string slug, string? locale, CancellationToken cancellationToken);

    Task<IReadOnlyList<PublishedArticleItem>> ListPublishedForHomeAsync(
        int limit, string? locale, CancellationToken cancellationToken);

    Task<PagedResult<AdminArticleSnapshot>> ListAllAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<AdminArticleSnapshot?> GetByIdAsync(Guid articleId, CancellationToken cancellationToken);

    Task<AdminArticleSnapshot> CreateAsync(CreateArticleCommand command, CancellationToken cancellationToken);

    Task<AdminArticleSnapshot> UpdateAsync(UpdateArticleCommand command, CancellationToken cancellationToken);

    Task<AdminArticleSnapshot> PublishAsync(Guid articleId, CancellationToken cancellationToken);

    Task<AdminArticleSnapshot> UnpublishAsync(Guid articleId, CancellationToken cancellationToken);

    Task<AdminArticleSnapshot> ArchiveAsync(Guid articleId, CancellationToken cancellationToken);

    Task DeleteDraftAsync(Guid articleId, CancellationToken cancellationToken);

    Task<ArticlePublicationReadiness> GetPublishReadinessAsync(Guid articleId, CancellationToken cancellationToken);

    Task<ArticlePreviewSnapshot?> GetPreviewAsync(Guid articleId, CancellationToken cancellationToken);

    Task<ArticleHistoryPage> ListHistoryAsync(Guid articleId, int skip, int take, CancellationToken cancellationToken);
}
