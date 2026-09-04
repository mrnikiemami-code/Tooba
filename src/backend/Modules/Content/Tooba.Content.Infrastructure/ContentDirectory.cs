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
    private readonly IContentTagDirectory _tags;

    /// <summary>DbContext مالک را تزریق می‌کند.</summary>
    public ContentDirectory(
        ContentDbContext db,
        ILanguageDirectory languages,
        IContentCategoryDirectory categories,
        IContentAuthorDirectory authors,
        IContentTagDirectory tags)
    {
        _db = db;
        _languages = languages;
        _categories = categories;
        _authors = authors;
        _tags = tags;
    }

    /// <inheritdoc />
    public async Task<PagedResult<PublishedArticleItem>> ListPublishedAsync(
        int page,
        int pageSize,
        string? category,
        string? locale,
        Guid? categoryId,
        Guid? authorId,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var utcNow = DateTimeOffset.UtcNow;
        var query = PubliclyVisibleArticles(utcNow);
        if (!string.IsNullOrWhiteSpace(locale))
        {
            var normalizedLocale = locale.Trim();
            query = query.Where(article => article.Locale == normalizedLocale);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalized = category.Trim();
            query = query.Where(article => article.Category == normalized);
        }

        if (categoryId is Guid selectedCategoryId)
        {
            query = query.Where(article => article.CategoryId == selectedCategoryId);
        }

        if (authorId is Guid selectedAuthorId)
        {
            query = query.Where(article => article.AuthorId == selectedAuthorId);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(article => article.PublishDate)
            .ThenBy(article => article.ArticleId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<PublishedArticleItem>(
            await MapPublishedBatchAsync(rows, includeBody: false, cancellationToken),
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
        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(locale))
        {
            return null;
        }

        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var normalizedLocale = locale.Trim();
        var utcNow = DateTimeOffset.UtcNow;
        var article = await PubliclyVisibleArticles(utcNow)
            .Where(row => row.Slug == normalizedSlug && row.Locale == normalizedLocale)
            .FirstOrDefaultAsync(cancellationToken);
        if (article is null)
        {
            return null;
        }

        var mapped = await MapPublishedBatchAsync([article], includeBody: true, cancellationToken);
        return mapped[0];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedArticleItem>> ListPublishedForHomeAsync(
        int limit,
        string? locale,
        CancellationToken cancellationToken)
    {
        limit = Math.Clamp(limit, 1, 12);
        var utcNow = DateTimeOffset.UtcNow;
        var query = PubliclyVisibleArticles(utcNow);
        if (!string.IsNullOrWhiteSpace(locale))
        {
            var normalizedLocale = locale.Trim();
            query = query.Where(article => article.Locale == normalizedLocale);
        }

        var rows = await query
            .OrderByDescending(article => article.PublishDate)
            .ThenBy(article => article.ArticleId)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return await MapPublishedBatchAsync(rows, includeBody: false, cancellationToken);
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
        var snapshots = new List<AdminArticleSnapshot>(rows.Count);
        foreach (var row in rows)
        {
            snapshots.Add(await MapAdminAsync(row, cancellationToken));
        }

        return new PagedResult<AdminArticleSnapshot>(snapshots, page, pageSize, total);
    }

    /// <inheritdoc />
    public async Task<AdminArticleSnapshot?> GetByIdAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ArticleId == articleId, cancellationToken);
        return article is null ? null : await MapAdminAsync(article, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminArticleSnapshot> CreateAsync(CreateArticleCommand command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var slug = command.Slug.Trim().ToLowerInvariant();
        var locale = string.IsNullOrWhiteSpace(command.Locale) ? ContentArticle.DefaultLocale : command.Locale.Trim();
        if (await _db.Articles.AnyAsync(
                article => article.Slug == slug && article.Locale == locale,
                cancellationToken))
        {
            throw new InvalidOperationException("slug مقاله تکراری است.");
        }

        await _languages.EnsureActiveLanguageCodeAsync(locale, cancellationToken);
        await _categories.EnsureArticleCategoryLanguageMatchAsync(locale, command.CategoryId, cancellationToken, isNewAssignment: true);
        await _authors.EnsureArticleAuthorAssignmentAsync(command.AuthorId, isNewAssignment: true, cancellationToken);
        var categoryLabel = await ResolveCategoryLabelAsync(command.CategoryId, command.Category, cancellationToken);
        var authorDisplayName = await ResolveAuthorDisplayNameAsync(command.AuthorId, cancellationToken);

        // TagsCsv دیگر canonical نیست؛ انتساب از IContentTagDirectory انجام می‌شود.
        var article = ContentArticle.Create(
            command.Slug,
            command.Title,
            command.Excerpt,
            command.Body,
            command.CoverMediaAssetId,
            command.AuthorId,
            authorDisplayName,
            [],
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
        return await MapAdminAsync(article, cancellationToken);
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
        if (!string.Equals(locale, article.Locale, StringComparison.Ordinal))
        {
            if (!article.CanChangeLocale())
            {
                throw new InvalidOperationException(ContentArticleErrorCodes.LocaleLocked);
            }

            if (await _db.ArticleMedia.AnyAsync(row => row.ArticleId == articleId, cancellationToken))
            {
                throw new InvalidOperationException(ContentArticleErrorCodes.LocaleLocked);
            }

            if (await _db.ArticleTags.AnyAsync(row => row.ArticleId == articleId, cancellationToken))
            {
                throw new InvalidOperationException(ContentArticleErrorCodes.LocaleLocked);
            }
        }

        await _languages.EnsureActiveLanguageCodeAsync(locale, cancellationToken);
        var isNewCategoryAssignment = command.CategoryId != article.CategoryId;
        await _categories.EnsureArticleCategoryLanguageMatchAsync(
            locale,
            command.CategoryId,
            cancellationToken,
            isNewAssignment: isNewCategoryAssignment);
        var isNewAuthorAssignment = command.AuthorId != article.AuthorId;
        await _authors.EnsureArticleAuthorAssignmentAsync(command.AuthorId, isNewAuthorAssignment, cancellationToken);
        var categoryLabel = await ResolveCategoryLabelAsync(command.CategoryId, command.Category, cancellationToken);
        var authorDisplayName = await ResolveAuthorDisplayNameAsync(command.AuthorId, cancellationToken);
        var existingTagNames = await ResolveTagNamesAsync(article, cancellationToken);
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
            existingTagNames,
            command.IsFeatured,
            now,
            locale,
            command.PublishDate);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAdminAsync(article, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminArticleSnapshot> PublishAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(row => row.ArticleId == articleId, cancellationToken)
            ?? throw new InvalidOperationException("مقاله یافت نشد.");
        await _authors.EnsurePublishableAuthorAsync(article.AuthorId, cancellationToken);
        article.Publish(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAdminAsync(article, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminArticleSnapshot> UnpublishAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(row => row.ArticleId == articleId, cancellationToken)
            ?? throw new InvalidOperationException("مقاله یافت نشد.");
        article.Unpublish(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAdminAsync(article, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminArticleSnapshot> ArchiveAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(row => row.ArticleId == articleId, cancellationToken)
            ?? throw new InvalidOperationException("مقاله یافت نشد.");
        if (!ContentArticleLifecycleRules.CanArchive(article.Status))
            throw new InvalidOperationException(ContentArticleErrorCodes.ArchiveNotAllowed);
        article.Archive(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAdminAsync(article, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteDraftAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(row => row.ArticleId == articleId, cancellationToken)
            ?? throw new InvalidOperationException("مقاله یافت نشد.");
        if (!ContentArticleLifecycleRules.CanHardDelete(article.Status))
            throw new InvalidOperationException(ContentArticleErrorCodes.DeleteNotAllowed);

        var gallery = await _db.ArticleMedia.Where(row => row.ArticleId == articleId).ToListAsync(cancellationToken);
        if (gallery.Count > 0)
            _db.ArticleMedia.RemoveRange(gallery);
        _db.Articles.Remove(article);
        await _db.SaveChangesAsync(cancellationToken);
    }

    internal static PublishedArticleItem MapPublished(
        ContentArticle article,
        bool includeBody,
        string? categorySlug = null,
        string? authorSlug = null,
        IReadOnlyList<string>? tags = null) => new(
        article.ArticleId,
        article.Slug,
        article.Title,
        article.Excerpt,
        article.CoverMediaAssetId,
        article.PublishDate,
        article.AuthorDisplayName,
        tags ?? ParseTags(article.TagsCsv),
        article.IsFeatured,
        includeBody ? article.Body : null,
        article.SeoTitle,
        article.SeoDescription,
        article.Category,
        article.CategoryId,
        article.AuthorId,
        article.Locale,
        article.ResolveEffectiveSeoImageId(),
        ContentArticleSeoRules.BuildPublicPath(article.Locale, article.Slug),
        categorySlug,
        authorSlug);

    private async Task<IReadOnlyList<PublishedArticleItem>> MapPublishedBatchAsync(
        IReadOnlyList<ContentArticle> rows,
        bool includeBody,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var categoryIds = rows
            .Where(article => article.CategoryId is not null)
            .Select(article => article.CategoryId!.Value)
            .Distinct()
            .ToList();
        var authorIds = rows
            .Where(article => article.AuthorId is not null)
            .Select(article => article.AuthorId!.Value)
            .Distinct()
            .ToList();
        var articleIds = rows.Select(article => article.ArticleId).ToList();

        var categorySlugs = categoryIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Categories.AsNoTracking()
                .Where(category => categoryIds.Contains(category.CategoryId))
                .ToDictionaryAsync(category => category.CategoryId, category => category.Slug, cancellationToken);
        var authorSlugs = authorIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Authors.AsNoTracking()
                .Where(author => authorIds.Contains(author.AuthorId))
                .ToDictionaryAsync(author => author.AuthorId, author => author.Slug, cancellationToken);
        var tagNamesByArticle = await _tags.GetArticleTagNamesAsync(articleIds, cancellationToken);

        return rows.Select(article => MapPublished(
            article,
            includeBody,
            article.CategoryId is Guid categoryId && categorySlugs.TryGetValue(categoryId, out var categorySlug)
                ? categorySlug
                : null,
            article.AuthorId is Guid authorId && authorSlugs.TryGetValue(authorId, out var authorSlug)
                ? authorSlug
                : null,
            tagNamesByArticle.TryGetValue(article.ArticleId, out var tagNames) && tagNames.Count > 0
                ? tagNames
                : ParseTags(article.TagsCsv))).ToList();
    }

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
        article.SeoImageMediaAssetId,
        article.AuthorDisplayName,
        ParseTags(article.TagsCsv),
        article.IsFeatured,
        article.Status,
        article.PublishDate,
        article.CreatedAt,
        article.UpdatedAt);

    private async Task<AdminArticleSnapshot> MapAdminAsync(ContentArticle article, CancellationToken cancellationToken)
    {
        var tags = await ResolveTagNamesAsync(article, cancellationToken);
        return new AdminArticleSnapshot(
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
            article.SeoImageMediaAssetId,
            article.AuthorDisplayName,
            tags,
            article.IsFeatured,
            article.Status,
            article.PublishDate,
            article.CreatedAt,
            article.UpdatedAt);
    }

    private async Task<IReadOnlyList<string>> ResolveTagNamesAsync(
        ContentArticle article,
        CancellationToken cancellationToken)
    {
        var map = await _tags.GetArticleTagNamesAsync([article.ArticleId], cancellationToken);
        if (map.TryGetValue(article.ArticleId, out var names) && names.Count > 0)
        {
            return names;
        }

        return ParseTags(article.TagsCsv);
    }

    private IQueryable<ContentArticle> PubliclyVisibleArticles(DateTimeOffset utcNow) =>
        _db.Articles.AsNoTracking()
            .Where(article =>
                article.Status == ContentPublicationStatus.Published
                && article.PublishDate <= utcNow);

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
            return string.Empty;
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