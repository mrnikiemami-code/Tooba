using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Host.Grid;

namespace Tooba.Host.Content;

/// <summary>ترکیب HTTP برای مسیرهای عمومی و مدیریتی Content.</summary>
public sealed class ContentPanelComposer
{
    private readonly IContentDirectory _content;
    private readonly AdminContentGridQueryEngine _grid;

    /// <summary>دایرکتوری Content و DbContext را تزریق می‌کند.</summary>
    public ContentPanelComposer(IContentDirectory content, ContentDbContext db)
    {
        _content = content;
        _grid = new AdminContentGridQueryEngine(db);
    }

    /// <summary>صفحهٔ مقالات Published.</summary>
    public Task<PagedResult<PublishedArticleItem>> ListPublishedAsync(
        int page,
        int pageSize,
        string? category,
        CancellationToken cancellationToken) =>
        _content.ListPublishedAsync(page, pageSize, category, cancellationToken);

    /// <summary>جزئیات Published با slug.</summary>
    public Task<PublishedArticleItem?> GetPublishedBySlugAsync(
        string slug,
        string? locale,
        CancellationToken cancellationToken) =>
        _content.GetPublishedBySlugAsync(slug, locale, cancellationToken);

    /// <summary>فهرست admin.</summary>
    public Task<PagedResult<AdminArticleSnapshot>> ListAllAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        _content.ListAllAsync(page, pageSize, cancellationToken);

    /// <summary>صفحه‌بندی server-side گرید مقالات Admin (DB-native).</summary>
    public Task<GridPageResponse<AdminArticleSnapshot>> QueryGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = AdminListGridPolicies.Content.Normalize(request);
        return _grid.QueryAsync(q, cancellationToken);
    }

    /// <summary>جزئیات admin.</summary>
    public Task<AdminArticleSnapshot?> GetByIdAsync(Guid articleId, CancellationToken cancellationToken) =>
        _content.GetByIdAsync(articleId, cancellationToken);

    /// <summary>ایجاد مقاله.</summary>
    public Task<AdminArticleSnapshot> CreateAsync(CreateArticleBody body, CancellationToken cancellationToken) =>
        _content.CreateAsync(
            new CreateArticleCommand(
                body.Slug,
                body.Title,
                body.Excerpt,
                body.Body,
                body.CoverMediaAssetId,
                body.AuthorDisplayName,
                body.Tags ?? [],
                body.IsFeatured,
                body.PublishDate,
                body.Locale,
                body.SeoTitle,
                body.SeoDescription,
                body.Category,
                body.CategoryId),
            cancellationToken);

    /// <summary>به‌روزرسانی مقاله.</summary>
    public Task<AdminArticleSnapshot> UpdateAsync(Guid articleId, UpdateArticleBody body, CancellationToken cancellationToken) =>
        _content.UpdateAsync(
            articleId,
            new UpdateArticleCommand(
                body.Title,
                body.Excerpt,
                body.Body,
                body.CoverMediaAssetId,
                body.AuthorDisplayName,
                body.Tags ?? [],
                body.IsFeatured,
                body.Locale,
                body.SeoTitle,
                body.SeoDescription,
                body.Category,
                body.CategoryId),
            cancellationToken);

    /// <summary>انتشار مقاله.</summary>
    public Task<AdminArticleSnapshot> PublishAsync(Guid articleId, CancellationToken cancellationToken) =>
        _content.PublishAsync(articleId, cancellationToken);

    /// <summary>خارج‌کردن از انتشار.</summary>
    public Task<AdminArticleSnapshot> UnpublishAsync(Guid articleId, CancellationToken cancellationToken) =>
        _content.UnpublishAsync(articleId, cancellationToken);
}
