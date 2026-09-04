using Microsoft.EntityFrameworkCore;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure.Persistence;

namespace Tooba.Content.Infrastructure;

/// <summary>دایرکتوری برچسب محتوا — مالک Content.</summary>
public sealed class ContentTagDirectory : IContentTagDirectory
{
    private const int DefaultSearchLimit = 30;
    private const int MaxSearchLimit = 50;

    private readonly ContentDbContext _db;

    /// <summary>DbContext مالک Content را تزریق می‌کند.</summary>
    public ContentTagDirectory(ContentDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentTagDto>> SearchAsync(
        string languageCode,
        string? search,
        int limit,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var language = ContentTag.NormalizeLanguageCode(languageCode);
        var take = Math.Clamp(limit <= 0 ? DefaultSearchLimit : limit, 1, MaxSearchLimit);
        var query = _db.Tags.AsNoTracking().Where(x => x.LanguageCode == language);
        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var normalized = ContentTag.NormalizeName(term);
            query = query.Where(x =>
                x.Name.Contains(term) || x.NormalizedName.Contains(normalized));
        }

        var rows = await query
            .OrderBy(x => x.Name)
            .Take(take)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task<ContentTagDto> CreateAsync(
        CreateContentTagCommand command,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var tag = ContentTag.Create(command.LanguageCode, command.Name, now, command.Slug);
        if (await _db.Tags.AnyAsync(
                x => x.LanguageCode == tag.LanguageCode && x.NormalizedName == tag.NormalizedName,
                cancellationToken))
        {
            throw new InvalidOperationException(ContentTagErrorCodes.DuplicateName);
        }

        _db.Tags.Add(tag);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(tag);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentTagDto>> ListArticleTagsAsync(
        Guid articleId,
        CancellationToken cancellationToken)
    {
        await EnsureArticleExistsAsync(articleId, cancellationToken);
        var rows = await (
            from link in _db.ArticleTags.AsNoTracking()
            join tag in _db.Tags.AsNoTracking() on link.TagId equals tag.TagId
            where link.ArticleId == articleId
            orderby tag.Name
            select tag).ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentTagDto>> AssignToArticleAsync(
        Guid articleId,
        Guid tagId,
        CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(x => x.ArticleId == articleId, cancellationToken)
            ?? throw new InvalidOperationException(ContentTagErrorCodes.ArticleNotFound);
        var tag = await _db.Tags.AsNoTracking().FirstOrDefaultAsync(x => x.TagId == tagId, cancellationToken)
            ?? throw new InvalidOperationException(ContentTagErrorCodes.NotFound);

        if (!string.Equals(tag.LanguageCode, article.Locale, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(ContentTagErrorCodes.LanguageMismatch);
        }

        if (!tag.IsActive)
        {
            throw new InvalidOperationException(ContentTagErrorCodes.Inactive);
        }

        var exists = await _db.ArticleTags.AnyAsync(
            x => x.ArticleId == articleId && x.TagId == tagId,
            cancellationToken);
        if (!exists)
        {
            _db.ArticleTags.Add(ArticleTag.Create(articleId, tagId, DateTimeOffset.UtcNow));
            await SyncTagsCsvProjectionAsync(article, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return await ListArticleTagsAsync(articleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentTagDto>> RemoveFromArticleAsync(
        Guid articleId,
        Guid tagId,
        CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(x => x.ArticleId == articleId, cancellationToken)
            ?? throw new InvalidOperationException(ContentTagErrorCodes.ArticleNotFound);
        var link = await _db.ArticleTags.FirstOrDefaultAsync(
            x => x.ArticleId == articleId && x.TagId == tagId,
            cancellationToken);
        if (link is not null)
        {
            _db.ArticleTags.Remove(link);
            await SyncTagsCsvProjectionAsync(article, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return await ListArticleTagsAsync(articleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetArticleTagNamesAsync(
        IReadOnlyCollection<Guid> articleIds,
        CancellationToken cancellationToken)
    {
        if (articleIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<string>>();
        }

        var rows = await (
            from link in _db.ArticleTags.AsNoTracking()
            join tag in _db.Tags.AsNoTracking() on link.TagId equals tag.TagId
            where articleIds.Contains(link.ArticleId)
            orderby tag.Name
            select new { link.ArticleId, tag.Name }).ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.ArticleId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(x => x.Name).ToList());
    }

    private async Task EnsureArticleExistsAsync(Guid articleId, CancellationToken cancellationToken)
    {
        if (!await _db.Articles.AnyAsync(x => x.ArticleId == articleId, cancellationToken))
        {
            throw new InvalidOperationException(ContentTagErrorCodes.ArticleNotFound);
        }
    }

    private async Task SyncTagsCsvProjectionAsync(ContentArticle article, CancellationToken cancellationToken)
    {
        var tagIds = _db.ArticleTags.Local
            .Where(x => x.ArticleId == article.ArticleId)
            .Select(x => x.TagId)
            .ToHashSet();

        foreach (var entry in _db.ChangeTracker.Entries<ArticleTag>())
        {
            if (entry.Entity.ArticleId != article.ArticleId)
            {
                continue;
            }

            if (entry.State is EntityState.Added or EntityState.Unchanged or EntityState.Modified)
            {
                tagIds.Add(entry.Entity.TagId);
            }
            else if (entry.State == EntityState.Deleted)
            {
                tagIds.Remove(entry.Entity.TagId);
            }
        }

        var persisted = await _db.ArticleTags
            .Where(x => x.ArticleId == article.ArticleId)
            .Select(x => x.TagId)
            .ToListAsync(cancellationToken);
        foreach (var id in persisted)
        {
            tagIds.Add(id);
        }

        foreach (var entry in _db.ChangeTracker.Entries<ArticleTag>())
        {
            if (entry.State == EntityState.Deleted && entry.Entity.ArticleId == article.ArticleId)
            {
                tagIds.Remove(entry.Entity.TagId);
            }
        }

        var names = tagIds.Count == 0
            ? []
            : await _db.Tags.AsNoTracking()
                .Where(t => tagIds.Contains(t.TagId))
                .OrderBy(t => t.Name)
                .Select(t => t.Name)
                .ToListAsync(cancellationToken);

        article.SetTagsProjection(names, DateTimeOffset.UtcNow);
    }

    private static ContentTagDto Map(ContentTag tag) => new(
        tag.TagId,
        tag.LanguageCode,
        tag.Name,
        tag.NormalizedName,
        tag.Slug,
        tag.IsActive,
        tag.CreatedAt,
        tag.UpdatedAt);
}
