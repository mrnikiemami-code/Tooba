using Microsoft.EntityFrameworkCore;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Localization.Application;

namespace Tooba.Content.Infrastructure;

/// <summary>دایرکتوری Content با schema مستقل.</summary>
public sealed class ContentDirectory : IContentDirectory
{
    private readonly ContentDbContext _db;
    private readonly ILanguageDirectory _languages;
    private readonly IContentCategoryDirectory _categories;
    private readonly IContentAuthorDirectory _authors;

    /// <summary>DbContext مالک را تزریق می‌کند.</summary>
    public ContentDirectory(
        ContentDbContext db,
        ILanguageDirectory languages,
        IContentCategoryDirectory categories,
        IContentAuthorDirectory authors)
    {
        _db = db;
        _languages = languages;
        _categories = categories;
        _authors = authors;
    }

    /// <inheritdoc />
    public async Task<PagedResult<PublishedArticleItem>> ListPublishedAsync(
        int page,
        int pageSize,
        string? category,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = _db.Articles.AsNoTracking()
            .Where(article => article.Status == ContentPublicationStatus.Published);
        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalized = category.Trim();
            query = query.Where(article => article.Category == normalized);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(article => article.PublishDate)
            .ThenBy(article => article.ArticleId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<PublishedArticleItem>(
            rows.Select(article => MapPublished(article, includeBody: false)).ToList(),
            page,
            pageSize,
            total);
    }

    /// <inheritdoc />
    public async Task<PublishedArticleItem?> GetPublishedBySlugAsync(
        string slug,
        string? locale,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var query = _db.Articles.AsNoTracking()
            .Where(article =>
                article.Status == ContentPublicationStatus.Published
                && article.Slug == normalizedSlug);
        if (!string.IsNullOrWhiteSpace(locale))
        {
            var normalizedLocale = locale.Trim();
            query = query.Where(article => article.Locale == normalizedLocale);
        }

        var article = await query.FirstOrDefaultAsync(cancellationToken);
        return article is null ? null : MapPublished(article, includeBody: true);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedArticleItem>> ListPublishedForHomeAsync(int limit, CancellationToken cancellationToken)
    {
        limit = Math.Clamp(limit, 1, 12);
        var rows = await _db.Articles.AsNoTracking()
            .Where(article => article.Status == ContentPublicationStatus.Published)
            .OrderByDescending(article => article.PublishDate)
            .ThenBy(article => article.ArticleId)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return rows.Select(article => MapPublished(article, includeBody: false)).ToList();
    }

    /// <inheritdoc />
    public async Task<PagedResult<AdminArticleSnapshot>> ListAllAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Articles.AsNoTracking();
        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(article => article.UpdatedAt)
            .ThenBy(article => article.ArticleId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<AdminArticleSnapshot>(
            rows.Select(MapAdmin).ToList(),
            page,
            pageSize,
            total);
    }

    /// <inheritdoc />
    public async Task<AdminArticleSnapshot?> GetByIdAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ArticleId == articleId, cancellationToken);
        return article is null ? null : MapAdmin(article);
    }

    /// <inheritdoc />
    public async Task<AdminArticleSnapshot> CreateAsync(CreateArticleCommand command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var slug = command.Slug.Trim().ToLowerInvariant();
        if (await _db.Articles.AnyAsync(article => article.Slug == slug, cancellationToken))
            throw new InvalidOperationException("slug مقاله تکراری است.");

        var locale = string.IsNullOrWhiteSpace(command.Locale) ? ContentArticle.DefaultLocale : command.Locale.Trim();
        await _languages.EnsureActiveLanguageCodeAsync(locale, cancellationToken);
        await _categories.EnsureArticleCategoryLanguageMatchAsync(locale, command.CategoryId, cancellationToken);
        await _authors.EnsureArticleAuthorAssignmentAsync(command.AuthorId, isNewAssignment: true, cancellationToken);
        var categoryLabel = await ResolveCategoryLabelAsync(command.CategoryId, command.Category, cancellationToken);
        var authorDisplayName = await ResolveAuthorDisplayNameAsync(command.AuthorId, cancellationToken);

        var article = ContentArticle.Create(
            command.Slug,
            command.Title,
            command.Excerpt,
            command.Body,
            command.CoverMediaAssetId,
            command.AuthorId,
            authorDisplayName,
            command.Tags ?? [],
            command.IsFeatured,
            command.PublishDate ?? now,
            now,
            locale,
            command.SeoTitle,
            command.SeoDescription,
            categoryLabel,
            command.CategoryId);
        _db.Articles.Add(article);
        await _db.SaveChangesAsync(cancellationToken);
        return MapAdmin(article);
    }

    /// <inheritdoc />
    public async Task<AdminArticleSnapshot> UpdateAsync(
        Guid articleId,
        UpdateArticleCommand command,
        CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(row => row.ArticleId == articleId, cancellationToken)
            ?? throw new InvalidOperationException("مقاله یافت نشد.");
        var now = DateTimeOffset.UtcNow;
        var locale = string.IsNullOrWhiteSpace(command.Locale) ? article.Locale : command.Locale.Trim();
        if (!string.Equals(locale, article.Locale, StringComparison.Ordinal)
            && (article.Status == ContentPublicationStatus.Published
                || article.CategoryId is not null
                || article.AuthorId is not null))
        {
            throw new InvalidOperationException(ContentArticleErrorCodes.LocaleLocked);
        }

        await _languages.EnsureActiveLanguageCodeAsync(locale, cancellationToken);
        await _categories.EnsureArticleCategoryLanguageMatchAsync(locale, command.CategoryId, cancellationToken);
        var isNewAuthorAssignment = command.AuthorId != article.AuthorId;
        await _authors.EnsureArticleAuthorAssignmentAsync(command.AuthorId, isNewAuthorAssignment, cancellationToken);
        var categoryLabel = await ResolveCategoryLabelAsync(command.CategoryId, command.Category, cancellationToken);
        var authorDisplayName = await ResolveAuthorDisplayNameAsync(command.AuthorId, cancellationToken);
        article.Update(
            command.Title,
            command.Excerpt,
            command.Body,
            command.SeoTitle,
            command.SeoDescription,
            categoryLabel,
            command.CategoryId,
            command.CoverMediaAssetId,
            command.AuthorId,
            authorDisplayName,
            command.Tags ?? [],
            command.IsFeatured,
            now,
            locale,
            command.PublishDate);
        await _db.SaveChangesAsync(cancellationToken);
        return MapAdmin(article);
    }

    /// <inheritdoc />
    public async Task<AdminArticleSnapshot> PublishAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(row => row.ArticleId == articleId, cancellationToken)
            ?? throw new InvalidOperationException("مقاله یافت نشد.");
        await _authors.EnsurePublishableAuthorAsync(article.AuthorId, cancellationToken);
        article.Publish(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return MapAdmin(article);
    }

    /// <inheritdoc />
    public async Task<AdminArticleSnapshot> UnpublishAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(row => row.ArticleId == articleId, cancellationToken)
            ?? throw new InvalidOperationException("مقاله یافت نشد.");
        article.Unpublish(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return MapAdmin(article);
    }

    internal static PublishedArticleItem MapPublished(ContentArticle article, bool includeBody) => new(
        article.ArticleId,
        article.Slug,
        article.Title,
        article.Excerpt,
        article.CoverMediaAssetId,
        article.PublishDate,
        article.AuthorDisplayName,
        ParseTags(article.TagsCsv),
        article.IsFeatured,
        includeBody ? article.Body : null,
        article.SeoTitle,
        article.SeoDescription,
        article.Category,
        article.CategoryId,
        article.AuthorId,
        article.Locale);

    internal static AdminArticleSnapshot MapAdmin(ContentArticle article) => new(
        article.ArticleId,
        article.Slug,
        article.Title,
        article.Excerpt,
        article.Body,
        article.Locale,
        article.SeoTitle,
        article.SeoDescription,
        article.Category,
        article.CategoryId,
        article.AuthorId,
        article.CoverMediaAssetId,
        article.AuthorDisplayName,
        ParseTags(article.TagsCsv),
        article.IsFeatured,
        article.Status,
        article.PublishDate,
        article.CreatedAt,
        article.UpdatedAt);

    private async Task<string?> ResolveCategoryLabelAsync(
        Guid? categoryId,
        string? fallbackCategory,
        CancellationToken cancellationToken)
    {
        if (categoryId is null)
        {
            return fallbackCategory;
        }

        var workspace = await _categories.GetWorkspaceAsync(categoryId.Value, cancellationToken);
        return workspace?.Name ?? fallbackCategory;
    }

    private async Task<string> ResolveAuthorDisplayNameAsync(Guid? authorId, CancellationToken cancellationToken)
    {
        if (authorId is null)
        {
            throw new InvalidOperationException(ContentAuthorErrorCodes.NotFound);
        }

        var workspace = await _authors.GetWorkspaceAsync(authorId.Value, cancellationToken)
            ?? throw new InvalidOperationException(ContentAuthorErrorCodes.NotFound);
        return workspace.DisplayName;
    }

    private static IReadOnlyList<string> ParseTags(string tagsCsv) =>
        string.IsNullOrWhiteSpace(tagsCsv)
            ? []
            : tagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
