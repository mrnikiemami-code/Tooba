using Microsoft.EntityFrameworkCore;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure.Persistence;

namespace Tooba.Content.Infrastructure;

/// <summary>ارجاع‌های رسانهٔ مقاله — فقط MediaAssetId، بدون باینری.</summary>
public sealed class ContentArticleMediaDirectory : IContentArticleMediaDirectory
{
    private readonly ContentDbContext _db;
    private readonly IContentMediaAssetValidator _media;

    /// <summary>DbContext و اعتبارسنج DAM را تزریق می‌کند.</summary>
    public ContentArticleMediaDirectory(ContentDbContext db, IContentMediaAssetValidator media)
    {
        _db = db;
        _media = media;
    }

    /// <inheritdoc />
    public async Task<ArticleMediaWorkspaceDto> GetWorkspaceAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var article = await RequireArticleAsync(articleId, cancellationToken);
        var gallery = await LoadGalleryAsync(articleId, cancellationToken);
        return MapWorkspace(article, gallery);
    }

    /// <inheritdoc />
    public async Task<ArticleMediaWorkspaceDto> AssignFeaturedAsync(
        Guid articleId,
        Guid? mediaAssetId,
        CancellationToken cancellationToken)
    {
        var article = await RequireArticleTrackedAsync(articleId, cancellationToken);
        if (mediaAssetId is not null)
            await EnsureMediaExistsAsync(mediaAssetId.Value, cancellationToken);
        article.AssignFeaturedImage(mediaAssetId, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetWorkspaceAsync(articleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ArticleMediaWorkspaceDto> AssignSeoImageAsync(
        Guid articleId,
        Guid? mediaAssetId,
        CancellationToken cancellationToken)
    {
        var article = await RequireArticleTrackedAsync(articleId, cancellationToken);
        if (mediaAssetId is not null)
            await EnsureMediaExistsAsync(mediaAssetId.Value, cancellationToken);
        article.AssignSeoImage(mediaAssetId, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetWorkspaceAsync(articleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ArticleMediaWorkspaceDto> AddGalleryItemsAsync(
        Guid articleId,
        IReadOnlyList<Guid> mediaAssetIds,
        CancellationToken cancellationToken)
    {
        if (mediaAssetIds.Count == 0)
            return await GetWorkspaceAsync(articleId, cancellationToken);

        _ = await RequireArticleTrackedAsync(articleId, cancellationToken);
        var existing = await _db.ArticleMedia.AsNoTracking()
            .Where(row => row.ArticleId == articleId)
            .ToListAsync(cancellationToken);
        var existingIds = existing.Select(row => row.MediaAssetId).ToHashSet();
        var nextOrder = existing.Count == 0 ? 0 : existing.Max(row => row.DisplayOrder) + 1;

        foreach (var mediaAssetId in mediaAssetIds.Distinct())
        {
            if (existingIds.Contains(mediaAssetId)) continue;
            await EnsureMediaExistsAsync(mediaAssetId, cancellationToken);
            _db.ArticleMedia.Add(ContentArticleMediaItem.Create(articleId, mediaAssetId, nextOrder++, null, null));
        }

        await TouchArticleAsync(articleId, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetWorkspaceAsync(articleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ArticleMediaWorkspaceDto> RemoveGalleryItemAsync(
        Guid articleId,
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        var row = await _db.ArticleMedia.FirstOrDefaultAsync(
            item => item.ArticleId == articleId && item.MediaAssetId == mediaAssetId,
            cancellationToken);
        if (row is not null)
        {
            _db.ArticleMedia.Remove(row);
            await TouchArticleAsync(articleId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await NormalizeGalleryOrderAsync(articleId, cancellationToken);
        }

        return await GetWorkspaceAsync(articleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ArticleMediaWorkspaceDto> ReorderGalleryAsync(
        Guid articleId,
        IReadOnlyList<Guid> orderedMediaAssetIds,
        CancellationToken cancellationToken)
    {
        var rows = await _db.ArticleMedia
            .Where(item => item.ArticleId == articleId)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return await GetWorkspaceAsync(articleId, cancellationToken);

        var byId = rows.ToDictionary(row => row.MediaAssetId);
        var order = 0;
        foreach (var mediaAssetId in orderedMediaAssetIds)
        {
            if (!byId.TryGetValue(mediaAssetId, out var row)) continue;
            row.UpdateMetadata(row.AltText, row.Caption, order++);
        }

        foreach (var row in rows.Where(row => !orderedMediaAssetIds.Contains(row.MediaAssetId)).OrderBy(row => row.DisplayOrder))
        {
            row.UpdateMetadata(row.AltText, row.Caption, order++);
        }

        await TouchArticleAsync(articleId, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetWorkspaceAsync(articleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ArticleMediaWorkspaceDto> PatchGalleryItemAsync(
        Guid articleId,
        Guid mediaAssetId,
        string? altText,
        string? caption,
        CancellationToken cancellationToken)
    {
        var row = await _db.ArticleMedia.FirstOrDefaultAsync(
            item => item.ArticleId == articleId && item.MediaAssetId == mediaAssetId,
            cancellationToken)
            ?? throw new InvalidOperationException("ردیف گالری مقاله یافت نشد.");
        row.UpdateMetadata(altText, caption, row.DisplayOrder);
        await TouchArticleAsync(articleId, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetWorkspaceAsync(articleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountStructuredReferencesAsync(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        var featured = await _db.Articles.AsNoTracking().CountAsync(
            article => article.CoverMediaAssetId == mediaAssetId,
            cancellationToken);
        var seo = await _db.Articles.AsNoTracking().CountAsync(
            article => article.SeoImageMediaAssetId == mediaAssetId,
            cancellationToken);
        var gallery = await _db.ArticleMedia.AsNoTracking().CountAsync(
            item => item.MediaAssetId == mediaAssetId,
            cancellationToken);
        return featured + seo + gallery;
    }

    private async Task NormalizeGalleryOrderAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var rows = await _db.ArticleMedia
            .Where(item => item.ArticleId == articleId)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.MediaAssetId)
            .ToListAsync(cancellationToken);
        for (var index = 0; index < rows.Count; index++)
        {
            rows[index].UpdateMetadata(rows[index].AltText, rows[index].Caption, index);
        }

        if (rows.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<ContentArticleMediaItem>> LoadGalleryAsync(Guid articleId, CancellationToken cancellationToken) =>
        await _db.ArticleMedia.AsNoTracking()
            .Where(item => item.ArticleId == articleId)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.MediaAssetId)
            .ToListAsync(cancellationToken);

    private static ArticleMediaWorkspaceDto MapWorkspace(ContentArticle article, IReadOnlyList<ContentArticleMediaItem> gallery) =>
        new(
            article.ArticleId,
            article.CoverMediaAssetId,
            article.SeoImageMediaAssetId,
            article.ResolveEffectiveSeoImageId(),
            gallery.Select(item => new ArticleGalleryItemDto(
                item.MediaAssetId,
                item.DisplayOrder,
                item.AltText,
                item.Caption)).ToList());

    private async Task<ContentArticle> RequireArticleAsync(Guid articleId, CancellationToken cancellationToken) =>
        await _db.Articles.AsNoTracking().FirstOrDefaultAsync(row => row.ArticleId == articleId, cancellationToken)
        ?? throw new InvalidOperationException("مقاله یافت نشد.");

    private async Task<ContentArticle> RequireArticleTrackedAsync(Guid articleId, CancellationToken cancellationToken) =>
        await _db.Articles.FirstOrDefaultAsync(row => row.ArticleId == articleId, cancellationToken)
        ?? throw new InvalidOperationException("مقاله یافت نشد.");

    private async Task TouchArticleAsync(Guid articleId, CancellationToken cancellationToken)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(row => row.ArticleId == articleId, cancellationToken);
        if (article is not null)
        {
            article.Touch(DateTimeOffset.UtcNow);
        }
    }

    private async Task EnsureMediaExistsAsync(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        try
        {
            await _media.EnsureReadyAssetExistsAsync(mediaAssetId, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException(ContentArticleErrorCodes.MediaNotFound);
        }
    }
}
