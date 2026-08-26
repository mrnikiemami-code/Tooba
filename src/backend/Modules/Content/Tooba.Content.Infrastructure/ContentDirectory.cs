using Microsoft.EntityFrameworkCore;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure.Persistence;

namespace Tooba.Content.Infrastructure;

/// <summary>دایرکتوری Content با schema مستقل.</summary>
public sealed class ContentDirectory : IContentDirectory
{
    private readonly ContentDbContext _db;

    /// <summary>DbContext مالک را تزریق می‌کند.</summary>
    public ContentDirectory(ContentDbContext db) => _db = db;

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
        return rows.Select(Map).ToList();
    }

    internal static PublishedArticleItem Map(ContentArticle article) => new(
        article.ArticleId,
        article.Slug,
        article.Title,
        article.Excerpt,
        article.CoverMediaAssetId,
        article.PublishDate,
        article.AuthorDisplayName,
        string.IsNullOrWhiteSpace(article.TagsCsv)
            ? []
            : article.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        article.IsFeatured);
}
