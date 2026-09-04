using Microsoft.EntityFrameworkCore;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure.Persistence;

namespace Tooba.Content.Infrastructure;

/// <summary>دایرکتوری دسته‌بندی مقاله — مالک Content.</summary>
public sealed class ContentCategoryDirectory : IContentCategoryDirectory
{
    private readonly ContentDbContext _db;

    /// <summary>DbContext مالک Content را تزریق می‌کند.</summary>
    public ContentCategoryDirectory(ContentDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentCategoryTreeNodeDto>> GetTreeAsync(
        string languageCode,
        string? search,
        CancellationToken cancellationToken)
    {
        var normalizedLanguage = ContentCategory.NormalizeLanguageCode(languageCode);
        var query = _db.Categories.AsNoTracking()
            .Where(x => x.LanguageCode == normalizedLanguage);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Name.Contains(term) || x.Slug.Contains(term));
        }

        var rows = await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        var articleCounts = await _db.Articles.AsNoTracking()
            .Where(x => x.CategoryId != null)
            .GroupBy(x => x.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, cancellationToken);
        var childCounts = rows
            .Where(x => x.ParentCategoryId is not null)
            .GroupBy(x => x.ParentCategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        return rows.Select(row => new ContentCategoryTreeNodeDto(
            row.CategoryId,
            row.LanguageCode,
            row.ParentCategoryId,
            row.Name,
            row.Slug,
            row.Status.ToString(),
            row.SortOrder,
            childCounts.TryGetValue(row.CategoryId, out var children) && children > 0,
            articleCounts.GetValueOrDefault(row.CategoryId))).ToList();
    }

    /// <inheritdoc />
    public async Task<ContentCategoryWorkspaceDto?> GetWorkspaceAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var row = await _db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);
        return row is null ? null : await MapWorkspaceAsync(row, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContentCategoryWorkspaceDto> CreateAsync(
        CreateContentCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var language = ContentCategory.NormalizeLanguageCode(command.LanguageCode);
        if (command.ParentCategoryId is Guid parentId)
        {
            await ValidateParentAsync(Guid.Empty, parentId, language, cancellationToken);
            var maps = await BuildMapsAsync(cancellationToken);
            ContentCategoryTreeRules.ValidateCreateUnderParent(parentId, maps.ParentById);
        }

        var slug = ContentCategory.NormalizeSlug(command.Slug);
        if (await _db.Categories.AnyAsync(
            x => x.LanguageCode == language && x.Slug == slug,
            cancellationToken))
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.SlugDuplicate);
        }

        var category = ContentCategory.Create(
            language,
            command.ParentCategoryId,
            command.Name,
            command.Slug,
            command.ShortDescription,
            command.Description,
            command.SortOrder,
            null,
            null,
            null,
            now);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapWorkspaceAsync(category, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContentCategoryWorkspaceDto> UpdateAsync(
        Guid categoryId,
        UpdateContentCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var category = await FindTrackedAsync(categoryId, cancellationToken);
        var slug = ContentCategory.NormalizeSlug(command.Slug);
        if (await _db.Categories.AnyAsync(
            x => x.CategoryId != categoryId
                && x.LanguageCode == category.LanguageCode
                && x.Slug == slug,
            cancellationToken))
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.SlugDuplicate);
        }

        var status = Enum.TryParse<ContentCategoryStatus>(command.Status, true, out var parsed)
            ? parsed
            : ContentCategoryStatus.Active;
        category.UpdateCore(
            command.Name,
            command.Slug,
            command.ShortDescription,
            command.Description,
            command.SortOrder,
            status,
            DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapWorkspaceAsync(category, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContentCategoryWorkspaceDto> UpdateSeoAsync(
        Guid categoryId,
        UpdateContentCategorySeoCommand command,
        CancellationToken cancellationToken)
    {
        var category = await FindTrackedAsync(categoryId, cancellationToken);
        category.UpdateSeo(command.SeoTitle, command.SeoDescription, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapWorkspaceAsync(category, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContentCategoryWorkspaceDto> UpdateMediaAsync(
        Guid categoryId,
        UpdateContentCategoryMediaCommand command,
        CancellationToken cancellationToken)
    {
        var category = await FindTrackedAsync(categoryId, cancellationToken);
        category.SetImage(command.ImageMediaAssetId, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapWorkspaceAsync(category, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContentCategoryWorkspaceDto> MoveAsync(
        Guid categoryId,
        MoveContentCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var category = await FindTrackedAsync(categoryId, cancellationToken);
        if (command.NewParentId is Guid parentId)
        {
            await ValidateParentAsync(categoryId, parentId, category.LanguageCode, cancellationToken);
        }

        var maps = await BuildMapsAsync(cancellationToken);
        ContentCategoryTreeRules.ValidateMove(categoryId, command.NewParentId, maps.ParentById, maps.LanguageById);
        category.SetParent(command.NewParentId, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapWorkspaceAsync(category, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReorderAsync(IReadOnlyList<ReorderContentCategoryItem> items, CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            var category = await FindTrackedAsync(item.CategoryId, cancellationToken);
            category.UpdateCore(
                category.Name,
                category.Slug,
                category.ShortDescription,
                category.Description,
                item.SortOrder,
                category.Status,
                DateTimeOffset.UtcNow);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ArchiveAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await FindTrackedAsync(categoryId, cancellationToken);
        if (await _db.Categories.AnyAsync(x => x.ParentCategoryId == categoryId, cancellationToken))
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.HasChildren);
        }

        if (await _db.Articles.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.HasArticles);
        }

        category.Archive(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PublishedContentCategoryItem?> GetPublicBySlugAsync(
        string languageCode,
        string slug,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(languageCode) || string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var language = ContentCategory.NormalizeLanguageCode(languageCode);
        var normalizedSlug = ContentCategory.NormalizeSlug(slug);
        var row = await _db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.LanguageCode == language
                    && x.Slug == normalizedSlug
                    && x.Status == ContentCategoryStatus.Active,
                cancellationToken);
        return row is null ? null : MapPublic(row);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedContentCategoryItem>> ListPublicAsync(
        string languageCode,
        CancellationToken cancellationToken)
    {
        var language = ContentCategory.NormalizeLanguageCode(languageCode);
        var rows = await _db.Categories.AsNoTracking()
            .Where(x => x.LanguageCode == language && x.Status == ContentCategoryStatus.Active)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
        return rows.Select(MapPublic).ToList();
    }

    /// <inheritdoc />
    public async Task EnsureArticleCategoryLanguageMatchAsync(
        string articleLocale,
        Guid? categoryId,
        CancellationToken cancellationToken,
        bool isNewAssignment = true)
    {
        if (categoryId is null)
        {
            return;
        }

        var category = await _db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken)
            ?? throw new InvalidOperationException(ContentCategoryErrorCodes.NotFound);
        if (!string.Equals(category.LanguageCode, articleLocale.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.LanguageMismatch);
        }

        if (isNewAssignment && category.Status != ContentCategoryStatus.Active)
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.Inactive);
        }
    }

    private async Task ValidateParentAsync(
        Guid categoryId,
        Guid parentId,
        string languageCode,
        CancellationToken cancellationToken)
    {
        var parent = await _db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CategoryId == parentId, cancellationToken)
            ?? throw new InvalidOperationException(ContentCategoryErrorCodes.NotFound);
        if (!string.Equals(parent.LanguageCode, languageCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(ContentCategoryErrorCodes.CrossLanguageParent);
        }

        if (categoryId != Guid.Empty)
        {
            var maps = await BuildMapsAsync(cancellationToken);
            ContentCategoryTreeRules.ValidateMove(categoryId, parentId, maps.ParentById, maps.LanguageById);
        }
    }

    private async Task<ContentCategory> FindTrackedAsync(Guid categoryId, CancellationToken cancellationToken) =>
        await _db.Categories.FirstOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken)
        ?? throw new InvalidOperationException(ContentCategoryErrorCodes.NotFound);

    private async Task<(Dictionary<Guid, Guid?> ParentById, Dictionary<Guid, string> LanguageById)> BuildMapsAsync(
        CancellationToken cancellationToken)
    {
        var rows = await _db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        return (
            rows.ToDictionary(x => x.CategoryId, x => x.ParentCategoryId),
            rows.ToDictionary(x => x.CategoryId, x => x.LanguageCode));
    }

    private async Task<ContentCategoryWorkspaceDto> MapWorkspaceAsync(
        ContentCategory row,
        CancellationToken cancellationToken)
    {
        var articleCount = await _db.Articles.AsNoTracking()
            .CountAsync(x => x.CategoryId == row.CategoryId, cancellationToken);
        return new ContentCategoryWorkspaceDto(
            row.CategoryId,
            row.LanguageCode,
            row.ParentCategoryId,
            row.Name,
            row.Slug,
            row.ShortDescription,
            row.Description,
            row.Status.ToString(),
            row.SortOrder,
            row.SeoTitle,
            row.SeoDescription,
            row.ImageMediaAssetId,
            articleCount,
            row.CreatedAt,
            row.UpdatedAt);
    }

    private static PublishedContentCategoryItem MapPublic(ContentCategory row) => new(
        row.CategoryId,
        row.LanguageCode,
        row.Name,
        row.Slug,
        row.ShortDescription,
        row.Description,
        row.SeoTitle,
        row.SeoDescription,
        row.ImageMediaAssetId,
        ContentTaxonomySeoRules.BuildCategoryPublicPath(row.LanguageCode, row.Slug));
}
