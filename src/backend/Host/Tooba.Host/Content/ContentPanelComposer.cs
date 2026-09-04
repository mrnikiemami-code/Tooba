using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Host.Grid;

namespace Tooba.Host.Content;

/// <summary>ترکیب HTTP برای مسیرهای عمومی و مدیریتی Content.</summary>
public sealed class ContentPanelComposer
{
    private readonly IContentDirectory _content;
    private readonly IContentCategoryDirectory _categories;
    private readonly IContentAuthorDirectory _authors;
    private readonly AdminContentGridQueryEngine _grid;

    /// <summary>دایرکتوری Content و taxonomy را تزریق می‌کند.</summary>
    public ContentPanelComposer(
        IContentDirectory content,
        IContentCategoryDirectory categories,
        IContentAuthorDirectory authors,
        ContentDbContext db)
    {
        _content = content;
        _categories = categories;
        _authors = authors;
        _grid = new AdminContentGridQueryEngine(db);
    }

    /// <summary>صفحهٔ مقالات Published با فیلتر اختیاری دسته/نویسنده.</summary>
    public async Task<PagedResult<PublishedArticleItem>> ListPublishedAsync(
        int page,
        int pageSize,
        string? category,
        string? locale,
        string? categorySlug,
        string? authorSlug,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var filterLocale = string.IsNullOrWhiteSpace(locale)
            ? null
            : ContentTaxonomySeoRules.ResolveContentLocale(locale);
        var taxonomyLocale = ContentTaxonomySeoRules.ResolveContentLocale(locale);

        Guid? categoryId = null;
        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            var publicCategory = await _categories.GetPublicBySlugAsync(
                taxonomyLocale,
                categorySlug,
                cancellationToken);
            if (publicCategory is null)
            {
                return new PagedResult<PublishedArticleItem>([], page, pageSize, 0);
            }

            categoryId = publicCategory.CategoryId;
        }

        Guid? authorId = null;
        if (!string.IsNullOrWhiteSpace(authorSlug))
        {
            var publicAuthor = await _authors.GetPublicBySlugAsync(
                authorSlug,
                taxonomyLocale,
                cancellationToken);
            if (publicAuthor is null)
            {
                return new PagedResult<PublishedArticleItem>([], page, pageSize, 0);
            }

            authorId = publicAuthor.AuthorId;
        }

        return await _content.ListPublishedAsync(
            page,
            pageSize,
            category,
            filterLocale,
            categoryId,
            authorId,
            cancellationToken);
    }

    /// <summary>دستهٔ Active عمومی با slug.</summary>
    public Task<PublishedContentCategoryItem?> GetPublicCategoryBySlugAsync(
        string? locale,
        string slug,
        CancellationToken cancellationToken) =>
        _categories.GetPublicBySlugAsync(
            ContentTaxonomySeoRules.ResolveContentLocale(locale),
            slug,
            cancellationToken);

    /// <summary>فهرست دسته‌های Active برای sitemap.</summary>
    public Task<IReadOnlyList<PublishedContentCategoryItem>> ListPublicCategoriesAsync(
        string? locale,
        CancellationToken cancellationToken) =>
        _categories.ListPublicAsync(
            ContentTaxonomySeoRules.ResolveContentLocale(locale),
            cancellationToken);

    /// <summary>نویسندهٔ Active عمومی با slug.</summary>
    public Task<PublishedContentAuthorItem?> GetPublicAuthorBySlugAsync(
        string slug,
        string? locale,
        CancellationToken cancellationToken) =>
        _authors.GetPublicBySlugAsync(
            slug,
            ContentTaxonomySeoRules.ResolveContentLocale(locale),
            cancellationToken);

    /// <summary>فهرست نویسندگان Active برای sitemap.</summary>
    public Task<IReadOnlyList<PublishedContentAuthorItem>> ListPublicAuthorsAsync(
        string? locale,
        CancellationToken cancellationToken) =>
        _authors.ListPublicAsync(
            ContentTaxonomySeoRules.ResolveContentLocale(locale),
            cancellationToken);

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
                body.AuthorId,
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
                body.AuthorId,
                body.Tags ?? [],
                body.IsFeatured,
                body.Locale,
                body.SeoTitle,
                body.SeoDescription,
                body.Category,
                body.CategoryId,
                body.PublishDate),
            cancellationToken);

    /// <summary>انتشار مقاله.</summary>
    public Task<AdminArticleSnapshot> PublishAsync(Guid articleId, CancellationToken cancellationToken) =>
        _content.PublishAsync(articleId, cancellationToken);

    /// <summary>خارج‌کردن از انتشار.</summary>
    public Task<AdminArticleSnapshot> UnpublishAsync(Guid articleId, CancellationToken cancellationToken) =>
        _content.UnpublishAsync(articleId, cancellationToken);

    /// <summary>بایگانی مقاله.</summary>
    public Task<AdminArticleSnapshot> ArchiveAsync(Guid articleId, CancellationToken cancellationToken) =>
        _content.ArchiveAsync(articleId, cancellationToken);

    /// <summary>حذف پیش‌نویس.</summary>
    public Task DeleteDraftAsync(Guid articleId, CancellationToken cancellationToken) =>
        _content.DeleteDraftAsync(articleId, cancellationToken);

    /// <summary>آمادگی انتشار مقاله.</summary>
    public Task<ArticlePublicationReadiness> GetPublishReadinessAsync(
        Guid articleId,
        CancellationToken cancellationToken) =>
        _content.GetPublishReadinessAsync(articleId, cancellationToken);

    /// <summary>پیش‌نمایش Admin مقاله.</summary>
    public Task<ArticlePreviewSnapshot?> GetPreviewAsync(Guid articleId, CancellationToken cancellationToken) =>
        _content.GetPreviewAsync(articleId, cancellationToken);

    /// <summary>تاریخچهٔ چرخهٔ عمر مقاله.</summary>
    public Task<ArticleHistoryPage> ListHistoryAsync(
        Guid articleId,
        int skip,
        int take,
        CancellationToken cancellationToken) =>
        _content.ListHistoryAsync(articleId, skip, take, cancellationToken);
}
