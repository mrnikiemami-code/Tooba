using Microsoft.EntityFrameworkCore;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure.Persistence;

namespace Tooba.Content.Infrastructure;

/// <summary>دایرکتوری نویسندهٔ مقاله — مالک Content.</summary>
public sealed class ContentAuthorDirectory : IContentAuthorDirectory
{
    private readonly ContentDbContext _db;

    /// <summary>DbContext مالک Content را تزریق می‌کند.</summary>
    public ContentAuthorDirectory(ContentDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<PublishedContentAuthorItem?> GetPublicBySlugAsync(
        string slug,
        string routeLocale,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var normalizedSlug = ContentAuthor.NormalizeSlug(slug);
        var row = await _db.Authors.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == normalizedSlug && x.IsActive, cancellationToken);
        return row is null ? null : MapPublic(row, routeLocale);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedContentAuthorItem>> ListPublicAsync(
        string routeLocale,
        CancellationToken cancellationToken)
    {
        var rows = await _db.Authors.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);
        return rows.Select(row => MapPublic(row, routeLocale)).ToList();
    }

    /// <inheritdoc />
    public async Task<ContentAuthorWorkspaceDto?> GetWorkspaceAsync(
        Guid authorId,
        CancellationToken cancellationToken)
    {
        var row = await _db.Authors.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AuthorId == authorId, cancellationToken);
        return row is null ? null : await MapWorkspaceAsync(row, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContentAuthorWorkspaceDto> CreateAsync(
        CreateContentAuthorCommand command,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var slug = ContentAuthor.NormalizeSlug(command.Slug);
        if (await _db.Authors.AnyAsync(x => x.Slug == slug, cancellationToken))
        {
            throw new InvalidOperationException(ContentAuthorErrorCodes.SlugDuplicate);
        }

        var author = ContentAuthor.Create(
            command.DisplayName,
            command.Slug,
            command.ShortBio,
            command.FullBio,
            command.ProfileImageMediaAssetId,
            command.CoverImageMediaAssetId,
            command.WebsiteUrl,
            command.InstagramUrl,
            command.TwitterUrl,
            command.LinkedInUrl,
            now);
        _db.Authors.Add(author);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapWorkspaceAsync(author, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContentAuthorWorkspaceDto> UpdateAsync(
        Guid authorId,
        UpdateContentAuthorCommand command,
        CancellationToken cancellationToken)
    {
        var author = await FindTrackedAsync(authorId, cancellationToken);
        var slug = ContentAuthor.NormalizeSlug(command.Slug);
        if (await _db.Authors.AnyAsync(
            x => x.AuthorId != authorId && x.Slug == slug,
            cancellationToken))
        {
            throw new InvalidOperationException(ContentAuthorErrorCodes.SlugDuplicate);
        }

        author.Update(
            command.DisplayName,
            command.Slug,
            command.ShortBio,
            command.FullBio,
            command.ProfileImageMediaAssetId,
            command.CoverImageMediaAssetId,
            command.WebsiteUrl,
            command.InstagramUrl,
            command.TwitterUrl,
            command.LinkedInUrl,
            DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapWorkspaceAsync(author, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeactivateAsync(Guid authorId, CancellationToken cancellationToken)
    {
        var author = await FindTrackedAsync(authorId, cancellationToken);
        author.Deactivate(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentAuthorPickerItemDto>> GetPickerListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var query = _db.Authors.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.DisplayName.Contains(term) || x.Slug.Contains(term));
        }

        return await query
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.DisplayName)
            .Select(x => new ContentAuthorPickerItemDto(x.AuthorId, x.DisplayName, x.Slug, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task EnsureArticleAuthorAssignmentAsync(
        Guid? authorId,
        bool isNewAssignment,
        CancellationToken cancellationToken)
    {
        // Draft-first: ایجاد/ذخیره بدون نویسنده مجاز است.
        if (authorId is null)
        {
            return;
        }

        var author = await _db.Authors.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AuthorId == authorId, cancellationToken)
            ?? throw new InvalidOperationException(ContentAuthorErrorCodes.NotFound);
        if (isNewAssignment && !author.IsActive)
        {
            throw new InvalidOperationException(ContentAuthorErrorCodes.Inactive);
        }
    }

    /// <inheritdoc />
    public async Task EnsurePublishableAuthorAsync(Guid? authorId, CancellationToken cancellationToken)
    {
        if (authorId is null)
        {
            throw new InvalidOperationException(ContentAuthorErrorCodes.RequiredForPublish);
        }

        _ = await _db.Authors.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AuthorId == authorId, cancellationToken)
            ?? throw new InvalidOperationException(ContentAuthorErrorCodes.NotFound);
    }

    private async Task<ContentAuthor> FindTrackedAsync(Guid authorId, CancellationToken cancellationToken) =>
        await _db.Authors.FirstOrDefaultAsync(x => x.AuthorId == authorId, cancellationToken)
        ?? throw new InvalidOperationException(ContentAuthorErrorCodes.NotFound);

    private async Task<ContentAuthorWorkspaceDto> MapWorkspaceAsync(
        ContentAuthor row,
        CancellationToken cancellationToken)
    {
        var articleCount = await _db.Articles.AsNoTracking()
            .CountAsync(x => x.AuthorId == row.AuthorId, cancellationToken);
        return new ContentAuthorWorkspaceDto(
            row.AuthorId,
            row.DisplayName,
            row.Slug,
            row.IsActive,
            row.ProfileImageMediaAssetId,
            row.CoverImageMediaAssetId,
            row.ShortBio,
            row.FullBio,
            row.WebsiteUrl,
            row.InstagramUrl,
            row.TwitterUrl,
            row.LinkedInUrl,
            articleCount,
            row.CreatedAt,
            row.UpdatedAt);
    }

    private static PublishedContentAuthorItem MapPublic(ContentAuthor row, string routeLocale) => new(
        row.AuthorId,
        row.DisplayName,
        row.Slug,
        row.ShortBio,
        row.FullBio,
        row.ProfileImageMediaAssetId,
        row.CoverImageMediaAssetId,
        ContentTaxonomySeoRules.BuildAuthorPublicPath(routeLocale, row.Slug));
}
