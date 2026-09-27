using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Categories.Models;
using Tooba.Catalog.Application.Categories.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>Catalog persistence for Category Admin + Storefront operations.</summary>
public sealed class CategoryDirectory : ICategoryDirectory
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;

    /// <summary>Creates the directory.</summary>
    public CategoryDirectory(CatalogDbContext db, ICatalogUseCaseGuard guard)
    {
        _db = db;
        _guard = guard;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CategoryTreeNodeDto>>> GetTreeAsync(
        string locale,
        string? search,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = CatalogCategorySlugNormalizer.NormalizeLocale(locale);
        var categories = await _db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        if (categories.Count == 0)
        {
            return Result.Success<IReadOnlyList<CategoryTreeNodeDto>>(Array.Empty<CategoryTreeNodeDto>());
        }

        var translations = await _db.CategoryTranslations.AsNoTracking().ToListAsync(cancellationToken);
        var preferred = translations
            .GroupBy(t => t.CategoryId)
            .ToDictionary(
                g => g.Key,
                g => g.FirstOrDefault(t => t.Locale == normalizedLocale)
                    ?? g.OrderByDescending(t => t.Locale == "fa-IR")
                        .ThenByDescending(t => t.Locale.StartsWith("fa"))
                        .ThenBy(t => t.Locale)
                        .First());

        var productCounts = await _db.ProductCategories.AsNoTracking()
            .GroupBy(x => x.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, cancellationToken);

        var childCounts = categories
            .Where(c => c.ParentCategoryId is not null)
            .GroupBy(c => c.ParentCategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        IEnumerable<CatalogCategory> filtered = categories;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim();
            var matchingIds = preferred
                .Where(p =>
                    p.Value.Name.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    || p.Value.Slug.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    || p.Key.ToString("D").Contains(needle, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Key)
                .ToHashSet();

            var parentById = categories.ToDictionary(c => c.CategoryId, c => c.ParentCategoryId);
            var keep = new HashSet<Guid>(matchingIds);
            foreach (var id in matchingIds)
            {
                var current = id;
                while (parentById.TryGetValue(current, out var parent) && parent is Guid p)
                {
                    keep.Add(p);
                    current = p;
                }
            }

            filtered = categories.Where(c => keep.Contains(c.CategoryId));
        }

        var list = filtered
            .OrderBy(c => c.ParentCategoryId.HasValue ? 1 : 0)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.CategoryId)
            .Select(c =>
            {
                preferred.TryGetValue(c.CategoryId, out var t);
                return new CategoryTreeNodeDto(
                    c.CategoryId,
                    c.ParentCategoryId,
                    t?.Name ?? "",
                    t?.Slug ?? "",
                    c.Status,
                    c.SortOrder,
                    c.IsVisible,
                    childCounts.GetValueOrDefault(c.CategoryId) > 0,
                    productCounts.GetValueOrDefault(c.CategoryId));
            })
            .ToList();

        return Result.Success<IReadOnlyList<CategoryTreeNodeDto>>(list);
    }

    /// <inheritdoc />
    public async Task<Result<CategoryWorkspaceSummaryDto>> GetWorkspaceAsync(
        Guid categoryId,
        string? locale,
        CancellationToken cancellationToken)
    {
        var category = await _db.Categories.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure<CategoryWorkspaceSummaryDto>(
                new SemanticError(CatalogErrorCodes.CategoryMissing));
        }

        var translations = await _db.CategoryTranslations.AsNoTracking()
            .Where(x => x.CategoryId == categoryId)
            .OrderBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(locale))
        {
            var normalized = CatalogCategorySlugNormalizer.NormalizeLocale(locale);
            translations = translations.Where(t => t.Locale == normalized).ToList();
        }

        return Result.Success(new CategoryWorkspaceSummaryDto(
            category.CategoryId,
            category.ParentCategoryId,
            category.Status,
            category.SortOrder,
            category.IsVisible,
            category.ImageMediaAssetId,
            category.IconMediaAssetId,
            category.BannerMediaAssetId,
            category.CreatedAt,
            category.UpdatedAt,
            translations.Select(ToTranslationDto).ToList()));
    }

    /// <inheritdoc />
    public async Task<Result<CategoryReference>> CreateAsync(
        CreateCategoryWriteModel model,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);

        CategoryCreateRequest request;
        if (model.Translations is { Count: > 0 })
        {
            request = new CategoryCreateRequest(
                model.ParentCategoryId,
                model.SortOrder,
                model.IsVisible,
                model.ImageMediaAssetId,
                model.IconMediaAssetId,
                model.BannerMediaAssetId,
                model.Translations);
        }
        else
        {
            var localizedNames = model.LocalizedNames ?? new Dictionary<string, string>();
            var maxSibling = await _db.Categories
                .Where(x => x.ParentCategoryId == model.ParentCategoryId)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync(cancellationToken);
            var sortOrder = (maxSibling ?? -1) + 1;
            var translations = new List<CategoryTranslationUpsertRequest>();
            foreach (var pair in localizedNames.Where(p => !string.IsNullOrWhiteSpace(p.Value)))
            {
                var slugResult = TryNormalizeSlug(pair.Value);
                if (slugResult.IsFailure)
                {
                    return Result.Failure<CategoryReference>(slugResult.Errors);
                }

                translations.Add(new CategoryTranslationUpsertRequest(
                    pair.Key,
                    pair.Value,
                    slugResult.Value));
            }

            request = new CategoryCreateRequest(
                model.ParentCategoryId,
                sortOrder,
                true,
                null,
                null,
                null,
                translations);
        }

        return await CreateStructuredAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateCoreAsync(
        Guid categoryId,
        CategoryCoreUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var category = await _db.Categories.SingleOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.CategoryMissing));
        }

        var concurrency = EnsureExpectedUpdatedAt(category, request.ExpectedUpdatedAt);
        if (concurrency is not null)
        {
            return concurrency;
        }

        category.SetCoreFields(
            request.Status,
            request.SortOrder,
            request.IsVisible,
            request.ImageMediaAssetId,
            request.IconMediaAssetId,
            request.BannerMediaAssetId,
            request.ClearImage,
            request.ClearIcon,
            request.ClearBanner,
            DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<CategoryTranslationDto>> UpsertTranslationAsync(
        Guid categoryId,
        CategoryTranslationUpsertRequest request,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Categories.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            return Result.Failure<CategoryTranslationDto>(
                new SemanticError(CatalogErrorCodes.CategoryMissing));
        }

        string locale;
        try
        {
            locale = CatalogCategorySlugNormalizer.NormalizeLocale(request.Locale);
        }
        catch (ArgumentException)
        {
            return Result.Failure<CategoryTranslationDto>(
                new SemanticError(CatalogErrorCodes.CategoryInvalid));
        }

        var slugResult = TryNormalizeSlug(request.Slug);
        if (slugResult.IsFailure)
        {
            return Result.Failure<CategoryTranslationDto>(slugResult.Errors);
        }

        var normalizedSlug = slugResult.Value;
        var now = DateTimeOffset.UtcNow;
        var existing = await _db.CategoryTranslations
            .SingleOrDefaultAsync(x => x.CategoryId == categoryId && x.Locale == locale, cancellationToken);

        if (existing is null)
        {
            var available = await EnsureSlugAvailableAsync(locale, normalizedSlug, excludeCategoryId: null, cancellationToken);
            if (available is not null)
            {
                return Result.Failure<CategoryTranslationDto>(available.Errors);
            }

            var created = CatalogCategoryTranslation.Create(
                categoryId,
                locale,
                request.Name,
                normalizedSlug,
                now,
                request.ShortDescription,
                request.Description,
                request.SeoTitle,
                request.SeoDescription,
                request.MetaKeywords);
            _db.CategoryTranslations.Add(created);
            await UpsertLocalizedNameAsync(categoryId, locale, created.Name, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success(ToTranslationDto(created));
        }

        var availableUpdate = await EnsureSlugAvailableAsync(
            locale, normalizedSlug, excludeCategoryId: categoryId, cancellationToken);
        if (availableUpdate is not null)
        {
            return Result.Failure<CategoryTranslationDto>(availableUpdate.Errors);
        }

        var previousSlug = existing.Update(
            request.Name,
            normalizedSlug,
            now,
            request.ShortDescription,
            request.Description,
            request.SeoTitle,
            request.SeoDescription,
            request.MetaKeywords);
        if (previousSlug is not null)
        {
            var collisionWithCurrent = await _db.CategoryTranslations.AsNoTracking().AnyAsync(
                x => x.Locale == locale
                    && x.Slug == previousSlug
                    && x.CategoryId != categoryId,
                cancellationToken);
            if (!collisionWithCurrent)
            {
                _db.CategorySlugHistories.Add(
                    CatalogCategorySlugHistory.Create(categoryId, locale, previousSlug, now));
            }
        }

        await UpsertLocalizedNameAsync(categoryId, locale, existing.Name, cancellationToken);
        var category = await _db.Categories.SingleAsync(x => x.CategoryId == categoryId, cancellationToken);
        category.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(ToTranslationDto(existing));
    }

    /// <inheritdoc />
    public async Task<Result> MoveAsync(
        Guid categoryId,
        Guid? newParentId,
        DateTimeOffset? expectedUpdatedAt,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var category = await _db.Categories.SingleOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.CategoryMissing));
        }

        var concurrency = EnsureExpectedUpdatedAt(category, expectedUpdatedAt);
        if (concurrency is not null)
        {
            return concurrency;
        }

        var parentMap = await _db.Categories.AsNoTracking()
            .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
        var moveCheck = ValidateMoveOrFail(categoryId, newParentId, parentMap);
        if (moveCheck is not null)
        {
            return moveCheck;
        }

        var now = DateTimeOffset.UtcNow;
        category.Move(newParentId, now);

        var maxSibling = await _db.Categories
            .Where(x => x.ParentCategoryId == newParentId && x.CategoryId != categoryId)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken);
        category.SortOrder = (maxSibling ?? -1) + 1;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ReorderAsync(
        Guid? parentId,
        IReadOnlyList<Guid> orderedCategoryIds,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (orderedCategoryIds.Count == 0)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.CategoryReorderInvalid));
        }

        var siblings = await _db.Categories
            .Where(x => x.ParentCategoryId == parentId)
            .ToListAsync(cancellationToken);
        var siblingIds = siblings.Select(x => x.CategoryId).ToHashSet();
        if (orderedCategoryIds.Count != siblingIds.Count
            || orderedCategoryIds.Any(id => !siblingIds.Contains(id))
            || orderedCategoryIds.Distinct().Count() != orderedCategoryIds.Count)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.CategoryReorderInvalid));
        }

        var now = DateTimeOffset.UtcNow;
        var byId = siblings.ToDictionary(x => x.CategoryId);
        for (var i = 0; i < orderedCategoryIds.Count; i++)
        {
            byId[orderedCategoryIds[i]].SetSortOrder(i, now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> PublishAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var category = await _db.Categories.SingleOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.CategoryMissing));
        }

        category.Publish(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ArchiveAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var category = await _db.Categories.SingleOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.CategoryMissing));
        }

        category.Archive(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<CategoryRouteResolveResult>> ResolveRouteAsync(
        string locale,
        string slug,
        bool forStorefront,
        CancellationToken cancellationToken)
    {
        string normalizedLocale;
        try
        {
            normalizedLocale = CatalogCategorySlugNormalizer.NormalizeLocale(locale);
        }
        catch (ArgumentException)
        {
            return Result.Failure<CategoryRouteResolveResult>(
                new SemanticError(CatalogErrorCodes.CategoryRouteInvalid));
        }

        var slugResult = TryNormalizeSlug(slug);
        if (slugResult.IsFailure)
        {
            return Result.Failure<CategoryRouteResolveResult>(
                new SemanticError(CatalogErrorCodes.CategoryRouteInvalid));
        }

        var normalizedSlug = slugResult.Value;

        var current = await _db.CategoryTranslations.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Locale == normalizedLocale && x.Slug == normalizedSlug,
                cancellationToken);
        if (current is not null)
        {
            var category = await _db.Categories.AsNoTracking()
                .SingleOrDefaultAsync(x => x.CategoryId == current.CategoryId, cancellationToken);
            if (category is null || (forStorefront && !IsStorefrontEligible(category)))
            {
                return Result.Failure<CategoryRouteResolveResult>(
                    new SemanticError(CatalogErrorCodes.CategoryRouteMissing));
            }

            return Result.Success(new CategoryRouteResolveResult(
                category.CategoryId,
                normalizedLocale,
                current.Slug,
                IsRedirect: false,
                CanonicalPath: BuildCanonicalPath(normalizedLocale, current.Slug)));
        }

        var history = await _db.CategorySlugHistories.AsNoTracking()
            .Where(x => x.Locale == normalizedLocale && x.OldSlug == normalizedSlug)
            .OrderByDescending(x => x.ChangedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (history is null)
        {
            return Result.Failure<CategoryRouteResolveResult>(
                new SemanticError(CatalogErrorCodes.CategoryRouteMissing));
        }

        var live = await _db.CategoryTranslations.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.CategoryId == history.CategoryId && x.Locale == normalizedLocale,
                cancellationToken);
        if (live is null)
        {
            return Result.Failure<CategoryRouteResolveResult>(
                new SemanticError(CatalogErrorCodes.CategoryRouteMissing));
        }

        var categoryHist = await _db.Categories.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CategoryId == history.CategoryId, cancellationToken);
        if (categoryHist is null || (forStorefront && !IsStorefrontEligible(categoryHist)))
        {
            return Result.Failure<CategoryRouteResolveResult>(
                new SemanticError(CatalogErrorCodes.CategoryRouteMissing));
        }

        return Result.Success(new CategoryRouteResolveResult(
            categoryHist.CategoryId,
            normalizedLocale,
            live.Slug,
            IsRedirect: true,
            CanonicalPath: BuildCanonicalPath(normalizedLocale, live.Slug)));
    }

    private async Task<Result<CategoryReference>> CreateStructuredAsync(
        CategoryCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Translations.Count == 0)
        {
            return Result.Failure<CategoryReference>(
                new SemanticError(CatalogErrorCodes.CategoryInvalid));
        }

        if (request.ParentCategoryId is Guid parent
            && !await _db.Categories.AnyAsync(x => x.CategoryId == parent, cancellationToken))
        {
            return Result.Failure<CategoryReference>(
                new SemanticError(CatalogErrorCodes.CategoryParentMissing));
        }

        if (request.ParentCategoryId is Guid parentForDepth)
        {
            var parentRows = await _db.Categories.AsNoTracking()
                .Select(x => new { x.CategoryId, x.ParentCategoryId })
                .ToListAsync(cancellationToken);
            var parentMap = parentRows.ToDictionary(x => x.CategoryId, x => x.ParentCategoryId);
            if (!CatalogCategoryTreeRules.CanAddChildUnder(parentForDepth, parentMap))
            {
                return Result.Failure<CategoryReference>(
                    new SemanticError(CatalogErrorCodes.CategoryMaxDepth));
            }
        }

        var now = DateTimeOffset.UtcNow;
        var category = CatalogCategory.Create(
            request.ParentCategoryId,
            now,
            request.SortOrder,
            request.IsVisible);
        category.ImageMediaAssetId = request.ImageMediaAssetId;
        category.IconMediaAssetId = request.IconMediaAssetId;
        category.BannerMediaAssetId = request.BannerMediaAssetId;
        _db.Categories.Add(category);

        var nameDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in request.Translations)
        {
            string locale;
            try
            {
                locale = CatalogCategorySlugNormalizer.NormalizeLocale(t.Locale);
            }
            catch (ArgumentException)
            {
                return Result.Failure<CategoryReference>(
                    new SemanticError(CatalogErrorCodes.CategoryInvalid));
            }

            var slugResult = TryNormalizeSlug(t.Slug);
            if (slugResult.IsFailure)
            {
                return Result.Failure<CategoryReference>(slugResult.Errors);
            }

            var available = await EnsureSlugAvailableAsync(
                locale, slugResult.Value, excludeCategoryId: null, cancellationToken);
            if (available is not null)
            {
                return Result.Failure<CategoryReference>(available.Errors);
            }

            var translation = CatalogCategoryTranslation.Create(
                category.CategoryId,
                locale,
                t.Name,
                slugResult.Value,
                now,
                t.ShortDescription,
                t.Description,
                t.SeoTitle,
                t.SeoDescription,
                t.MetaKeywords);
            _db.CategoryTranslations.Add(translation);
            nameDict[translation.Locale] = translation.Name;
        }

        if (nameDict.Count == 0)
        {
            return Result.Failure<CategoryReference>(
                new SemanticError(CatalogErrorCodes.CategoryInvalid));
        }

        foreach (var pair in nameDict)
        {
            _db.LocalizedTexts.Add(
                CatalogLocalizedText.Create(
                    CatalogLocalizedOwnerKind.Category,
                    category.CategoryId,
                    "name",
                    pair.Key,
                    pair.Value));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(new CategoryReference(category.CategoryId, category.ParentCategoryId, category.Status));
    }

    private async Task<Result?> EnsureSlugAvailableAsync(
        string locale,
        string normalizedSlug,
        Guid? excludeCategoryId,
        CancellationToken cancellationToken)
    {
        var conflict = await _db.CategoryTranslations.AsNoTracking().AnyAsync(
            x => x.Locale == locale
                && x.Slug == normalizedSlug
                && (excludeCategoryId == null || x.CategoryId != excludeCategoryId),
            cancellationToken);
        if (conflict)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.CategorySlugDuplicate));
        }

        return null;
    }

    private static Result? ValidateMoveOrFail(
        Guid categoryId,
        Guid? newParentId,
        IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        if (newParentId == Guid.Empty)
        {
            newParentId = null;
        }

        if (newParentId is null)
        {
            if (CatalogCategoryTreeRules.GetSubtreeHeight(categoryId, parentById)
                > CatalogCategoryTreeRules.MaxCategoryDepth)
            {
                return Result.Failure(new SemanticError(CatalogErrorCodes.CategoryMaxDepth));
            }

            return null;
        }

        if (newParentId == categoryId)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.CategorySelfParent));
        }

        if (!parentById.ContainsKey(newParentId.Value))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.CategoryParentMissing));
        }

        if (CatalogCategoryTreeRules.IsDescendant(categoryId, newParentId.Value, parentById))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.CategoryDescendantParent));
        }

        var parentLevel = CatalogCategoryTreeRules.GetCategoryLevel(newParentId.Value, parentById);
        if (parentLevel + CatalogCategoryTreeRules.GetSubtreeHeight(categoryId, parentById)
            > CatalogCategoryTreeRules.MaxCategoryDepth)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.CategoryMaxDepth));
        }

        return null;
    }

    private static Result? EnsureExpectedUpdatedAt(CatalogCategory category, DateTimeOffset? expectedUpdatedAt)
    {
        if (expectedUpdatedAt is { } expected && category.UpdatedAt != expected)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.CategoryConcurrencyConflict));
        }

        return null;
    }

    private static Result<string> TryNormalizeSlug(string slug)
    {
        try
        {
            return Result.Success(CatalogCategorySlugNormalizer.NormalizeSlug(slug));
        }
        catch (ArgumentException)
        {
            return Result.Failure<string>(new SemanticError(CatalogErrorCodes.CategorySlugInvalid));
        }
        catch (InvalidOperationException)
        {
            // Known Domain outcome for empty-after-normalize; classified by call site, not message text.
            return Result.Failure<string>(new SemanticError(CatalogErrorCodes.CategorySlugInvalid));
        }
    }

    private async Task UpsertLocalizedNameAsync(
        Guid categoryId,
        string locale,
        string name,
        CancellationToken cancellationToken)
    {
        var existing = await _db.LocalizedTexts.SingleOrDefaultAsync(
            x => x.OwnerKind == CatalogLocalizedOwnerKind.Category
                && x.OwnerId == categoryId
                && x.FieldKey == "name"
                && x.Locale == locale,
            cancellationToken);
        if (existing is null)
        {
            _db.LocalizedTexts.Add(
                CatalogLocalizedText.Create(CatalogLocalizedOwnerKind.Category, categoryId, "name", locale, name));
        }
        else
        {
            existing.Value = name.Trim();
        }
    }

    private static bool IsStorefrontEligible(CatalogCategory category) =>
        category.Status == CatalogPublicationStatus.Published && category.IsVisible;

    private static string BuildCanonicalPath(string locale, string slug) =>
        $"/{locale}/category/{slug}";

    private static CategoryTranslationDto ToTranslationDto(CatalogCategoryTranslation t) =>
        new(
            t.CategoryId,
            t.Locale,
            t.Name,
            t.Slug,
            t.ShortDescription,
            t.Description,
            t.SeoTitle,
            t.SeoDescription,
            t.MetaKeywords,
            t.UpdatedAt);
}
