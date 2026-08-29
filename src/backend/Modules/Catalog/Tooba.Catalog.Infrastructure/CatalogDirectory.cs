using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>
/// نگهبان موقتی موردکاربرد. ماتریس SpiceDB/نقش روی موجودیت Catalog نوشته نمی‌شود.
/// </summary>
public sealed class OpenCatalogUseCaseGuard : ICatalogUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// پیاده‌سازی نوشتن/خواندن Catalog روی schema همین ماژول. Host و Search را parse/ایندکس نمی‌کند.
/// </summary>
public sealed class CatalogDirectory : ICatalogDirectory, ICatalogLookupGateway
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;

    /// <summary>
    /// دایرکتوری را به DbContext Tenant-aware وصل می‌کند.
    /// </summary>
    public CatalogDirectory(CatalogDbContext db, ICatalogUseCaseGuard guard)
    {
        _db = db;
        _guard = guard;
    }

    /// <inheritdoc />
    public async Task<ProductReference?> FindProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await _db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        return product is null ? null : new ProductReference(product.ProductId, product.Kind, product.Status);
    }

    /// <inheritdoc />
    public async Task<VariantReference?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken)
    {
        var variant = await _db.Variants.AsNoTracking().SingleOrDefaultAsync(x => x.VariantId == variantId, cancellationToken);
        return variant is null
            ? null
            : new VariantReference(variant.VariantId, variant.ProductId, variant.CombinationFingerprint, variant.Status);
    }

    /// <inheritdoc />
    public async Task<CategoryReference?> FindCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await _db.Categories.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);
        return category is null
            ? null
            : new CategoryReference(category.CategoryId, category.ParentCategoryId, category.Status);
    }

    /// <inheritdoc />
    public async Task<ReviewableProductReference?> FindReviewableProductBySlugAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var normalized = slug.Trim().ToLowerInvariant();
        var productId = await _db.Products.AsNoTracking()
            .Where(x => x.SlugSeam == normalized)
            .Select(x => (Guid?)x.ProductId)
            .SingleOrDefaultAsync(cancellationToken);
        return productId is null ? null : await FindReviewableProductByIdAsync(productId.Value, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReviewableProductReference?> FindReviewableProductByIdAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => new { x.ProductId, x.SlugSeam, x.Status, VariantIds = x.Variants.Select(v => v.VariantId).ToArray() })
            .SingleOrDefaultAsync(cancellationToken);
        if (product is null) return null;
        var titles = await GetProductTitlesAsync([productId], cancellationToken);
        var slug = product.SlugSeam ?? product.ProductId.ToString("N");
        return new ReviewableProductReference(product.ProductId, slug, titles.GetValueOrDefault(productId) ?? slug, product.Status, product.VariantIds);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> GetProductTitlesAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0) return new Dictionary<Guid, string>();
        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                && x.FieldKey == "name" && productIds.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == "fa-IR")
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        return rows.GroupBy(x => x.OwnerId).ToDictionary(x => x.Key, x => x.First().Value);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> GetCategoryNamesAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken)
    {
        if (categoryIds.Count == 0) return new Dictionary<Guid, string>();
        var ids = categoryIds.Distinct().ToArray();
        var translations = await _db.CategoryTranslations.AsNoTracking()
            .Where(x => ids.Contains(x.CategoryId))
            .OrderByDescending(x => x.Locale == "fa-IR")
            .ThenByDescending(x => x.Locale.StartsWith("fa"))
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        var fromTranslations = translations
            .GroupBy(x => x.CategoryId)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var missing = ids.Where(id => !fromTranslations.ContainsKey(id)).ToArray();
        if (missing.Length == 0)
        {
            return fromTranslations;
        }

        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Category
                && x.FieldKey == "name" && missing.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == "fa-IR")
            .ThenByDescending(x => x.Locale.StartsWith("fa"))
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        foreach (var group in rows.GroupBy(x => x.OwnerId))
        {
            fromTranslations[group.Key] = group.First().Value;
        }

        return fromTranslations;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0) return new Dictionary<Guid, Guid?>();
        var distinct = variantIds.Distinct().ToArray();
        var variantRows = await _db.Variants.AsNoTracking()
            .Where(v => distinct.Contains(v.VariantId))
            .Select(v => new { v.VariantId, v.ProductId })
            .ToListAsync(cancellationToken);
        if (variantRows.Count == 0) return distinct.ToDictionary(id => id, _ => (Guid?)null);

        var productIds = variantRows.Select(v => v.ProductId).Distinct().ToArray();
        var categoryLinks = await _db.ProductCategories.AsNoTracking()
            .Where(pc => productIds.Contains(pc.ProductId))
            .Select(pc => new { pc.ProductId, pc.CategoryId, pc.AssignmentId })
            .ToListAsync(cancellationToken);
        var primaryByProduct = categoryLinks
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => (Guid?)g.OrderBy(x => x.AssignmentId).First().CategoryId);

        var result = new Dictionary<Guid, Guid?>();
        foreach (var id in distinct)
        {
            result[id] = null;
        }

        foreach (var row in variantRows)
        {
            result[row.VariantId] = primaryByProduct.GetValueOrDefault(row.ProductId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccessControlCategoryItem>> ListCategoriesForAccessControlAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var categories = await _db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        if (categories.Count == 0) return [];
        var names = await GetCategoryNamesAsync(categories.Select(c => c.CategoryId).ToArray(), cancellationToken);
        IEnumerable<AccessControlCategoryItem> items = categories.Select(c =>
            new AccessControlCategoryItem(
                c.CategoryId,
                c.ParentCategoryId,
                names.GetValueOrDefault(c.CategoryId) ?? "رده",
                c.Status.ToString()));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim();
            items = items.Where(i =>
                i.Name.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || i.CategoryId.ToString("D").Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        return items.OrderBy(i => i.Name, StringComparer.Ordinal).Take(200).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccessControlBrandItem>> ListBrandsForAccessControlAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var brands = await _db.Brands.AsNoTracking().ToListAsync(cancellationToken);
        if (brands.Count == 0) return [];
        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Brand
                && x.FieldKey == "name"
                && brands.Select(b => b.BrandId).Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == "fa-IR")
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        var names = rows.GroupBy(x => x.OwnerId).ToDictionary(g => g.Key, g => g.First().Value);
        IEnumerable<AccessControlBrandItem> items = brands.Select(b =>
            new AccessControlBrandItem(b.BrandId, names.GetValueOrDefault(b.BrandId) ?? b.SlugSeam ?? "برند", b.Status.ToString()));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim();
            items = items.Where(i => i.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        return items.OrderBy(i => i.Name, StringComparer.Ordinal).Take(200).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccessControlProductItem>> ListProductsForAccessControlAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var products = await _db.Products.AsNoTracking()
            .Where(p => p.Status == CatalogPublicationStatus.Published)
            .Take(500)
            .ToListAsync(cancellationToken);
        if (products.Count == 0) return [];
        var titles = await GetProductTitlesAsync(products.Select(p => p.ProductId).ToArray(), cancellationToken);
        IEnumerable<AccessControlProductItem> items = products.Select(p =>
            new AccessControlProductItem(
                p.ProductId,
                titles.GetValueOrDefault(p.ProductId) ?? p.SlugSeam ?? p.ProductId.ToString("N"),
                p.Status.ToString()));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim();
            items = items.Where(i => i.Title.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        return items.OrderBy(i => i.Title, StringComparer.Ordinal).Take(200).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, ReviewableProductReference>> GetReviewableProductsByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0) return new Dictionary<Guid, ReviewableProductReference>();
        var requested = productIds.Distinct().ToArray();
        var products = await _db.Products.AsNoTracking()
            .Where(product => requested.Contains(product.ProductId) && product.Status == CatalogPublicationStatus.Published)
            .ToListAsync(cancellationToken);
        if (products.Count == 0) return new Dictionary<Guid, ReviewableProductReference>();
        var titles = await GetProductTitlesAsync(products.Select(product => product.ProductId).ToArray(), cancellationToken);
        var variantRows = await _db.Variants.AsNoTracking()
            .Where(variant => products.Select(product => product.ProductId).Contains(variant.ProductId))
            .GroupBy(variant => variant.ProductId)
            .Select(group => new { ProductId = group.Key, VariantIds = group.Select(variant => variant.VariantId).ToList() })
            .ToListAsync(cancellationToken);
        var variants = variantRows.ToDictionary(row => row.ProductId, row => (IReadOnlyList<Guid>)row.VariantIds);
        return products.ToDictionary(
            product => product.ProductId,
            product =>
            {
                var slug = string.IsNullOrWhiteSpace(product.SlugSeam) ? product.ProductId.ToString("N") : product.SlugSeam;
                return new ReviewableProductReference(
                    product.ProductId,
                    slug,
                    titles.GetValueOrDefault(product.ProductId) ?? slug,
                    product.Status,
                    variants.GetValueOrDefault(product.ProductId) ?? []);
            });
    }

    /// <inheritdoc />
    public async Task<CategoryReference> CreateCategoryAsync(
        Guid? parentCategoryId,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken)
    {
        var maxSibling = await _db.Categories
            .Where(x => x.ParentCategoryId == parentCategoryId)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken);
        var sortOrder = (maxSibling ?? -1) + 1;
        var translations = localizedNames
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => new CategoryTranslationUpsertRequest(
                p.Key,
                p.Value,
                CatalogCategorySlugNormalizer.SlugifyFromName(p.Value)))
            .ToList();
        return await CreateCategoryAsync(
            new CategoryCreateRequest(parentCategoryId, sortOrder, true, null, null, translations),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CategoryReference> CreateCategoryAsync(
        CategoryCreateRequest request,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (request.Translations.Count == 0)
        {
            throw new InvalidOperationException("حداقل یک ترجمهٔ محلی برای رده لازم است.");
        }

        if (request.ParentCategoryId is Guid parent
            && !await _db.Categories.AnyAsync(x => x.CategoryId == parent, cancellationToken))
        {
            throw new InvalidOperationException("ردهٔ والد در Catalog این Tenant وجود ندارد.");
        }

        var now = DateTimeOffset.UtcNow;
        var category = CatalogCategory.Create(
            request.ParentCategoryId,
            now,
            request.SortOrder,
            request.IsVisible);
        category.ImageMediaAssetId = request.ImageMediaAssetId;
        category.IconMediaAssetId = request.IconMediaAssetId;
        _db.Categories.Add(category);

        var nameDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in request.Translations)
        {
            await EnsureSlugAvailableAsync(t.Locale, t.Slug, excludeCategoryId: null, cancellationToken);
            var translation = CatalogCategoryTranslation.Create(
                category.CategoryId,
                t.Locale,
                t.Name,
                t.Slug,
                now,
                t.ShortDescription,
                t.Description,
                t.SeoTitle,
                t.SeoDescription,
                t.MetaKeywords);
            _db.CategoryTranslations.Add(translation);
            nameDict[translation.Locale] = translation.Name;
        }

        AddLocalizedNames(CatalogLocalizedOwnerKind.Category, category.CategoryId, nameDict);
        await _db.SaveChangesAsync(cancellationToken);
        return new CategoryReference(category.CategoryId, category.ParentCategoryId, category.Status);
    }

    /// <inheritdoc />
    public async Task UpdateCategoryCoreAsync(
        Guid categoryId,
        CategoryCoreUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var category = await _db.Categories.SingleAsync(x => x.CategoryId == categoryId, cancellationToken);
        EnsureExpectedUpdatedAt(category, request.ExpectedUpdatedAt);
        category.SetCoreFields(
            request.Status,
            request.SortOrder,
            request.IsVisible,
            request.ImageMediaAssetId,
            request.IconMediaAssetId,
            request.ClearImage,
            request.ClearIcon,
            DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CategoryTranslationDto> UpsertCategoryTranslationAsync(
        Guid categoryId,
        CategoryTranslationUpsertRequest request,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Categories.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            throw new InvalidOperationException("رده در Catalog این Tenant وجود ندارد.");
        }

        var locale = CatalogCategorySlugNormalizer.NormalizeLocale(request.Locale);
        var now = DateTimeOffset.UtcNow;
        var existing = await _db.CategoryTranslations
            .SingleOrDefaultAsync(x => x.CategoryId == categoryId && x.Locale == locale, cancellationToken);

        if (existing is null)
        {
            await EnsureSlugAvailableAsync(locale, request.Slug, excludeCategoryId: null, cancellationToken);
            var created = CatalogCategoryTranslation.Create(
                categoryId,
                locale,
                request.Name,
                request.Slug,
                now,
                request.ShortDescription,
                request.Description,
                request.SeoTitle,
                request.SeoDescription,
                request.MetaKeywords);
            _db.CategoryTranslations.Add(created);
            await UpsertLocalizedNameAsync(categoryId, locale, created.Name, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return ToTranslationDto(created);
        }

        await EnsureSlugAvailableAsync(locale, request.Slug, excludeCategoryId: categoryId, cancellationToken);
        var previousSlug = existing.Update(
            request.Name,
            request.Slug,
            now,
            request.ShortDescription,
            request.Description,
            request.SeoTitle,
            request.SeoDescription,
            request.MetaKeywords);
        if (previousSlug is not null)
        {
            // اگر old slug الان slug جاری رده دیگری است، history ننویس تا resolve همیشه current را ببرد.
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
        return ToTranslationDto(existing);
    }

    /// <inheritdoc />
    public async Task MoveCategoryAsync(
        Guid categoryId,
        Guid? newParentId,
        DateTimeOffset? expectedUpdatedAt,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var category = await _db.Categories.SingleAsync(x => x.CategoryId == categoryId, cancellationToken);
        EnsureExpectedUpdatedAt(category, expectedUpdatedAt);

        var parentMap = await _db.Categories.AsNoTracking()
            .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
        CatalogCategoryTreeRules.ValidateMove(categoryId, newParentId, parentMap);

        var now = DateTimeOffset.UtcNow;
        category.Move(newParentId, now);

        var maxSibling = await _db.Categories
            .Where(x => x.ParentCategoryId == newParentId && x.CategoryId != categoryId)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken);
        category.SortOrder = (maxSibling ?? -1) + 1;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReorderCategorySiblingsAsync(
        Guid? parentId,
        IReadOnlyList<Guid> orderedCategoryIds,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (orderedCategoryIds.Count == 0)
        {
            throw new InvalidOperationException("فهرست ترتیب خواهر/برادر خالی است.");
        }

        var siblings = await _db.Categories
            .Where(x => x.ParentCategoryId == parentId)
            .ToListAsync(cancellationToken);
        var siblingIds = siblings.Select(x => x.CategoryId).ToHashSet();
        if (orderedCategoryIds.Count != siblingIds.Count
            || orderedCategoryIds.Any(id => !siblingIds.Contains(id))
            || orderedCategoryIds.Distinct().Count() != orderedCategoryIds.Count)
        {
            throw new InvalidOperationException("فهرست ترتیب باید دقیقاً همهٔ خواهر/برادرهای همان والد را پوشش دهد.");
        }

        var now = DateTimeOffset.UtcNow;
        var byId = siblings.ToDictionary(x => x.CategoryId);
        for (var i = 0; i < orderedCategoryIds.Count; i++)
        {
            byId[orderedCategoryIds[i]].SetSortOrder(i, now);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryTreeNodeDto>> GetCategoryTreeAsync(
        string locale,
        string? search,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = CatalogCategorySlugNormalizer.NormalizeLocale(locale);
        var categories = await _db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        if (categories.Count == 0) return [];

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

            // include ancestors so tree remains coherent for Ant Tree
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

        return filtered
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
    }

    /// <inheritdoc />
    public async Task<CategoryWorkspaceSummaryDto?> GetCategoryWorkspaceAsync(
        Guid categoryId,
        string? locale,
        CancellationToken cancellationToken)
    {
        var category = await _db.Categories.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);
        if (category is null) return null;

        var translations = await _db.CategoryTranslations.AsNoTracking()
            .Where(x => x.CategoryId == categoryId)
            .OrderBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(locale))
        {
            var normalized = CatalogCategorySlugNormalizer.NormalizeLocale(locale);
            translations = translations.Where(t => t.Locale == normalized).ToList();
        }

        return new CategoryWorkspaceSummaryDto(
            category.CategoryId,
            category.ParentCategoryId,
            category.Status,
            category.SortOrder,
            category.IsVisible,
            category.ImageMediaAssetId,
            category.IconMediaAssetId,
            category.CreatedAt,
            category.UpdatedAt,
            translations.Select(ToTranslationDto).ToList());
    }

    /// <inheritdoc />
    public async Task<CategoryRouteResolveResult?> ResolveCategoryRouteAsync(
        string locale,
        string slug,
        bool forStorefront,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = CatalogCategorySlugNormalizer.NormalizeLocale(locale);
        var normalizedSlug = CatalogCategorySlugNormalizer.NormalizeSlug(slug);

        var current = await _db.CategoryTranslations.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Locale == normalizedLocale && x.Slug == normalizedSlug,
                cancellationToken);
        if (current is not null)
        {
            var category = await _db.Categories.AsNoTracking()
                .SingleOrDefaultAsync(x => x.CategoryId == current.CategoryId, cancellationToken);
            if (category is null) return null;
            if (forStorefront && !IsStorefrontEligible(category)) return null;
            return new CategoryRouteResolveResult(
                category.CategoryId,
                normalizedLocale,
                current.Slug,
                IsRedirect: false,
                CanonicalPath: BuildCanonicalPath(normalizedLocale, current.Slug));
        }

        var history = await _db.CategorySlugHistories.AsNoTracking()
            .Where(x => x.Locale == normalizedLocale && x.OldSlug == normalizedSlug)
            .OrderByDescending(x => x.ChangedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (history is null) return null;

        // current slug always wins — if history target somehow lost translation, fail closed
        var live = await _db.CategoryTranslations.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.CategoryId == history.CategoryId && x.Locale == normalizedLocale,
                cancellationToken);
        if (live is null) return null;

        // loop/conflict: if historical slug equals another category's current slug, current already handled above
        var categoryHist = await _db.Categories.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CategoryId == history.CategoryId, cancellationToken);
        if (categoryHist is null) return null;
        if (forStorefront && !IsStorefrontEligible(categoryHist)) return null;

        return new CategoryRouteResolveResult(
            categoryHist.CategoryId,
            normalizedLocale,
            live.Slug,
            IsRedirect: true,
            CanonicalPath: BuildCanonicalPath(normalizedLocale, live.Slug));
    }

    /// <inheritdoc />
    public async Task ArchiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var category = await _db.Categories.SingleAsync(x => x.CategoryId == categoryId, cancellationToken);
        category.Archive(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var category = await _db.Categories.SingleAsync(x => x.CategoryId == categoryId, cancellationToken);
        category.Publish(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BrandReference> CreateBrandAsync(
        string? slugSeam,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var brand = CatalogBrand.Create(slugSeam, DateTimeOffset.UtcNow);
        _db.Brands.Add(brand);
        AddLocalizedNames(CatalogLocalizedOwnerKind.Brand, brand.BrandId, localizedNames);
        await _db.SaveChangesAsync(cancellationToken);
        return new BrandReference(brand.BrandId, brand.SlugSeam, brand.Status);
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAttributeDefinitionAsync(
        string code,
        CatalogAttributeValueKind valueKind,
        bool isVariantAxis,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var definition = CatalogAttributeDefinition.Create(code, valueKind, isVariantAxis, DateTimeOffset.UtcNow);
        _db.AttributeDefinitions.Add(definition);
        AddLocalizedNames(CatalogLocalizedOwnerKind.AttributeDefinition, definition.DefinitionId, localizedNames);
        await _db.SaveChangesAsync(cancellationToken);
        return definition.DefinitionId;
    }

    /// <inheritdoc />
    public async Task UpdateAttributeDefinitionAsync(
        Guid definitionId,
        string? unit,
        bool isRequired,
        bool isFilterable,
        bool isComparable,
        bool isMultivalue,
        int displayOrder,
        decimal? validationMin,
        decimal? validationMax,
        int? validationMaxLength,
        bool isActive,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var definition = await _db.AttributeDefinitions.SingleAsync(x => x.DefinitionId == definitionId, cancellationToken);
        definition.UpdateMetadata(
            unit,
            isRequired,
            isFilterable,
            isComparable,
            isMultivalue,
            displayOrder,
            validationMin,
            validationMax,
            validationMaxLength,
            isActive);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AttributeDefinitionView>> ListAttributeDefinitionsAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.AttributeDefinitions.AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        return rows.Select(ToDefinitionView).ToList();
    }

    /// <inheritdoc />
    public async Task<AttributeDefinitionView?> GetAttributeDefinitionAsync(
        Guid definitionId,
        CancellationToken cancellationToken)
    {
        var row = await _db.AttributeDefinitions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DefinitionId == definitionId, cancellationToken);
        return row is null ? null : ToDefinitionView(row);
    }

    /// <inheritdoc />
    public async Task<Guid> AddAttributeOptionAsync(
        Guid definitionId,
        string code,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var definition = await _db.AttributeDefinitions.SingleAsync(x => x.DefinitionId == definitionId, cancellationToken);
        if (definition.ValueKind != CatalogAttributeValueKind.Enumeration)
        {
            throw new InvalidOperationException("گزینه فقط برای ویژگی شمارشی معنا دارد.");
        }

        var option = CatalogAttributeOption.Create(definitionId, code);
        _db.AttributeOptions.Add(option);
        AddLocalizedNames(CatalogLocalizedOwnerKind.AttributeOption, option.OptionId, localizedNames);
        await _db.SaveChangesAsync(cancellationToken);
        return option.OptionId;
    }

    /// <inheritdoc />
    public async Task BindCategoryAttributeAsync(
        Guid categoryId,
        Guid definitionId,
        int displayOrder,
        CategoryAttributeAssignmentFlags flags,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(flags);
        var definition = await _db.AttributeDefinitions.SingleOrDefaultAsync(
            x => x.DefinitionId == definitionId,
            cancellationToken)
            ?? throw new InvalidOperationException("رده یا تعریف ویژگی در Catalog این Tenant نیست.");
        if (!await _db.Categories.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            throw new InvalidOperationException("رده یا تعریف ویژگی در Catalog این Tenant نیست.");
        }

        CatalogCategoryAttributeAssignmentRules.ValidateVariantAxis(definition, flags.IsVariantAxis);

        if (await _db.CategoryAttributeBindings.AnyAsync(
                x => x.CategoryId == categoryId && x.DefinitionId == definitionId,
                cancellationToken))
        {
            throw new InvalidOperationException("این تعریف از قبل به رده پیوند شده است.");
        }

        _db.CategoryAttributeBindings.Add(
            CatalogCategoryAttributeBinding.Bind(
                categoryId,
                definitionId,
                displayOrder,
                flags.IsRequired,
                flags.IsFilterable,
                flags.IsVariantAxis,
                flags.IsComparable,
                DateTimeOffset.UtcNow));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateCategoryAttributeBindingAsync(
        Guid categoryId,
        Guid definitionId,
        CategoryAttributeAssignmentFlags flags,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(flags);
        var definition = await _db.AttributeDefinitions.SingleOrDefaultAsync(
            x => x.DefinitionId == definitionId,
            cancellationToken)
            ?? throw new InvalidOperationException("تعریف ویژگی در Catalog این Tenant نیست.");
        var binding = await _db.CategoryAttributeBindings.SingleOrDefaultAsync(
            x => x.CategoryId == categoryId && x.DefinitionId == definitionId,
            cancellationToken)
            ?? throw new InvalidOperationException("پیوند schema رده پیدا نشد.");

        CatalogCategoryAttributeAssignmentRules.ValidateVariantAxis(definition, flags.IsVariantAxis);

        binding.IsRequired = flags.IsRequired;
        binding.IsFilterable = flags.IsFilterable;
        binding.IsVariantAxis = flags.IsVariantAxis;
        binding.IsComparable = flags.IsComparable;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UnbindCategoryAttributeAsync(
        Guid categoryId,
        Guid definitionId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var binding = await _db.CategoryAttributeBindings.SingleOrDefaultAsync(
            x => x.CategoryId == categoryId && x.DefinitionId == definitionId,
            cancellationToken)
            ?? throw new InvalidOperationException("پیوند schema رده پیدا نشد.");
        _db.CategoryAttributeBindings.Remove(binding);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReorderCategoryAttributeBindingsAsync(
        Guid categoryId,
        IReadOnlyList<Guid> orderedDefinitionIds,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(orderedDefinitionIds);
        var bindings = await _db.CategoryAttributeBindings
            .Where(x => x.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
        if (bindings.Count != orderedDefinitionIds.Count
            || orderedDefinitionIds.Distinct().Count() != orderedDefinitionIds.Count
            || bindings.Select(b => b.DefinitionId).ToHashSet().SetEquals(orderedDefinitionIds) is false)
        {
            throw new InvalidOperationException("فهرست ترتیب باید دقیقاً همان پیوندهای موجود رده باشد.");
        }

        for (var i = 0; i < orderedDefinitionIds.Count; i++)
        {
            var binding = bindings.Single(b => b.DefinitionId == orderedDefinitionIds[i]);
            binding.DisplayOrder = i;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EffectiveSchemaEntry>> GetEffectiveCategorySchemaAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveEffectiveBindingsAsync(categoryId, cancellationToken);
        return resolved.Select(x => new EffectiveSchemaEntry(
            x.DefinitionId,
            x.Definition.Code,
            x.Definition.ValueKind,
            x.Definition.IsVariantAxisAllowed,
            x.IsVariantAxis,
            x.Definition.Unit,
            x.IsRequired,
            x.IsFilterable,
            x.IsComparable,
            x.Definition.IsMultivalue,
            x.DisplayOrder,
            x.InheritedFromCategoryId,
            x.Definition.IsActive)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EffectiveCategoryFacet>> GetEffectiveCategoryFacetsAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveEffectiveFacetsAsync(categoryId, cancellationToken);
        if (resolved.Count == 0)
        {
            return Array.Empty<EffectiveCategoryFacet>();
        }

        var names = await GetAttributeDefinitionNamesAsync(
            resolved.Select(x => x.DefinitionId).ToArray(),
            locale,
            cancellationToken);
        return resolved.Select(x => new EffectiveCategoryFacet(
            x.DefinitionId,
            x.Definition.Code,
            names.GetValueOrDefault(x.DefinitionId) ?? x.Definition.Code,
            x.Definition.ValueKind,
            x.DisplayType,
            x.SortOrder,
            x.IsVisible,
            x.IsSearchable,
            x.IsCollapsedByDefault,
            x.ShowCounts,
            x.SourceCategoryId,
            x.SourceCategoryId != categoryId)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryFacetConfigurationView>> ListLocalFacetConfigurationsAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Categories.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            throw new InvalidOperationException("رده در Catalog این Tenant نیست.");
        }

        var configs = await _db.CategoryFacetConfigurations.AsNoTracking()
            .Where(x => x.CategoryId == categoryId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
        if (configs.Count == 0)
        {
            return Array.Empty<CategoryFacetConfigurationView>();
        }

        var definitionIds = configs.Select(x => x.DefinitionId).ToArray();
        var definitions = await _db.AttributeDefinitions.AsNoTracking()
            .Where(x => definitionIds.Contains(x.DefinitionId))
            .ToDictionaryAsync(x => x.DefinitionId, cancellationToken);
        return configs.Select(config =>
        {
            if (!definitions.TryGetValue(config.DefinitionId, out var definition))
            {
                throw new InvalidOperationException("تعریف ویژگی facet در Catalog نیست.");
            }

            return new CategoryFacetConfigurationView(
                config.FacetConfigurationId,
                config.CategoryId,
                config.DefinitionId,
                definition.Code,
                definition.ValueKind,
                config.DisplayType,
                config.SortOrder,
                config.IsVisible,
                config.IsSearchable,
                config.IsCollapsedByDefault,
                config.ShowCounts);
        }).ToList();
    }

    /// <inheritdoc />
    public async Task UpsertCategoryFacetConfigurationAsync(
        Guid categoryId,
        Guid definitionId,
        CategoryFacetConfigurationInput input,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(input);
        if (!await _db.Categories.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            throw new InvalidOperationException("رده در Catalog این Tenant نیست.");
        }

        var definition = await _db.AttributeDefinitions.SingleOrDefaultAsync(
            x => x.DefinitionId == definitionId,
            cancellationToken)
            ?? throw new InvalidOperationException("تعریف ویژگی در Catalog این Tenant نیست.");

        var effective = await ResolveEffectiveBindingsAsync(categoryId, cancellationToken);
        var schemaRow = effective.SingleOrDefault(x => x.DefinitionId == definitionId)
            ?? throw new InvalidOperationException("این ویژگی در schema مؤثر این رده نیست.");
        if (!schemaRow.IsFilterable)
        {
            throw new InvalidOperationException("فقط ویژگی‌های قابل فیلتر برای این رده مجاز به پیکربندی فیلتر هستند.");
        }

        CatalogCategoryFacetRules.ValidateDisplayType(definition, input.DisplayType);
        var isSearchable = input.IsSearchable && CatalogCategoryFacetRules.IsSearchableAllowed(input.DisplayType);

        var existing = await _db.CategoryFacetConfigurations.SingleOrDefaultAsync(
            x => x.CategoryId == categoryId && x.DefinitionId == definitionId,
            cancellationToken);
        if (existing is null)
        {
            _db.CategoryFacetConfigurations.Add(
                CatalogCategoryFacetConfiguration.Create(
                    categoryId,
                    definitionId,
                    input.DisplayType,
                    input.SortOrder,
                    input.IsVisible,
                    isSearchable,
                    input.IsCollapsedByDefault,
                    input.ShowCounts,
                    DateTimeOffset.UtcNow));
        }
        else
        {
            existing.DisplayType = input.DisplayType;
            existing.SortOrder = input.SortOrder;
            existing.IsVisible = input.IsVisible;
            existing.IsSearchable = isSearchable;
            existing.IsCollapsedByDefault = input.IsCollapsedByDefault;
            existing.ShowCounts = input.ShowCounts;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveCategoryFacetOverrideAsync(
        Guid categoryId,
        Guid definitionId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var config = await _db.CategoryFacetConfigurations.SingleOrDefaultAsync(
            x => x.CategoryId == categoryId && x.DefinitionId == definitionId,
            cancellationToken)
            ?? throw new InvalidOperationException("پیکربندی facet محلی پیدا نشد.");
        _db.CategoryFacetConfigurations.Remove(config);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReorderCategoryFacetConfigurationsAsync(
        Guid categoryId,
        IReadOnlyList<Guid> orderedDefinitionIds,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(orderedDefinitionIds);
        var configs = await _db.CategoryFacetConfigurations
            .Where(x => x.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
        if (configs.Count != orderedDefinitionIds.Count
            || orderedDefinitionIds.Distinct().Count() != orderedDefinitionIds.Count
            || configs.Select(c => c.DefinitionId).ToHashSet().SetEquals(orderedDefinitionIds) is false)
        {
            throw new InvalidOperationException("فهرست ترتیب باید دقیقاً همان پیکربندی‌های محلی رده باشد.");
        }

        for (var i = 0; i < orderedDefinitionIds.Count; i++)
        {
            var config = configs.Single(c => c.DefinitionId == orderedDefinitionIds[i]);
            config.SortOrder = i;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CategoryMegaMenuConfigurationView> GetCategoryMegaMenuConfigurationAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken)
    {
        var category = await _db.Categories.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken)
            ?? throw new InvalidOperationException("رده در Catalog این Tenant نیست.");

        var normalizedLocale = CatalogCategorySlugNormalizer.NormalizeLocale(locale);
        var translation = await _db.CategoryTranslations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CategoryId == categoryId && x.Locale == normalizedLocale, cancellationToken);

        var item = await _db.MegaMenuItems.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);

        if (item is null)
        {
            var defaultTitle = translation?.Name ?? await ResolveCategoryDisplayNameAsync(categoryId, normalizedLocale, cancellationToken);
            var previewSlug = translation?.Slug ?? string.Empty;
            return new CategoryMegaMenuConfigurationView(
                categoryId,
                false,
                null,
                null,
                null,
                0,
                false,
                false,
                null,
                null,
                defaultTitle,
                null,
                null,
                null,
                BuildUiCategoryRoute(normalizedLocale, previewSlug),
                0,
                category.Status == CatalogPublicationStatus.Published,
                category.IsVisible);
        }

        var overrideRow = await _db.MegaMenuItemTranslations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MegaMenuItemId == item.MegaMenuItemId && x.Locale == normalizedLocale, cancellationToken);

        var allItems = await _db.MegaMenuItems.AsNoTracking().ToListAsync(cancellationToken);
        var itemsById = allItems.ToDictionary(x => x.MegaMenuItemId);
        var level = ComputePresentationLevel(item.MegaMenuItemId, itemsById);
        var parentPath = item.ParentMegaMenuItemId is Guid parentId
            ? await BuildMenuPathAsync(parentId, normalizedLocale, itemsById, cancellationToken)
            : null;

        var displayTitle = overrideRow?.TitleOverride ?? translation?.Name
            ?? await ResolveCategoryDisplayNameAsync(categoryId, normalizedLocale, cancellationToken);
        var slug = translation?.Slug ?? string.Empty;

        return new CategoryMegaMenuConfigurationView(
            categoryId,
            true,
            item.MegaMenuItemId,
            item.ParentMegaMenuItemId,
            parentPath,
            item.SortOrder,
            item.IsVisible,
            item.IsFeatured,
            item.ImageMediaAssetId,
            item.IconMediaAssetId,
            displayTitle,
            overrideRow?.TitleOverride,
            overrideRow?.BadgeText,
            overrideRow?.ShortLabel,
            BuildUiCategoryRoute(normalizedLocale, slug),
            level,
            category.Status == CatalogPublicationStatus.Published,
            category.IsVisible);
    }

    /// <inheritdoc />
    public async Task UpsertCategoryMegaMenuBindingAsync(
        Guid categoryId,
        string locale,
        CategoryMegaMenuBindingInput input,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(input);
        _ = await _db.Categories.SingleOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken)
            ?? throw new InvalidOperationException("رده در Catalog این Tenant نیست.");

        var allItems = await _db.MegaMenuItems.ToListAsync(cancellationToken);
        var duplicate = allItems.SingleOrDefault(x => x.CategoryId == categoryId);
        var itemsById = allItems.ToDictionary(x => x.MegaMenuItemId);

        CatalogMegaMenuItem item;
        if (duplicate is null)
        {
            item = CatalogMegaMenuItem.BindCategory(
                categoryId,
                input.ParentMegaMenuItemId,
                input.SortOrder,
                input.IsVisible,
                input.IsFeatured,
                input.ImageMediaAssetId,
                input.IconMediaAssetId,
                DateTimeOffset.UtcNow);
            allItems.Add(item);
            itemsById[item.MegaMenuItemId] = item;
            _db.MegaMenuItems.Add(item);
        }
        else
        {
            item = duplicate;
            item.ParentMegaMenuItemId = input.ParentMegaMenuItemId;
            item.SortOrder = input.SortOrder;
            item.IsVisible = input.IsVisible;
            item.IsFeatured = input.IsFeatured;
            item.ImageMediaAssetId = input.ImageMediaAssetId;
            item.IconMediaAssetId = input.IconMediaAssetId;
        }

        CatalogMegaMenuTreeRules.ValidatePlacement(item.MegaMenuItemId, item.ParentMegaMenuItemId, itemsById);
        var normalizedLocale = CatalogCategorySlugNormalizer.NormalizeLocale(locale);
        await UpsertMegaMenuTranslationAsync(item.MegaMenuItemId, normalizedLocale, input, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveCategoryMegaMenuBindingAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var item = await _db.MegaMenuItems.SingleOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);
        if (item is null)
        {
            return;
        }

        var children = await _db.MegaMenuItems.Where(x => x.ParentMegaMenuItemId == item.MegaMenuItemId).ToListAsync(cancellationToken);
        if (children.Count > 0)
        {
            throw new InvalidOperationException("ابتدا زیرمجموعه‌های presentation این آیتم را جابه‌جا یا حذف کنید.");
        }

        var translations = await _db.MegaMenuItemTranslations
            .Where(x => x.MegaMenuItemId == item.MegaMenuItemId)
            .ToListAsync(cancellationToken);
        _db.MegaMenuItemTranslations.RemoveRange(translations);
        _db.MegaMenuItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MegaMenuPlacementOption>> ListMegaMenuPlacementOptionsAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = CatalogCategorySlugNormalizer.NormalizeLocale(locale);
        var allItems = await _db.MegaMenuItems.AsNoTracking().ToListAsync(cancellationToken);
        var items = allItems.Where(x => x.CategoryId != categoryId).ToList();
        if (items.Count == 0)
        {
            return Array.Empty<MegaMenuPlacementOption>();
        }

        var itemsById = allItems.ToDictionary(x => x.MegaMenuItemId);
        var categoryIds = items.Select(x => x.CategoryId).Distinct().ToArray();
        var translations = await _db.CategoryTranslations.AsNoTracking()
            .Where(x => categoryIds.Contains(x.CategoryId) && x.Locale == normalizedLocale)
            .ToDictionaryAsync(x => x.CategoryId, cancellationToken);

        var options = new List<MegaMenuPlacementOption>();
        foreach (var item in items.OrderBy(x => x.SortOrder))
        {
            var level = ComputePresentationLevel(item.MegaMenuItemId, itemsById);
            if (level >= CatalogMegaMenuTreeRules.MaxPresentationDepth)
            {
                continue;
            }

            var label = translations.TryGetValue(item.CategoryId, out var tr) ? tr.Name : "—";
            var path = await BuildMenuPathAsync(item.MegaMenuItemId, normalizedLocale, itemsById, cancellationToken);
            options.Add(new MegaMenuPlacementOption(item.MegaMenuItemId, item.CategoryId, label, path, level));
        }

        return options;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StorefrontMegaMenuItem>> GetStorefrontMegaMenuAsync(
        string locale,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = CatalogCategorySlugNormalizer.NormalizeLocale(locale);
        var uiSegment = MapUiLocaleSegment(normalizedLocale);
        var items = await _db.MegaMenuItems.AsNoTracking().ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            return Array.Empty<StorefrontMegaMenuItem>();
        }

        var categories = await _db.Categories.AsNoTracking().ToDictionaryAsync(x => x.CategoryId, cancellationToken);
        var categoryIds = items.Select(x => x.CategoryId).Distinct().ToArray();
        var translations = await _db.CategoryTranslations.AsNoTracking()
            .Where(x => categoryIds.Contains(x.CategoryId) && x.Locale == normalizedLocale)
            .ToDictionaryAsync(x => x.CategoryId, cancellationToken);

        var itemIds = items.Select(x => x.MegaMenuItemId).ToArray();
        var overrides = await _db.MegaMenuItemTranslations.AsNoTracking()
            .Where(x => itemIds.Contains(x.MegaMenuItemId) && x.Locale == normalizedLocale)
            .ToDictionaryAsync(x => x.MegaMenuItemId, cancellationToken);

        var composed = CatalogMegaMenuComposer.ComposeStorefrontMenu(
            items,
            categories,
            translations,
            overrides,
            normalizedLocale,
            uiSegment);

        return composed.Select(x => new StorefrontMegaMenuItem(
            x.MegaMenuItemId,
            x.ParentMegaMenuItemId,
            x.CategoryId,
            x.Title,
            x.Destination,
            x.IsFeatured,
            x.IconMediaAssetId,
            x.ImageMediaAssetId,
            x.SortOrder)).ToList();
    }

    /// <inheritdoc />
    public async Task<ProductReference> CreateProductAsync(
        CatalogProductKind kind,
        string? slugSeam,
        Guid? brandId,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (brandId is Guid brand && !await _db.Brands.AnyAsync(x => x.BrandId == brand, cancellationToken))
        {
            throw new InvalidOperationException("برند در Catalog این Tenant وجود ندارد.");
        }

        var product = CatalogProduct.Create(kind, slugSeam, DateTimeOffset.UtcNow);
        product.BrandId = brandId;
        _db.Products.Add(product);
        AddLocalizedNames(CatalogLocalizedOwnerKind.Product, product.ProductId, localizedNames);
        await _db.SaveChangesAsync(cancellationToken);
        return new ProductReference(product.ProductId, product.Kind, product.Status);
    }

    /// <inheritdoc />
    public async Task UpsertProductLocalizedFieldAsync(
        Guid productId,
        string fieldKey,
        IReadOnlyDictionary<string, string> localizedValues,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var normalizedKey = fieldKey.Trim().ToLowerInvariant();
        if (normalizedKey is not ("short_description" or "full_description"))
        {
            throw new InvalidOperationException("فقط فیلدهای شرح کوتاه و شرح کامل محصول از این درز پذیرفته می‌شوند.");
        }

        if (localizedValues.Count == 0
            || !await _db.Products.AnyAsync(product => product.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول موجود و حداقل یک متن محلی غیرخالی لازم است.");
        }

        foreach (var pair in localizedValues)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key);
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Value);
            var locale = pair.Key.Trim();
            var row = await _db.LocalizedTexts.SingleOrDefaultAsync(
                text => text.OwnerKind == CatalogLocalizedOwnerKind.Product
                    && text.OwnerId == productId
                    && text.FieldKey == normalizedKey
                    && text.Locale == locale,
                cancellationToken);
            if (row is null)
            {
                _db.LocalizedTexts.Add(CatalogLocalizedText.Create(
                    CatalogLocalizedOwnerKind.Product,
                    productId,
                    normalizedKey,
                    locale,
                    pair.Value));
            }
            else
            {
                row.Value = pair.Value.Trim();
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AssignCategoryAsync(Guid productId, Guid categoryId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken)
            || !await _db.Categories.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            throw new InvalidOperationException("محصول یا رده در Catalog این Tenant نیست.");
        }

        await EnsureAssignableProductCategoryAsync(categoryId, cancellationToken);

        _db.ProductCategories.Add(CatalogProductCategory.Assign(productId, categoryId));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AttachMediaReferenceAsync(Guid productId, Guid mediaAssetId, CancellationToken cancellationToken) =>
        await AttachMediaReferenceAsync(productId, mediaAssetId, altText: null, cancellationToken);

    /// <inheritdoc />
    public async Task AttachMediaReferenceAsync(
        Guid productId,
        Guid mediaAssetId,
        string? altText,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (mediaAssetId == Guid.Empty)
        {
            throw new InvalidOperationException("شناسهٔ رسانه لازم است.");
        }

        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        if (await _db.MediaReferences.AnyAsync(
                x => x.ProductId == productId && x.MediaAssetId == mediaAssetId,
                cancellationToken))
        {
            throw new InvalidOperationException("این رسانه قبلاً به محصول وصل شده است.");
        }

        var existing = await _db.MediaReferences
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var maxOrder = existing.Count == 0 ? -1 : existing.Max(x => x.DisplayOrder);
        var isFirst = existing.Count == 0;
        var link = CatalogProductMediaReference.Link(
            productId,
            mediaAssetId,
            displayOrder: maxOrder + 1,
            isPrimary: isFirst,
            altText: altText);
        existing.Add(link);
        _db.MediaReferences.Add(link);
        EnforcePrimaryUniqueness(existing);
        TouchProductUpdatedAt(productId);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid> AttachGeneratedPlaceholderMediaAsync(
        Guid productId,
        string? altText,
        CancellationToken cancellationToken)
    {
        var assetId = UuidV7.New();
        await AttachMediaReferenceAsync(productId, assetId, altText, cancellationToken);
        return assetId;
    }

    /// <inheritdoc />
    public async Task<ProductMediaEditorState> GetProductMediaEditorStateAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        var items = await LoadOrderedMediaAssignmentsAsync(productId, cancellationToken);
        return new ProductMediaEditorState(productId, items, BuildMediaReadiness(items));
    }

    /// <inheritdoc />
    public async Task ReorderProductMediaAsync(
        Guid productId,
        IReadOnlyList<Guid> orderedMediaAssetIds,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(orderedMediaAssetIds);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        var media = await _db.MediaReferences.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        if (media.Count == 0)
        {
            throw new InvalidOperationException("رسانه‌ای برای این محصول نیست.");
        }

        var existing = media.Select(x => x.MediaAssetId).ToHashSet();
        if (orderedMediaAssetIds.Count != existing.Count
            || orderedMediaAssetIds.Any(id => !existing.Contains(id))
            || orderedMediaAssetIds.Distinct().Count() != orderedMediaAssetIds.Count)
        {
            throw new InvalidOperationException("فهرست ترتیب باید دقیقاً همهٔ رسانه‌های فعلی را بدون تکرار پوشش دهد.");
        }

        for (var i = 0; i < orderedMediaAssetIds.Count; i++)
        {
            media.Single(m => m.MediaAssetId == orderedMediaAssetIds[i]).DisplayOrder = i;
        }

        EnforcePrimaryUniqueness(media);
        TouchProductUpdatedAt(productId);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetProductPrimaryMediaAsync(
        Guid productId,
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        var media = await _db.MediaReferences.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var target = media.SingleOrDefault(x => x.MediaAssetId == mediaAssetId)
            ?? throw new InvalidOperationException("رسانه روی این محصول پیدا نشد.");
        foreach (var row in media)
        {
            row.IsPrimary = row.MediaAssetId == target.MediaAssetId;
        }

        TouchProductUpdatedAt(productId);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task PatchProductMediaAltAsync(
        Guid productId,
        Guid mediaAssetId,
        string? altText,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var row = await _db.MediaReferences.SingleOrDefaultAsync(
            x => x.ProductId == productId && x.MediaAssetId == mediaAssetId,
            cancellationToken)
            ?? throw new InvalidOperationException("رسانه روی این محصول پیدا نشد.");
        row.AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
        TouchProductUpdatedAt(productId);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DetachProductMediaAsync(
        Guid productId,
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        var media = await _db.MediaReferences.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        var row = media.SingleOrDefault(x => x.MediaAssetId == mediaAssetId)
            ?? throw new InvalidOperationException("رسانه روی این محصول پیدا نشد.");
        _db.MediaReferences.Remove(row);
        media.Remove(row);
        EnforcePrimaryUniqueness(media);
        TouchProductUpdatedAt(productId);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProductMediaReadiness> GetProductMediaReadinessAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        var items = await LoadOrderedMediaAssignmentsAsync(productId, cancellationToken);
        return BuildMediaReadiness(items);
    }

    /// <inheritdoc />
    public async Task<ProductSeoDetail> GetProductSeoAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken)
            ?? throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        return await BuildProductSeoDetailAsync(product, locale, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProductSeoDetail> UpdateProductSeoAsync(
        Guid productId,
        ProductSeoUpdateInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        var locale = ProductSeoRules.NormalizeLocale(input.Locale);
        var product = await _db.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken)
            ?? throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        await _db.Entry(product).ReloadAsync(cancellationToken);
        if (product.UpdatedAt != input.ExpectedUpdatedAt)
        {
            throw new InvalidOperationException("workspace.catalog.stale");
        }

        string slug;
        try
        {
            if (string.IsNullOrWhiteSpace(input.Slug))
            {
                var name = await ResolveProductNameForSeoAsync(productId, locale, cancellationToken);
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new InvalidOperationException("نشانی صفحه نامعتبر است.");
                }

                slug = CatalogCategorySlugNormalizer.SlugifyFromName(name);
            }
            else
            {
                slug = CatalogCategorySlugNormalizer.NormalizeSlug(input.Slug);
            }
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException("نشانی صفحه نامعتبر است.");
        }

        if (await _db.Products.AsNoTracking()
                .AnyAsync(x => x.ProductId != productId && x.SlugSeam == slug, cancellationToken))
        {
            throw new InvalidOperationException("این نشانی صفحه قبلاً استفاده شده است.");
        }

        await UpsertProductLocalizedFieldForSeoAsync(productId, "seo_title", locale, input.SeoTitle, cancellationToken);
        await UpsertProductLocalizedFieldForSeoAsync(
            productId,
            "seo_description",
            locale,
            input.SeoDescription,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var seoTitleTrimmed = string.IsNullOrWhiteSpace(input.SeoTitle) ? null : input.SeoTitle.Trim();
        product.TouchDescriptiveSeams(slug, seoTitleTrimmed ?? product.SeoTitleSeam, product.BrandId, now);
        if (locale.Equals("fa-IR", StringComparison.OrdinalIgnoreCase))
        {
            product.SeoTitleSeam = seoTitleTrimmed;
        }

        product.SlugSeam = slug;
        product.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);

        return await GetProductSeoAsync(productId, locale, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProductSeoReadiness> GetProductSeoReadinessAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken)
    {
        var detail = await GetProductSeoAsync(productId, locale, cancellationToken);
        return detail.Readiness;
    }

    private async Task<ProductSeoDetail> BuildProductSeoDetailAsync(
        CatalogProduct product,
        string locale,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = ProductSeoRules.NormalizeLocale(locale);
        var name = await ResolveProductNameForSeoAsync(product.ProductId, normalizedLocale, cancellationToken);
        var seoTitle = await ResolveLocalizedFieldForSeoAsync(
            product.ProductId,
            "seo_title",
            normalizedLocale,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(seoTitle)
            && normalizedLocale.Equals("fa-IR", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(product.SeoTitleSeam))
        {
            seoTitle = product.SeoTitleSeam;
        }

        var seoDescription = await ResolveLocalizedFieldForSeoAsync(
            product.ProductId,
            "seo_description",
            normalizedLocale,
            cancellationToken);
        var titleFallback = string.IsNullOrWhiteSpace(seoTitle) ? name : seoTitle;
        var snapshot = ProductSeoRules.Evaluate(product.SlugSeam, seoTitle, seoDescription, name);
        var readiness = new ProductSeoReadiness(
            snapshot.HasValidSlug,
            snapshot.HasSeoTitleOrFallback,
            snapshot.HasSeoDescription,
            snapshot.HasLocalizedIdentity,
            snapshot.IsReady,
            snapshot.MessageFa);

        return new ProductSeoDetail(
            product.ProductId,
            normalizedLocale,
            product.SlugSeam,
            seoTitle,
            seoDescription,
            name,
            titleFallback,
            ProductSeoRules.BuildPublicPath(normalizedLocale, product.SlugSeam),
            readiness,
            product.UpdatedAt);
    }

    private async Task<string?> ResolveProductNameForSeoAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken)
    {
        var exact = await ResolveLocalizedFieldForSeoAsync(productId, "name", locale, cancellationToken);
        if (!string.IsNullOrWhiteSpace(exact))
        {
            return exact;
        }

        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                && x.OwnerId == productId
                && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        return rows
            .OrderBy(x => x.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .Select(x => x.Value)
            .FirstOrDefault();
    }

    private async Task<string?> ResolveLocalizedFieldForSeoAsync(
        Guid productId,
        string fieldKey,
        string locale,
        CancellationToken cancellationToken)
    {
        var row = await _db.LocalizedTexts.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                    && x.OwnerId == productId
                    && x.FieldKey == fieldKey
                    && x.Locale == locale,
                cancellationToken);
        return row?.Value;
    }

    private async Task UpsertProductLocalizedFieldForSeoAsync(
        Guid productId,
        string fieldKey,
        string locale,
        string? value,
        CancellationToken cancellationToken)
    {
        var normalized = value?.Trim();
        var row = await _db.LocalizedTexts.SingleOrDefaultAsync(
            x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                && x.OwnerId == productId
                && x.FieldKey == fieldKey
                && x.Locale == locale,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            if (row is not null)
            {
                _db.LocalizedTexts.Remove(row);
            }

            return;
        }

        if (row is null)
        {
            _db.LocalizedTexts.Add(CatalogLocalizedText.Create(
                CatalogLocalizedOwnerKind.Product,
                productId,
                fieldKey,
                locale,
                normalized));
        }
        else
        {
            row.Value = normalized;
        }
    }

    private async Task<IReadOnlyList<ProductMediaAssignment>> LoadOrderedMediaAssignmentsAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var media = await _db.MediaReferences.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ReferenceId)
            .ToListAsync(cancellationToken);
        return media
            .Select(m => new ProductMediaAssignment(m.MediaAssetId, m.IsPrimary, m.DisplayOrder, m.AltText))
            .ToList();
    }

    private static ProductMediaReadiness BuildMediaReadiness(IReadOnlyList<ProductMediaAssignment> items)
    {
        var count = items.Count;
        var hasPrimary = items.Any(x => x.IsPrimary);
        var isReady = count > 0 && hasPrimary;
        string? messageFa;
        if (count == 0 || !hasPrimary)
        {
            messageFa = "تصویر اصلی تعیین نشده";
        }
        else if (isReady)
        {
            messageFa = "رسانه کامل است";
        }
        else
        {
            messageFa = null;
        }

        return new ProductMediaReadiness(hasPrimary, count, isReady, messageFa);
    }

    /// <summary>
    /// دقیقاً یک IsPrimary وقتی count&gt;0؛ در غیر این صورت اولین DisplayOrder.
    /// </summary>
    private static void EnforcePrimaryUniqueness(IList<CatalogProductMediaReference> media)
    {
        if (media.Count == 0)
        {
            return;
        }

        var ordered = media.OrderBy(x => x.DisplayOrder).ThenBy(x => x.ReferenceId).ToList();
        var primaries = ordered.Where(x => x.IsPrimary).ToList();
        CatalogProductMediaReference keep;
        if (primaries.Count == 1)
        {
            keep = primaries[0];
        }
        else if (primaries.Count == 0)
        {
            keep = ordered[0];
        }
        else
        {
            keep = primaries[0];
        }

        foreach (var row in media)
        {
            row.IsPrimary = ReferenceEquals(row, keep) || row.ReferenceId == keep.ReferenceId;
        }
    }

    private void TouchProductUpdatedAt(Guid productId)
    {
        var product = _db.Products.Local.SingleOrDefault(x => x.ProductId == productId)
            ?? _db.Products.Single(x => x.ProductId == productId);
        product.UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public async Task SetProductAttributeAsync(
        Guid productId,
        Guid definitionId,
        string rawValue,
        Guid? enumOptionId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        var definition = await _db.AttributeDefinitions.SingleAsync(x => x.DefinitionId == definitionId, cancellationToken);
        if (!definition.IsActive)
        {
            throw new InvalidOperationException("تعریف ویژگی غیرفعال است.");
        }

        if (definition.IsVariantAxis)
        {
            throw new InvalidOperationException("محور Variant روی خود Product ذخیره نمی‌شود؛ به گونه تعلق دارد.");
        }

        await EnsureDefinitionAllowedForProductSchemaAsync(productId, definitionId, cancellationToken);

        if (definition.ValueKind == CatalogAttributeValueKind.Enumeration)
        {
            if (enumOptionId is not Guid optionId)
            {
                throw new InvalidOperationException("گزینهٔ شمارشی باید شناسه داشته باشد.");
            }

            var option = await _db.AttributeOptions.SingleOrDefaultAsync(
                x => x.OptionId == optionId && x.DefinitionId == definitionId,
                cancellationToken)
                ?? throw new InvalidOperationException("گزینه به این تعریف تعلق ندارد.");
            if (!option.IsActive)
            {
                throw new InvalidOperationException("گزینهٔ شمارشی غیرفعال است.");
            }
        }

        var canonical = CatalogAttributeCanonicalizer.Canonicalize(definition.ValueKind, rawValue, enumOptionId);
        CatalogAttributeCanonicalizer.EnforceValidationBounds(definition, canonical);

        var existing = await _db.ProductAttributeValues.SingleOrDefaultAsync(
            x => x.ProductId == productId && x.DefinitionId == definitionId,
            cancellationToken);
        if (existing is null)
        {
            _db.ProductAttributeValues.Add(CatalogProductAttributeValue.Create(productId, definitionId, canonical));
        }
        else
        {
            // upsert: ردیف موجود را عوض می‌کنیم تا unique (ProductId, DefinitionId) بشکند.
            _db.ProductAttributeValues.Remove(existing);
            _db.ProductAttributeValues.Add(CatalogProductAttributeValue.Create(productId, definitionId, canonical));
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProductAttributeEditorState> GetProductAttributeEditorStateAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        var normalizedLocale = string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale.Trim();
        var categoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
        string? categoryPath = null;
        IReadOnlyList<CatalogEffectiveSchemaBinding> schema = Array.Empty<CatalogEffectiveSchemaBinding>();
        if (categoryId is Guid cid)
        {
            categoryPath = await BuildCategoryPathAsync(cid, normalizedLocale, cancellationToken);
            schema = await ResolveEffectiveBindingsAsync(cid, cancellationToken);
        }

        var values = await _db.ProductAttributeValues.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var valueByDef = values.ToDictionary(x => x.DefinitionId);

        var definitionIds = schema.Select(x => x.DefinitionId).ToArray();
        var names = await GetAttributeDefinitionNamesAsync(definitionIds, normalizedLocale, cancellationToken);
        var enumDefIds = schema
            .Where(x => x.Definition.ValueKind == CatalogAttributeValueKind.Enumeration)
            .Select(x => x.DefinitionId)
            .ToArray();
        var options = enumDefIds.Length == 0
            ? new List<CatalogAttributeOption>()
            : await _db.AttributeOptions.AsNoTracking()
                .Where(x => enumDefIds.Contains(x.DefinitionId))
                .OrderBy(x => x.Code)
                .ToListAsync(cancellationToken);
        var optionNames = await GetAttributeOptionNamesAsync(
            options.Select(x => x.OptionId).ToArray(),
            normalizedLocale,
            cancellationToken);
        var optionsByDef = options.GroupBy(x => x.DefinitionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var fields = new List<ProductAttributeEditorField>();
        foreach (var entry in schema.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Definition.Code, StringComparer.Ordinal))
        {
            valueByDef.TryGetValue(entry.DefinitionId, out var stored);
            var optionViews = Array.Empty<ProductAttributeEditorOption>();
            if (optionsByDef.TryGetValue(entry.DefinitionId, out var defOptions))
            {
                optionViews = defOptions.Select(o => new ProductAttributeEditorOption(
                    o.OptionId,
                    optionNames.GetValueOrDefault(o.OptionId) ?? o.Code,
                    o.IsActive)).ToArray();
            }

            Guid? currentEnumOptionId = null;
            string? displayValue = null;
            if (stored is not null)
            {
                (currentEnumOptionId, displayValue) = FormatAttributeDisplay(
                    entry.Definition.ValueKind,
                    entry.Definition.IsMultivalue,
                    stored.CanonicalValue,
                    entry.Definition.Unit,
                    optionViews);
            }

            var isMissingRequired = entry.IsRequired
                && !entry.IsVariantAxis
                && entry.Definition.IsActive
                && stored is null;

            fields.Add(new ProductAttributeEditorField(
                entry.DefinitionId,
                entry.Definition.Code,
                names.GetValueOrDefault(entry.DefinitionId) ?? entry.Definition.Code,
                entry.Definition.ValueKind,
                entry.Definition.Unit,
                entry.IsRequired,
                entry.IsVariantAxis,
                entry.IsFilterable,
                entry.IsComparable,
                entry.Definition.IsMultivalue,
                entry.DisplayOrder,
                optionViews,
                stored?.CanonicalValue,
                currentEnumOptionId,
                displayValue,
                isMissingRequired));
        }

        var readiness = BuildReadiness(schema, values);
        return new ProductAttributeEditorState(productId, categoryId, categoryPath, fields, readiness);
    }

    /// <inheritdoc />
    public async Task SetProductAttributesAsync(
        Guid productId,
        IReadOnlyList<ProductAttributeValueInput> values,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(values);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var input in values)
            {
                await ApplyProductAttributeValueAsync(productId, input, cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductAttributeReadiness> GetProductAttributeReadinessAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        var categoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
        if (categoryId is not Guid cid)
        {
            return new ProductAttributeReadiness(true, Array.Empty<string>(), Array.Empty<string>());
        }

        var schema = await ResolveEffectiveBindingsAsync(cid, cancellationToken);
        var values = await _db.ProductAttributeValues.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        return BuildReadiness(schema, values);
    }

    /// <inheritdoc />
    public async Task SetProductVariantAxesAsync(
        Guid productId,
        IReadOnlyList<Guid> orderedDefinitionIds,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(orderedDefinitionIds);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        if (orderedDefinitionIds.Distinct().Count() != orderedDefinitionIds.Count)
        {
            throw new InvalidOperationException("محورهای Variant محصول نباید تکراری باشند.");
        }

        foreach (var definitionId in orderedDefinitionIds)
        {
            var definition = await _db.AttributeDefinitions.SingleAsync(x => x.DefinitionId == definitionId, cancellationToken);
            if (!definition.IsVariantAxisAllowed)
            {
                throw new InvalidOperationException("فقط تعریف‌های مجاز محور Variant قابل انتخاب هستند.");
            }

            if (!definition.IsActive)
            {
                throw new InvalidOperationException("تعریف محور Variant غیرفعال است.");
            }
        }

        var categoryIds = await _db.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.CategoryId)
            .ToListAsync(cancellationToken);
        if (categoryIds.Count > 0)
        {
            foreach (var definitionId in orderedDefinitionIds)
            {
                var enabled = false;
                foreach (var categoryId in categoryIds)
                {
                    var schema = await ResolveEffectiveBindingsAsync(categoryId, cancellationToken);
                    if (schema.Any(x => x.DefinitionId == definitionId && x.IsVariantAxis))
                    {
                        enabled = true;
                        break;
                    }
                }

                if (!enabled)
                {
                    throw new InvalidOperationException("محور تنوع در schema مؤثر رده‌های محصول فعال نیست.");
                }
            }
        }

        var existing = await _db.ProductVariantAxes.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        _db.ProductVariantAxes.RemoveRange(existing);
        for (var i = 0; i < orderedDefinitionIds.Count; i++)
        {
            _db.ProductVariantAxes.Add(CatalogProductVariantAxis.Create(productId, orderedDefinitionIds[i], i));
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private const int MaxVariantCombinations = 200;

    /// <inheritdoc />
    public async Task<ProductVariantEditorState> GetProductVariantEditorStateAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        var normalizedLocale = string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale.Trim();
        var categoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
        string? categoryPath = null;
        IReadOnlyList<CatalogEffectiveSchemaBinding> schema = Array.Empty<CatalogEffectiveSchemaBinding>();
        if (categoryId is Guid cid)
        {
            categoryPath = await BuildCategoryPathAsync(cid, normalizedLocale, cancellationToken);
            schema = await ResolveEffectiveBindingsAsync(cid, cancellationToken);
        }

        var axisBindings = schema
            .Where(x => x.IsVariantAxis && x.Definition.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Definition.Code, StringComparer.Ordinal)
            .ToList();

        string? messageFa = null;
        if (axisBindings.Count == 0)
        {
            messageFa = "برای این دسته‌بندی ویژگی تنوع تعریف نشده است.";
        }

        var axisDefIds = axisBindings.Select(x => x.DefinitionId).ToArray();
        var names = await GetAttributeDefinitionNamesAsync(axisDefIds, normalizedLocale, cancellationToken);
        var options = axisDefIds.Length == 0
            ? new List<CatalogAttributeOption>()
            : await _db.AttributeOptions.AsNoTracking()
                .Where(x => axisDefIds.Contains(x.DefinitionId))
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Code)
                .ToListAsync(cancellationToken);
        var optionNames = await GetAttributeOptionNamesAsync(
            options.Select(x => x.OptionId).ToArray(),
            normalizedLocale,
            cancellationToken);
        var optionsByDef = options.GroupBy(x => x.DefinitionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var variants = await _db.Variants.AsNoTracking()
            .Include(x => x.AttributeValues)
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var selectedByDef = new Dictionary<Guid, HashSet<Guid>>();
        var labelDefIds = axisDefIds.ToHashSet();
        var labelOptionIds = options.Select(x => x.OptionId).ToHashSet();
        foreach (var variant in variants)
        {
            foreach (var av in variant.AttributeValues)
            {
                labelDefIds.Add(av.DefinitionId);
                if (Guid.TryParseExact(av.CanonicalValue, "N", out var optionId)
                    || Guid.TryParse(av.CanonicalValue, out optionId))
                {
                    labelOptionIds.Add(optionId);
                    if (variant.Status == CatalogPublicationStatus.Archived)
                    {
                        continue;
                    }

                    if (!selectedByDef.TryGetValue(av.DefinitionId, out var set))
                    {
                        set = [];
                        selectedByDef[av.DefinitionId] = set;
                    }

                    set.Add(optionId);
                }
            }
        }

        names = await GetAttributeDefinitionNamesAsync(labelDefIds.ToArray(), normalizedLocale, cancellationToken);
        optionNames = await GetAttributeOptionNamesAsync(labelOptionIds.ToArray(), normalizedLocale, cancellationToken);

        var axes = new List<ProductVariantAxisEditorField>();
        foreach (var binding in axisBindings)
        {
            optionsByDef.TryGetValue(binding.DefinitionId, out var defOptions);
            defOptions ??= [];
            var optionViews = defOptions.Select(o => new ProductVariantAxisOption(
                o.OptionId,
                optionNames.GetValueOrDefault(o.OptionId) ?? o.Code,
                o.Code,
                o.IsActive)).ToList();
            selectedByDef.TryGetValue(binding.DefinitionId, out var selected);
            axes.Add(new ProductVariantAxisEditorField(
                binding.DefinitionId,
                binding.Definition.Code,
                names.GetValueOrDefault(binding.DefinitionId) ?? binding.Definition.Code,
                binding.Definition.ValueKind,
                optionViews,
                selected?.ToList() ?? []));
        }

        var listItems = await MapVariantListItemsAsync(variants, names, optionNames, cancellationToken);
        var readiness = await GetProductVariantReadinessAsync(productId, cancellationToken);
        return new ProductVariantEditorState(
            productId,
            categoryPath,
            axes,
            listItems,
            readiness,
            MaxVariantCombinations,
            messageFa);
    }

    /// <inheritdoc />
    public async Task<ProductVariantPreviewResult> PreviewProductVariantCombinationsAsync(
        Guid productId,
        IReadOnlyList<ProductVariantSelectedAxisInput> selectedAxes,
        string locale,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectedAxes);
        var normalizedLocale = string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale.Trim();
        var built = await BuildDesiredCombinationsAsync(productId, selectedAxes, normalizedLocale, cancellationToken);
        if (built.ErrorFa is not null)
        {
            throw new InvalidOperationException(built.ErrorFa);
        }

        var existing = await _db.Variants.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var byFingerprint = existing
            .GroupBy(x => x.CombinationFingerprint)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.UpdatedAt).First());

        var desiredSet = built.Combinations.Select(c => c.Fingerprint).ToHashSet(StringComparer.Ordinal);
        var previews = new List<ProductVariantCombinationPreview>();
        foreach (var combo in built.Combinations)
        {
            byFingerprint.TryGetValue(combo.Fingerprint, out var match);
            var action = match is null
                ? ProductVariantCombinationAction.New
                : match.Status == CatalogPublicationStatus.Archived
                    ? ProductVariantCombinationAction.New
                    : ProductVariantCombinationAction.Unchanged;
            previews.Add(new ProductVariantCombinationPreview(
                combo.Fingerprint,
                combo.Labels,
                match?.VariantId,
                action,
                null));
        }

        foreach (var variant in existing.Where(v => v.Status != CatalogPublicationStatus.Archived))
        {
            if (desiredSet.Contains(variant.CombinationFingerprint))
            {
                continue;
            }

            var labels = built.LabelLookup.TryGetValue(variant.CombinationFingerprint, out var cached)
                ? cached
                : await ResolveVariantAxisLabelsAsync(variant.VariantId, normalizedLocale, cancellationToken);
            previews.Add(new ProductVariantCombinationPreview(
                variant.CombinationFingerprint,
                labels,
                variant.VariantId,
                ProductVariantCombinationAction.Deactivate,
                null));
        }

        var unchanged = previews.Count(x => x.Action == ProductVariantCombinationAction.Unchanged);
        var neu = previews.Count(x => x.Action == ProductVariantCombinationAction.New);
        var deactivate = previews.Count(x => x.Action == ProductVariantCombinationAction.Deactivate);
        var messageFa =
            $"{ToPersianDigits(unchanged)} تنوع بدون تغییر · {ToPersianDigits(neu)} تنوع جدید · {ToPersianDigits(deactivate)} تنوع دیگر انتخاب نشده است";

        return new ProductVariantPreviewResult(
            previews,
            unchanged,
            neu,
            deactivate,
            built.Combinations.Count,
            built.Capped,
            built.WarningFa,
            messageFa);
    }

    /// <inheritdoc />
    public async Task<ProductVariantApplyResult> ApplyProductVariantMatrixAsync(
        Guid productId,
        ProductVariantApplyInput input,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.SelectedAxes);

        var locale = string.IsNullOrWhiteSpace(input.Locale) ? "fa-IR" : input.Locale.Trim();
        var built = await BuildDesiredCombinationsAsync(productId, input.SelectedAxes, locale, cancellationToken);
        if (built.ErrorFa is not null)
        {
            throw new InvalidOperationException(built.ErrorFa);
        }

        if (built.Capped)
        {
            throw new InvalidOperationException(
                built.WarningFa ?? $"تعداد ترکیب‌ها از سقف {MaxVariantCombinations} بیشتر است.");
        }

        var now = DateTimeOffset.UtcNow;
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var axisDefIds = built.OrderedAxes.Select(x => x.DefinitionId).ToList();
            var existingAxes = await _db.ProductVariantAxes.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
            _db.ProductVariantAxes.RemoveRange(existingAxes);
            for (var i = 0; i < axisDefIds.Count; i++)
            {
                _db.ProductVariantAxes.Add(CatalogProductVariantAxis.Create(productId, axisDefIds[i], i));
            }

            var variants = await _db.Variants
                .Include(x => x.AttributeValues)
                .Where(x => x.ProductId == productId)
                .ToListAsync(cancellationToken);
            var byFingerprint = variants
                .GroupBy(x => x.CombinationFingerprint)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.UpdatedAt).First());

            var desiredSet = built.Combinations.Select(c => c.Fingerprint).ToHashSet(StringComparer.Ordinal);
            var created = 0;
            var unchanged = 0;
            var deactivated = 0;
            var sort = 0;

            foreach (var combo in built.Combinations)
            {
                if (byFingerprint.TryGetValue(combo.Fingerprint, out var existing))
                {
                    if (existing.Status == CatalogPublicationStatus.Archived)
                    {
                        existing.SetStatus(CatalogPublicationStatus.Draft, now);
                    }

                    existing.SetSortOrder(sort++, now);
                    unchanged++;
                    continue;
                }

                var variant = CatalogVariant.Create(productId, combo.Fingerprint, null, now);
                variant.SetSortOrder(sort++, now);
                foreach (var axis in combo.Axes)
                {
                    variant.AttributeValues.Add(
                        CatalogVariantAttributeValue.Create(variant.VariantId, axis.DefinitionId, axis.Canonical));
                }

                _db.Variants.Add(variant);
                variants.Add(variant);
                byFingerprint[combo.Fingerprint] = variant;
                created++;
            }

            foreach (var variant in variants.Where(v => v.Status != CatalogPublicationStatus.Archived).ToList())
            {
                if (desiredSet.Contains(variant.CombinationFingerprint))
                {
                    continue;
                }

                // Prefer archive/deactivate; never hard-delete (Offer safety without Catalog→Offer join).
                variant.SetStatus(CatalogPublicationStatus.Archived, now);
                deactivated++;
            }

            if (input.VariantPatches is { Count: > 0 })
            {
                var byId = variants.ToDictionary(x => x.VariantId);
                foreach (var patch in input.VariantPatches)
                {
                    if (!byId.TryGetValue(patch.VariantId, out var target))
                    {
                        throw new InvalidOperationException("تنوع موردنظر در این محصول نیست.");
                    }

                    if (patch.Status is CatalogPublicationStatus status)
                    {
                        target.SetStatus(status, now);
                    }

                    if (patch.CatalogCodeSeam is not null)
                    {
                        target.UpdateCatalogCodeSeam(patch.CatalogCodeSeam, now);
                    }

                    if (patch.SortOrder is int order)
                    {
                        target.SetSortOrder(order, now);
                    }

                    if (patch.IsDefault is bool isDefault)
                    {
                        if (isDefault)
                        {
                            ClearDefaultFlags(variants, now);
                            target.SetDefault(true, now);
                        }
                        else
                        {
                            target.SetDefault(false, now);
                        }
                    }
                }
            }

            if (input.DefaultVariantId is Guid defaultId)
            {
                var target = variants.SingleOrDefault(x => x.VariantId == defaultId)
                    ?? throw new InvalidOperationException("تنوع پیش‌فرض در این محصول نیست.");
                if (target.Status == CatalogPublicationStatus.Archived)
                {
                    throw new InvalidOperationException("تنوع بایگانی‌شده نمی‌تواند پیش‌فرض باشد.");
                }

                ClearDefaultFlags(variants, now);
                target.SetDefault(true, now);
            }
            else
            {
                EnforceSingleDefault(variants, now);
            }

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            var editor = await GetProductVariantEditorStateAsync(productId, locale, cancellationToken);
            return new ProductVariantApplyResult(created, unchanged, deactivated, editor.Variants);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductVariantReadiness> GetProductVariantReadinessAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        var categoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
        var missingAxes = new List<string>();
        var invalidVariants = new List<string>();
        var duplicates = new List<string>();

        IReadOnlyList<CatalogEffectiveSchemaBinding> schema = Array.Empty<CatalogEffectiveSchemaBinding>();
        if (categoryId is Guid cid)
        {
            schema = await ResolveEffectiveBindingsAsync(cid, cancellationToken);
        }

        var effectiveAxes = schema.Where(x => x.IsVariantAxis && x.Definition.IsActive).ToList();
        var productAxes = await _db.ProductVariantAxes.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);

        if (effectiveAxes.Count > 0 && productAxes.Count == 0)
        {
            var hasActiveVariants = await _db.Variants.AsNoTracking()
                .AnyAsync(x => x.ProductId == productId && x.Status != CatalogPublicationStatus.Archived, cancellationToken);
            if (!hasActiveVariants)
            {
                // Axes available but matrix not applied yet — not a hard readiness failure until variants exist.
            }
        }

        foreach (var productAxis in productAxes)
        {
            var binding = effectiveAxes.FirstOrDefault(x => x.DefinitionId == productAxis.DefinitionId);
            if (binding is null)
            {
                missingAxes.Add(productAxis.DefinitionId.ToString("N"));
                continue;
            }

            if (binding.Definition.ValueKind != CatalogAttributeValueKind.Enumeration)
            {
                missingAxes.Add($"{binding.Definition.Code}:محور غیرگزینه‌ای");
            }
        }

        var variants = await _db.Variants.AsNoTracking()
            .Include(x => x.AttributeValues)
            .Where(x => x.ProductId == productId && x.Status != CatalogPublicationStatus.Archived)
            .ToListAsync(cancellationToken);

        var effectiveAxisIds = effectiveAxes.Select(x => x.DefinitionId).ToHashSet();
        foreach (var group in variants.GroupBy(x => x.CombinationFingerprint))
        {
            if (group.Count() > 1)
            {
                duplicates.Add(group.Key);
            }
        }

        foreach (var variant in variants)
        {
            var defs = variant.AttributeValues.Select(x => x.DefinitionId).ToHashSet();
            if (effectiveAxisIds.Count > 0 && !defs.SetEquals(effectiveAxisIds) && productAxes.Count > 0)
            {
                var selected = productAxes.Select(x => x.DefinitionId).ToHashSet();
                if (!defs.SetEquals(selected))
                {
                    invalidVariants.Add(variant.VariantId.ToString("N"));
                }
            }

            foreach (var av in variant.AttributeValues)
            {
                if (!Guid.TryParseExact(av.CanonicalValue, "N", out _)
                    && !Guid.TryParse(av.CanonicalValue, out _))
                {
                    invalidVariants.Add(variant.VariantId.ToString("N"));
                    break;
                }
            }
        }

        bool? noDefault = null;
        if (variants.Count > 0)
        {
            noDefault = !variants.Any(x => x.IsDefault);
        }

        var isValid = missingAxes.Count == 0
            && invalidVariants.Count == 0
            && duplicates.Count == 0
            && noDefault != true;

        return new ProductVariantReadiness(
            isValid,
            missingAxes,
            invalidVariants.Distinct().ToList(),
            duplicates,
            noDefault);
    }

    /// <inheritdoc />
    public async Task<CategoryChangeImpact> PreviewCategoryChangeAsync(
        Guid productId,
        Guid newCategoryId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        if (!await _db.Categories.AnyAsync(x => x.CategoryId == newCategoryId, cancellationToken))
        {
            throw new InvalidOperationException("رده در Catalog این Tenant نیست.");
        }

        await EnsureAssignableProductCategoryAsync(newCategoryId, cancellationToken);

        var schema = await ResolveEffectiveBindingsAsync(newCategoryId, cancellationToken);
        var values = await _db.ProductAttributeValues.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var axes = await _db.ProductVariantAxes.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var report = CatalogCategorySchemaResolver.PreviewCategoryChange(values, axes, schema);
        return new CategoryChangeImpact(
            productId,
            newCategoryId,
            report.OrphanAttributeValues.Select(x => new OrphanProductAttributeValue(x.DefinitionId, x.CanonicalValue)).ToList(),
            report.InvalidVariantAxisDefinitionIds);
    }

    /// <inheritdoc />
    public async Task<CategoryChangeImpactReport> PreviewCategoryChangeReportAsync(
        Guid productId,
        Guid newCategoryId,
        string locale,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale.Trim();
        var impact = await PreviewCategoryChangeAsync(productId, newCategoryId, cancellationToken);
        var newSchema = await ResolveEffectiveBindingsAsync(newCategoryId, cancellationToken);
        var values = await _db.ProductAttributeValues.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var allowed = newSchema.Select(x => x.DefinitionId).ToHashSet();
        var compatiblePreserved = values.Count(v => allowed.Contains(v.DefinitionId));

        var presentIds = values.Select(v => v.DefinitionId).ToHashSet();
        var newlyRequired = newSchema
            .Where(x => x.IsRequired && !x.IsVariantAxis && x.Definition.IsActive && !presentIds.Contains(x.DefinitionId))
            .ToList();

        var orphanDefIds = impact.OrphanAttributeValues.Select(x => x.DefinitionId).Distinct().ToArray();
        var labelIds = orphanDefIds.Concat(newlyRequired.Select(x => x.DefinitionId)).Distinct().ToArray();
        var names = await GetAttributeDefinitionNamesAsync(labelIds, normalizedLocale, cancellationToken);

        var orphanOptionIds = new List<Guid>();
        foreach (var orphan in impact.OrphanAttributeValues)
        {
            foreach (var part in orphan.CanonicalValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Guid.TryParse(part, out var oid))
                {
                    orphanOptionIds.Add(oid);
                }
            }
        }

        var optionNames = await GetAttributeOptionNamesAsync(orphanOptionIds, normalizedLocale, cancellationToken);
        var definitions = orphanDefIds.Length == 0
            ? new Dictionary<Guid, CatalogAttributeDefinition>()
            : await _db.AttributeDefinitions.AsNoTracking()
                .Where(x => orphanDefIds.Contains(x.DefinitionId))
                .ToDictionaryAsync(x => x.DefinitionId, cancellationToken);

        var orphanSummaries = impact.OrphanAttributeValues.Select(o =>
        {
            definitions.TryGetValue(o.DefinitionId, out var def);
            var display = FormatOrphanDisplay(def, o.CanonicalValue, optionNames);
            return new CategoryChangeOrphanSummary(
                o.DefinitionId,
                names.GetValueOrDefault(o.DefinitionId) ?? def?.Code ?? o.DefinitionId.ToString("N"),
                display);
        }).ToList();

        var newlyRequiredLabels = newlyRequired
            .Select(x => names.GetValueOrDefault(x.DefinitionId) ?? x.Definition.Code)
            .ToList();

        var impactedVariants = await _db.Variants.AsNoTracking()
            .CountAsync(
                x => x.ProductId == productId && x.Status != CatalogPublicationStatus.Archived,
                cancellationToken);
        var newAxisIds = newSchema.Where(x => x.IsVariantAxis).Select(x => x.DefinitionId).ToHashSet();
        var currentAxes = await _db.ProductVariantAxes.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.DefinitionId)
            .ToListAsync(cancellationToken);
        var axesChanged = impact.InvalidVariantAxisDefinitionIds.Count > 0
            || currentAxes.Any(id => !newAxisIds.Contains(id))
            || (currentAxes.Count > 0 && !currentAxes.ToHashSet().SetEquals(newAxisIds));
        var variantImpactCount = axesChanged ? impactedVariants : 0;
        var variantImpactFa = variantImpactCount > 0
            ? $"{ToPersianDigits(variantImpactCount)} تنوع تحت تأثیر تغییر محورها قرار می‌گیرد و حذف خودکار نمی‌شود"
            : null;

        var messageParts = new List<string>
        {
            $"{ToPersianDigits(compatiblePreserved)} مقدار حفظ می‌شود",
            $"{ToPersianDigits(impact.OrphanAttributeValues.Count)} ویژگی دیگر در دسته جدید وجود ندارد",
            $"{ToPersianDigits(newlyRequired.Count)} ویژگی الزامی جدید باید تکمیل شود",
        };
        if (variantImpactFa is not null)
        {
            messageParts.Add(variantImpactFa);
        }

        var messageFa = string.Join("\n", messageParts);

        return new CategoryChangeImpactReport(
            productId,
            newCategoryId,
            compatiblePreserved,
            impact.OrphanAttributeValues.Count,
            newlyRequired.Count,
            orphanSummaries,
            newlyRequiredLabels,
            impact.InvalidVariantAxisDefinitionIds,
            messageFa,
            variantImpactCount,
            variantImpactFa);
    }

    /// <inheritdoc />
    public async Task<CategoryChangeImpact> ReplaceProductPrimaryCategoryAsync(
        Guid productId,
        Guid newCategoryId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        // Preview enforces Level-3 assignability before replace.
        var impact = await PreviewCategoryChangeAsync(productId, newCategoryId, cancellationToken);
        var existing = await _db.ProductCategories.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        _db.ProductCategories.RemoveRange(existing);
        _db.ProductCategories.Add(CatalogProductCategory.Assign(productId, newCategoryId));
        await _db.SaveChangesAsync(cancellationToken);
        return impact;
    }

    /// <inheritdoc />
    public async Task ValidateProductAttributesAsync(Guid productId, CancellationToken cancellationToken)
    {
        var categoryIds = await _db.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.CategoryId)
            .ToListAsync(cancellationToken);
        if (categoryIds.Count == 0)
        {
            return;
        }

        var required = new HashSet<Guid>();
        foreach (var categoryId in categoryIds)
        {
            foreach (var entry in await ResolveEffectiveBindingsAsync(categoryId, cancellationToken))
            {
                if (entry.IsRequired && entry.Definition.IsActive && !entry.IsVariantAxis)
                {
                    required.Add(entry.DefinitionId);
                }
            }
        }

        if (required.Count == 0)
        {
            return;
        }

        var present = await _db.ProductAttributeValues.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.DefinitionId)
            .ToListAsync(cancellationToken);
        var missing = required.Except(present).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException("مقادیر الزامی schema محصول کامل نیست.");
        }
    }

    /// <inheritdoc />
    public async Task<ProductPublishReadiness> GetProductPublishReadinessAsync(
        Guid productId,
        string? locale,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        var normalizedLocale = ProductSeoRules.NormalizeLocale(locale);
        var categoryReady = await IsProductPrimaryCategoryAssignableAsync(productId, cancellationToken);
        var name = await ResolveProductNameForSeoAsync(productId, normalizedLocale, cancellationToken);
        var translationReady = !string.IsNullOrWhiteSpace(name);

        var attributes = await GetProductAttributeReadinessAsync(productId, cancellationToken);
        var attributeReady = attributes.IsComplete;

        var variants = await GetProductVariantReadinessAsync(productId, cancellationToken);
        var variantReady = variants.IsValid;

        var media = await GetProductMediaReadinessAsync(productId, cancellationToken);
        var mediaReady = media.IsReady;

        var seo = await GetProductSeoReadinessAsync(productId, normalizedLocale, cancellationToken);
        var seoReady = seo.IsReady;

        var missing = new List<ProductPublishMissingRequirement>();
        if (!categoryReady)
        {
            missing.Add(new ProductPublishMissingRequirement(
                "category",
                ProductPublishRules.MessageCategoryIncompleteFa,
                "general"));
        }

        if (!translationReady)
        {
            missing.Add(new ProductPublishMissingRequirement(
                "identity",
                ProductPublishRules.MessageIdentityIncompleteFa,
                "general"));
        }

        if (!attributeReady)
        {
            var attrMessage = attributes.MissingRequiredCodes.Count > 0
                ? $"{ProductPublishRules.MessageAttributesIncompleteFa} ({string.Join("، ", attributes.MissingRequiredCodes)})"
                : ProductPublishRules.MessageAttributesIncompleteFa;
            missing.Add(new ProductPublishMissingRequirement("attributes", attrMessage, "attributes"));
        }

        if (!variantReady)
        {
            missing.Add(new ProductPublishMissingRequirement(
                "variants",
                ProductPublishRules.MessageVariantsIncompleteFa,
                "variants"));
        }

        if (!mediaReady)
        {
            missing.Add(new ProductPublishMissingRequirement(
                "media",
                media.MessageFa ?? ProductPublishRules.MessageMediaIncompleteFa,
                "media"));
        }

        if (!seoReady)
        {
            missing.Add(new ProductPublishMissingRequirement(
                "seo",
                seo.MessageFa ?? ProductPublishRules.MessageSeoIncompleteFa,
                "seo"));
        }

        var isReady = missing.Count == 0;
        var messageFa = isReady
            ? ProductPublishRules.MessageReadyFa
            : ProductPublishRules.SummarizeMissingFa(missing.Count);

        return new ProductPublishReadiness(
            isReady,
            categoryReady,
            translationReady,
            attributeReady,
            variantReady,
            mediaReady,
            seoReady,
            missing,
            messageFa);
    }

    /// <inheritdoc />
    public async Task PublishProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var product = await _db.Products.SingleAsync(x => x.ProductId == productId, cancellationToken);
        if (product.Status == CatalogPublicationStatus.Published)
        {
            return;
        }

        if (product.Status == CatalogPublicationStatus.Archived)
        {
            throw new InvalidOperationException(ProductPublishRules.MessageRestoreBeforePublishFa);
        }

        var readiness = await GetProductPublishReadinessAsync(productId, "fa-IR", cancellationToken);
        if (!readiness.IsReady)
        {
            var detail = readiness.MissingRequirements.Count > 0
                ? string.Join(" ", readiness.MissingRequirements.Select(m => m.MessageFa))
                : ProductPublishRules.MessageNotReadyFa;
            throw new InvalidOperationException($"{readiness.MessageFa} {detail}".Trim());
        }

        product.Publish(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UnpublishProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var product = await _db.Products.SingleAsync(x => x.ProductId == productId, cancellationToken);
        product.Unpublish(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ArchiveProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var product = await _db.Products.SingleAsync(x => x.ProductId == productId, cancellationToken);
        product.Archive(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RestoreProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var product = await _db.Products.SingleAsync(x => x.ProductId == productId, cancellationToken);
        product.RestoreFromArchive(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsProductPrimaryCategoryAssignableAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var categoryIds = await _db.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.CategoryId)
            .ToListAsync(cancellationToken);
        if (categoryIds.Count == 0)
        {
            return false;
        }

        var parentById = await _db.Categories.AsNoTracking()
            .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
        foreach (var categoryId in categoryIds)
        {
            try
            {
                CatalogCategoryTreeRules.EnsureAssignableProductCategory(categoryId, parentById);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public async Task PublishBrandAsync(Guid brandId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var brand = await _db.Brands.SingleAsync(x => x.BrandId == brandId, cancellationToken);
        brand.Publish(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<VariantReference> CreateVariantAsync(
        Guid productId,
        string? catalogCodeSeam,
        IReadOnlyList<(Guid DefinitionId, string RawValue, Guid? EnumOptionId)> axes,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول والد تنوع در Catalog این Tenant نیست.");
        }

        var selectedAxes = await _db.ProductVariantAxes.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => x.DefinitionId)
            .ToListAsync(cancellationToken);
        if (selectedAxes.Count > 0)
        {
            var selectedSet = selectedAxes.ToHashSet();
            var axisDefs = axes.Select(a => a.DefinitionId).ToHashSet();
            if (!axisDefs.SetEquals(selectedSet))
            {
                throw new InvalidOperationException(
                    "وقتی محورهای محصول انتخاب شده‌اند، ترکیب تنوع باید دقیقاً همان مجموعه‌محورها باشد.");
            }
        }

        var normalized = new List<(Guid DefinitionId, string Canonical)>();
        foreach (var axis in axes)
        {
            var definition = await _db.AttributeDefinitions.SingleAsync(x => x.DefinitionId == axis.DefinitionId, cancellationToken);
            if (!definition.IsVariantAxis)
            {
                throw new InvalidOperationException("فقط ویژگی محور تنوع می‌تواند ترکیب تنوع بسازد.");
            }

            if (definition.ValueKind == CatalogAttributeValueKind.Enumeration && axis.EnumOptionId is Guid optionId)
            {
                var option = await _db.AttributeOptions.SingleOrDefaultAsync(
                    x => x.OptionId == optionId && x.DefinitionId == definition.DefinitionId,
                    cancellationToken)
                    ?? throw new InvalidOperationException("گزینه به این تعریف تعلق ندارد.");
                if (!option.IsActive)
                {
                    throw new InvalidOperationException("گزینهٔ شمارشی غیرفعال است.");
                }
            }

            var canonical = CatalogAttributeCanonicalizer.Canonicalize(definition.ValueKind, axis.RawValue, axis.EnumOptionId);
            CatalogAttributeCanonicalizer.EnforceValidationBounds(definition, canonical);
            normalized.Add((definition.DefinitionId, canonical));
        }

        var fingerprint = CatalogVariant.ComputeFingerprint(normalized);
        if (await _db.Variants.AnyAsync(
                x => x.ProductId == productId && x.CombinationFingerprint == fingerprint,
                cancellationToken))
        {
            throw new InvalidOperationException("ترکیب محور این تنوع برای همین محصول تکراری است؛ هویت Offer فروشنده نیست.");
        }

        var variant = CatalogVariant.Create(productId, fingerprint, catalogCodeSeam, DateTimeOffset.UtcNow);
        foreach (var item in normalized)
        {
            variant.AttributeValues.Add(CatalogVariantAttributeValue.Create(variant.VariantId, item.DefinitionId, item.Canonical));
        }

        _db.Variants.Add(variant);
        await _db.SaveChangesAsync(cancellationToken);
        return new VariantReference(variant.VariantId, variant.ProductId, variant.CombinationFingerprint, variant.Status);
    }

    private async Task ApplyProductAttributeValueAsync(
        Guid productId,
        ProductAttributeValueInput input,
        CancellationToken cancellationToken)
    {
        var definition = await _db.AttributeDefinitions.SingleOrDefaultAsync(
            x => x.DefinitionId == input.DefinitionId,
            cancellationToken)
            ?? throw new InvalidOperationException("تعریف ویژگی در Catalog این Tenant نیست.");

        if (!definition.IsActive)
        {
            throw new InvalidOperationException("تعریف ویژگی غیرفعال است.");
        }

        if (definition.IsVariantAxis)
        {
            throw new InvalidOperationException("محور Variant روی خود Product ذخیره نمی‌شود؛ به گونه تعلق دارد.");
        }

        await EnsureDefinitionAllowedForProductSchemaAsync(productId, input.DefinitionId, cancellationToken);

        var existing = await _db.ProductAttributeValues.SingleOrDefaultAsync(
            x => x.ProductId == productId && x.DefinitionId == input.DefinitionId,
            cancellationToken);

        if (input.Clear)
        {
            var categoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
            var isRequired = false;
            if (categoryId is Guid cid)
            {
                var schema = await ResolveEffectiveBindingsAsync(cid, cancellationToken);
                isRequired = schema.Any(x => x.DefinitionId == input.DefinitionId && x.IsRequired && !x.IsVariantAxis);
            }

            if (isRequired)
            {
                throw new InvalidOperationException("پاک‌سازی مقدار الزامی مجاز نیست.");
            }

            if (existing is not null)
            {
                _db.ProductAttributeValues.Remove(existing);
            }

            return;
        }

        string canonical;
        if (definition.ValueKind == CatalogAttributeValueKind.Enumeration && definition.IsMultivalue)
        {
            canonical = await CanonicalizeMultivalueEnumerationAsync(
                definition.DefinitionId,
                input.RawValue,
                input.EnumOptionId,
                cancellationToken);
        }
        else
        {
            if (definition.ValueKind == CatalogAttributeValueKind.Enumeration)
            {
                if (input.EnumOptionId is not Guid optionId)
                {
                    throw new InvalidOperationException("گزینهٔ شمارشی باید شناسه داشته باشد.");
                }

                var option = await _db.AttributeOptions.SingleOrDefaultAsync(
                    x => x.OptionId == optionId && x.DefinitionId == definition.DefinitionId,
                    cancellationToken)
                    ?? throw new InvalidOperationException("گزینه به این تعریف تعلق ندارد.");
                if (!option.IsActive)
                {
                    throw new InvalidOperationException("گزینهٔ شمارشی غیرفعال است.");
                }
            }

            var raw = input.RawValue;
            if (definition.ValueKind == CatalogAttributeValueKind.Enumeration)
            {
                raw = string.IsNullOrWhiteSpace(raw) ? "ignored" : raw;
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new InvalidOperationException("مقدار ویژگی خالی است.");
            }

            canonical = CatalogAttributeCanonicalizer.Canonicalize(definition.ValueKind, raw, input.EnumOptionId);
            CatalogAttributeCanonicalizer.EnforceValidationBounds(definition, canonical);
        }

        if (existing is null)
        {
            _db.ProductAttributeValues.Add(CatalogProductAttributeValue.Create(productId, input.DefinitionId, canonical));
        }
        else
        {
            _db.ProductAttributeValues.Remove(existing);
            _db.ProductAttributeValues.Add(CatalogProductAttributeValue.Create(productId, input.DefinitionId, canonical));
        }
    }

    private async Task<string> CanonicalizeMultivalueEnumerationAsync(
        Guid definitionId,
        string? rawValue,
        Guid? enumOptionId,
        CancellationToken cancellationToken)
    {
        var optionIds = new List<Guid>();
        if (enumOptionId is Guid single)
        {
            optionIds.Add(single);
        }

        if (!string.IsNullOrWhiteSpace(rawValue))
        {
            foreach (var part in rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!Guid.TryParse(part, out var oid))
                {
                    throw new InvalidOperationException("شناسهٔ گزینهٔ شمارشی چندمقداری نامعتبر است.");
                }

                optionIds.Add(oid);
            }
        }

        optionIds = optionIds.Distinct().ToList();
        if (optionIds.Count == 0)
        {
            throw new InvalidOperationException("حداقل یک گزینهٔ شمارشی لازم است.");
        }

        foreach (var optionId in optionIds)
        {
            var option = await _db.AttributeOptions.SingleOrDefaultAsync(
                x => x.OptionId == optionId && x.DefinitionId == definitionId,
                cancellationToken)
                ?? throw new InvalidOperationException("گزینه به این تعریف تعلق ندارد.");
            if (!option.IsActive)
            {
                throw new InvalidOperationException("گزینهٔ شمارشی غیرفعال است.");
            }
        }

        return string.Join(",", optionIds.Select(id => id.ToString("N")));
    }

    private static ProductAttributeReadiness BuildReadiness(
        IReadOnlyList<CatalogEffectiveSchemaBinding> schema,
        IReadOnlyList<CatalogProductAttributeValue> values)
    {
        var valueByDef = values.ToDictionary(x => x.DefinitionId);
        var missing = new List<string>();
        var invalid = new List<string>();

        foreach (var entry in schema)
        {
            if (entry.IsVariantAxis || !entry.Definition.IsActive)
            {
                continue;
            }

            valueByDef.TryGetValue(entry.DefinitionId, out var stored);
            if (entry.IsRequired && stored is null)
            {
                missing.Add(entry.Definition.Code);
                continue;
            }

            if (stored is null)
            {
                continue;
            }

            try
            {
                if (entry.Definition.ValueKind == CatalogAttributeValueKind.Enumeration)
                {
                    var parts = entry.Definition.IsMultivalue
                        ? stored.CanonicalValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        : [stored.CanonicalValue];
                    foreach (var part in parts)
                    {
                        if (!Guid.TryParse(part, out _))
                        {
                            invalid.Add(entry.Definition.Code);
                            break;
                        }
                    }
                }
                else
                {
                    CatalogAttributeCanonicalizer.EnforceValidationBounds(entry.Definition, stored.CanonicalValue);
                }
            }
            catch (Exception)
            {
                invalid.Add(entry.Definition.Code);
            }
        }

        return new ProductAttributeReadiness(missing.Count == 0 && invalid.Count == 0, missing, invalid);
    }

    private sealed record DesiredAxisValue(Guid DefinitionId, string Canonical, string DefinitionName, string ValueLabel);

    private sealed record DesiredCombination(
        string Fingerprint,
        IReadOnlyList<DesiredAxisValue> Axes,
        IReadOnlyList<ProductVariantAxisLabel> Labels);

    private sealed record DesiredCombinationBuild(
        IReadOnlyList<(Guid DefinitionId, int DisplayOrder, CatalogAttributeDefinition Definition)> OrderedAxes,
        IReadOnlyList<DesiredCombination> Combinations,
        IReadOnlyDictionary<string, IReadOnlyList<ProductVariantAxisLabel>> LabelLookup,
        bool Capped,
        string? WarningFa,
        string? ErrorFa);

    private async Task<DesiredCombinationBuild> BuildDesiredCombinationsAsync(
        Guid productId,
        IReadOnlyList<ProductVariantSelectedAxisInput> selectedAxes,
        string locale,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            return new DesiredCombinationBuild([], [], new Dictionary<string, IReadOnlyList<ProductVariantAxisLabel>>(), false, null, "محصول در Catalog این Tenant نیست.");
        }

        var categoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
        if (categoryId is not Guid cid)
        {
            return new DesiredCombinationBuild([], [], new Dictionary<string, IReadOnlyList<ProductVariantAxisLabel>>(), false, null, "برای این دسته‌بندی ویژگی تنوع تعریف نشده است.");
        }

        var schema = await ResolveEffectiveBindingsAsync(cid, cancellationToken);
        var effectiveAxes = schema
            .Where(x => x.IsVariantAxis && x.Definition.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Definition.Code, StringComparer.Ordinal)
            .ToList();
        if (effectiveAxes.Count == 0)
        {
            return new DesiredCombinationBuild([], [], new Dictionary<string, IReadOnlyList<ProductVariantAxisLabel>>(), false, null, "برای این دسته‌بندی ویژگی تنوع تعریف نشده است.");
        }

        var effectiveById = effectiveAxes.ToDictionary(x => x.DefinitionId);
        var orderedInputs = new List<(CatalogEffectiveSchemaBinding Binding, IReadOnlyList<Guid> OptionIds)>();
        foreach (var input in selectedAxes)
        {
            if (!effectiveById.TryGetValue(input.DefinitionId, out var binding))
            {
                return new DesiredCombinationBuild([], [], new Dictionary<string, IReadOnlyList<ProductVariantAxisLabel>>(), false, null, "محور انتخاب‌شده در schema مؤثر رده مجاز نیست.");
            }

            if (binding.Definition.ValueKind != CatalogAttributeValueKind.Enumeration)
            {
                return new DesiredCombinationBuild(
                    [],
                    [],
                    new Dictionary<string, IReadOnlyList<ProductVariantAxisLabel>>(),
                    false,
                    null,
                    "محورهای متن آزاد برای ماتریس تنوع پشتیبانی نمی‌شوند؛ فقط ویژگی‌های گزینه‌دار مجازند.");
            }

            var optionIds = (input.OptionIds ?? Array.Empty<Guid>()).Distinct().ToList();
            if (optionIds.Count == 0)
            {
                continue;
            }

            orderedInputs.Add((binding, optionIds));
        }

        orderedInputs = orderedInputs
            .OrderBy(x => x.Binding.DisplayOrder)
            .ThenBy(x => x.Binding.Definition.Code, StringComparer.Ordinal)
            .ToList();

        if (orderedInputs.Count == 0)
        {
            return new DesiredCombinationBuild(
                effectiveAxes.Select(x => (x.DefinitionId, x.DisplayOrder, x.Definition)).ToList(),
                [],
                new Dictionary<string, IReadOnlyList<ProductVariantAxisLabel>>(),
                false,
                null,
                null);
        }

        var allOptionIds = orderedInputs.SelectMany(x => x.OptionIds).Distinct().ToArray();
        var options = await _db.AttributeOptions.AsNoTracking()
            .Where(x => allOptionIds.Contains(x.OptionId))
            .ToListAsync(cancellationToken);
        var optionsById = options.ToDictionary(x => x.OptionId);
        var optionNames = await GetAttributeOptionNamesAsync(allOptionIds, locale, cancellationToken);
        var defNames = await GetAttributeDefinitionNamesAsync(
            orderedInputs.Select(x => x.Binding.DefinitionId).ToArray(),
            locale,
            cancellationToken);

        foreach (var (binding, optionIds) in orderedInputs)
        {
            foreach (var optionId in optionIds)
            {
                if (!optionsById.TryGetValue(optionId, out var option) || option.DefinitionId != binding.DefinitionId)
                {
                    return new DesiredCombinationBuild([], [], new Dictionary<string, IReadOnlyList<ProductVariantAxisLabel>>(), false, null, "گزینه به محور تنوع تعلق ندارد.");
                }

                if (!option.IsActive)
                {
                    return new DesiredCombinationBuild([], [], new Dictionary<string, IReadOnlyList<ProductVariantAxisLabel>>(), false, null, "گزینهٔ غیرفعال برای ماتریس تنوع مجاز نیست.");
                }
            }
        }

        var axisOptionLists = orderedInputs.Select(entry =>
        {
            var orderedOptions = entry.OptionIds
                .Select(id => optionsById[id])
                .OrderBy(o => o.DisplayOrder)
                .ThenBy(o => o.Code, StringComparer.Ordinal)
                .ToList();
            return (entry.Binding, Options: orderedOptions);
        }).ToList();

        long total = 1;
        foreach (var axis in axisOptionLists)
        {
            total *= axis.Options.Count;
            if (total > MaxVariantCombinations)
            {
                break;
            }
        }

        var capped = total > MaxVariantCombinations;
        var warningFa = capped
            ? $"تعداد ترکیب‌ها ({ToPersianDigits((int)Math.Min(total, int.MaxValue))}) از سقف امن {ToPersianDigits(MaxVariantCombinations)} بیشتر است. لطفاً گزینه‌های کمتری انتخاب کنید."
            : null;

        var combinations = new List<DesiredCombination>();
        if (!capped)
        {
            IEnumerable<IReadOnlyList<CatalogAttributeOption>> seed = [Array.Empty<CatalogAttributeOption>()];
            foreach (var axis in axisOptionLists)
            {
                seed = seed.SelectMany(prefix => axis.Options.Select(opt =>
                {
                    var next = new List<CatalogAttributeOption>(prefix.Count + 1);
                    next.AddRange(prefix);
                    next.Add(opt);
                    return (IReadOnlyList<CatalogAttributeOption>)next;
                }));
            }

            foreach (var comboOptions in seed)
            {
                var axes = new List<DesiredAxisValue>();
                for (var i = 0; i < comboOptions.Count; i++)
                {
                    var binding = axisOptionLists[i].Binding;
                    var option = comboOptions[i];
                    var canonical = option.OptionId.ToString("N");
                    axes.Add(new DesiredAxisValue(
                        binding.DefinitionId,
                        canonical,
                        defNames.GetValueOrDefault(binding.DefinitionId) ?? binding.Definition.Code,
                        optionNames.GetValueOrDefault(option.OptionId) ?? option.Code));
                }

                var fingerprint = CatalogVariant.ComputeFingerprint(axes.Select(a => (a.DefinitionId, a.Canonical)));
                var labels = axes.Select(a => new ProductVariantAxisLabel(a.DefinitionName, a.ValueLabel)).ToList();
                combinations.Add(new DesiredCombination(fingerprint, axes, labels));
            }
        }

        var labelLookup = combinations.ToDictionary(
            c => c.Fingerprint,
            c => c.Labels,
            StringComparer.Ordinal);

        return new DesiredCombinationBuild(
            orderedInputs.Select(x => (x.Binding.DefinitionId, x.Binding.DisplayOrder, x.Binding.Definition)).ToList(),
            combinations,
            labelLookup,
            capped,
            warningFa,
            null);
    }

    private async Task<IReadOnlyList<ProductVariantListItem>> MapVariantListItemsAsync(
        IReadOnlyList<CatalogVariant> variants,
        IReadOnlyDictionary<Guid, string> definitionNames,
        IReadOnlyDictionary<Guid, string> optionNames,
        CancellationToken cancellationToken)
    {
        var items = new List<ProductVariantListItem>();
        foreach (var variant in variants)
        {
            var labels = new List<ProductVariantAxisLabel>();
            foreach (var av in variant.AttributeValues.OrderBy(x => x.DefinitionId))
            {
                var defName = definitionNames.GetValueOrDefault(av.DefinitionId) ?? av.DefinitionId.ToString("N");
                string valueLabel = av.CanonicalValue;
                if (Guid.TryParseExact(av.CanonicalValue, "N", out var oid)
                    || Guid.TryParse(av.CanonicalValue, out oid))
                {
                    valueLabel = optionNames.GetValueOrDefault(oid) ?? av.CanonicalValue;
                }

                labels.Add(new ProductVariantAxisLabel(defName, valueLabel));
            }

            items.Add(new ProductVariantListItem(
                variant.VariantId,
                variant.CombinationFingerprint,
                variant.Status,
                variant.SortOrder,
                variant.IsDefault,
                variant.CatalogCodeSeam,
                labels,
                null));
        }

        await Task.CompletedTask;
        return items;
    }

    private async Task<IReadOnlyList<ProductVariantAxisLabel>> ResolveVariantAxisLabelsAsync(
        Guid variantId,
        string locale,
        CancellationToken cancellationToken)
    {
        var values = await _db.Set<CatalogVariantAttributeValue>().AsNoTracking()
            .Where(x => x.VariantId == variantId)
            .ToListAsync(cancellationToken);
        var defIds = values.Select(x => x.DefinitionId).ToArray();
        var names = await GetAttributeDefinitionNamesAsync(defIds, locale, cancellationToken);
        var optionIds = new List<Guid>();
        foreach (var v in values)
        {
            if (Guid.TryParseExact(v.CanonicalValue, "N", out var oid)
                || Guid.TryParse(v.CanonicalValue, out oid))
            {
                optionIds.Add(oid);
            }
        }

        var optionNames = await GetAttributeOptionNamesAsync(optionIds, locale, cancellationToken);
        return values
            .OrderBy(x => x.DefinitionId)
            .Select(v =>
            {
                var defName = names.GetValueOrDefault(v.DefinitionId) ?? v.DefinitionId.ToString("N");
                var valueLabel = v.CanonicalValue;
                if (Guid.TryParseExact(v.CanonicalValue, "N", out var oid)
                    || Guid.TryParse(v.CanonicalValue, out oid))
                {
                    valueLabel = optionNames.GetValueOrDefault(oid) ?? v.CanonicalValue;
                }

                return new ProductVariantAxisLabel(defName, valueLabel);
            })
            .ToList();
    }

    private static void ClearDefaultFlags(IEnumerable<CatalogVariant> variants, DateTimeOffset now)
    {
        foreach (var variant in variants.Where(x => x.IsDefault))
        {
            variant.SetDefault(false, now);
        }
    }

    private static void EnforceSingleDefault(IReadOnlyList<CatalogVariant> variants, DateTimeOffset now)
    {
        var activeDefaults = variants
            .Where(x => x.IsDefault && x.Status != CatalogPublicationStatus.Archived)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToList();
        if (activeDefaults.Count <= 1)
        {
            return;
        }

        foreach (var extra in activeDefaults.Skip(1))
        {
            extra.SetDefault(false, now);
        }
    }

    private async Task<Guid?> ResolvePrimaryCategoryIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        var categoryId = await _db.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.AssignmentId)
            .Select(x => x.CategoryId)
            .FirstOrDefaultAsync(cancellationToken);
        return categoryId == Guid.Empty ? null : categoryId;
    }

    private async Task<string> BuildCategoryPathAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken)
    {
        var categories = await _db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var byId = categories.ToDictionary(x => x.CategoryId);
        var chain = new List<Guid>();
        var current = categoryId;
        var seen = new HashSet<Guid>();
        while (byId.TryGetValue(current, out var node) && seen.Add(current))
        {
            chain.Add(current);
            if (node.ParentCategoryId is not Guid parent)
            {
                break;
            }

            current = parent;
        }

        chain.Reverse();
        var names = await GetCategoryNamesAsync(chain, locale, cancellationToken);
        return string.Join(" > ", chain.Select(id => names.GetValueOrDefault(id) ?? "رده"));
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetCategoryNamesAsync(
        IReadOnlyCollection<Guid> categoryIds,
        string locale,
        CancellationToken cancellationToken)
    {
        if (categoryIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var normalizedLocale = locale.Trim();
        var localePrefix = normalizedLocale.Split('-')[0];
        var ids = categoryIds.Distinct().ToArray();
        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Category
                && x.FieldKey == "name"
                && ids.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == normalizedLocale)
            .ThenByDescending(x => x.Locale.StartsWith(localePrefix))
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        return rows.GroupBy(x => x.OwnerId).ToDictionary(g => g.Key, g => g.First().Value);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetAttributeOptionNamesAsync(
        IReadOnlyCollection<Guid> optionIds,
        string locale,
        CancellationToken cancellationToken)
    {
        if (optionIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var normalizedLocale = locale.Trim();
        var localePrefix = normalizedLocale.Split('-')[0];
        var ids = optionIds.Distinct().ToArray();
        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.AttributeOption
                && x.FieldKey == "name"
                && ids.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == normalizedLocale)
            .ThenByDescending(x => x.Locale.StartsWith(localePrefix))
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        var names = rows.GroupBy(x => x.OwnerId).ToDictionary(g => g.Key, g => g.First().Value);
        var options = await _db.AttributeOptions.AsNoTracking()
            .Where(x => ids.Contains(x.OptionId))
            .Select(x => new { x.OptionId, x.Code })
            .ToListAsync(cancellationToken);
        foreach (var opt in options)
        {
            names.TryAdd(opt.OptionId, opt.Code);
        }

        return names;
    }

    private static (Guid? EnumOptionId, string? DisplayValue) FormatAttributeDisplay(
        CatalogAttributeValueKind kind,
        bool isMultivalue,
        string canonical,
        string? unit,
        IReadOnlyList<ProductAttributeEditorOption> options)
    {
        switch (kind)
        {
            case CatalogAttributeValueKind.Boolean:
                return (null, bool.TryParse(canonical, out var b) && b ? "بله" : "خیر");
            case CatalogAttributeValueKind.Number:
                return (null, string.IsNullOrWhiteSpace(unit) ? canonical : $"{canonical} {unit}");
            case CatalogAttributeValueKind.Enumeration:
            {
                var parts = isMultivalue
                    ? canonical.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    : [canonical];
                var labels = new List<string>();
                Guid? singleId = null;
                foreach (var part in parts)
                {
                    if (!Guid.TryParse(part, out var oid))
                    {
                        labels.Add(part);
                        continue;
                    }

                    singleId ??= oid;
                    var label = options.FirstOrDefault(o => o.OptionId == oid)?.LocalizedLabel ?? oid.ToString("N");
                    labels.Add(label);
                }

                return (isMultivalue ? null : singleId, string.Join("، ", labels));
            }
            case CatalogAttributeValueKind.Instant:
                return (null, canonical);
            default:
                return (null, canonical);
        }
    }

    private static string FormatOrphanDisplay(
        CatalogAttributeDefinition? definition,
        string canonical,
        IReadOnlyDictionary<Guid, string> optionNames)
    {
        if (definition?.ValueKind == CatalogAttributeValueKind.Enumeration)
        {
            var parts = canonical.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return string.Join("، ", parts.Select(p =>
                Guid.TryParse(p, out var oid) ? optionNames.GetValueOrDefault(oid) ?? p : p));
        }

        if (definition?.ValueKind == CatalogAttributeValueKind.Boolean
            && bool.TryParse(canonical, out var b))
        {
            return b ? "بله" : "خیر";
        }

        return canonical;
    }

    private static string ToPersianDigits(int value)
    {
        var s = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return string.Create(s.Length, s, static (span, src) =>
        {
            for (var i = 0; i < src.Length; i++)
            {
                var c = src[i];
                span[i] = c is >= '0' and <= '9' ? (char)('۰' + (c - '0')) : c;
            }
        });
    }

    private async Task EnsureDefinitionAllowedForProductSchemaAsync(
        Guid productId,
        Guid definitionId,
        CancellationToken cancellationToken)
    {
        var categoryIds = await _db.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.CategoryId)
            .ToListAsync(cancellationToken);
        if (categoryIds.Count == 0)
        {
            return;
        }

        var allowed = new HashSet<Guid>();
        foreach (var categoryId in categoryIds)
        {
            foreach (var entry in await ResolveEffectiveBindingsAsync(categoryId, cancellationToken))
            {
                allowed.Add(entry.DefinitionId);
            }
        }

        // schema-bound فقط وقتی حداقل یک binding در union مؤثر وجود دارد؛ در غیر این صورت BC آزاد.
        if (allowed.Count > 0 && !allowed.Contains(definitionId))
        {
            throw new InvalidOperationException("ویژگی در schema مؤثر رده‌های محصول نیست.");
        }
    }

    private async Task<IReadOnlyList<CatalogEffectiveSchemaBinding>> ResolveEffectiveBindingsAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var categories = await _db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var categoriesById = categories.ToDictionary(x => x.CategoryId);
        var bindings = await _db.CategoryAttributeBindings.AsNoTracking().ToListAsync(cancellationToken);
        var definitions = await _db.AttributeDefinitions.AsNoTracking().ToListAsync(cancellationToken);
        var definitionsById = definitions.ToDictionary(x => x.DefinitionId);
        return CatalogCategorySchemaResolver.ResolveEffectiveSchema(categoryId, categoriesById, bindings, definitionsById);
    }

    private async Task<IReadOnlyList<CatalogEffectiveFacetBinding>> ResolveEffectiveFacetsAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var categories = await _db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var categoriesById = categories.ToDictionary(x => x.CategoryId);
        var configs = await _db.CategoryFacetConfigurations.AsNoTracking().ToListAsync(cancellationToken);
        var effectiveSchema = await ResolveEffectiveBindingsAsync(categoryId, cancellationToken);
        var definitions = await _db.AttributeDefinitions.AsNoTracking().ToListAsync(cancellationToken);
        var definitionsById = definitions.ToDictionary(x => x.DefinitionId);
        return CatalogCategoryFacetResolver.ResolveEffectiveFacets(
            categoryId,
            categoriesById,
            configs,
            effectiveSchema,
            definitionsById);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetAttributeDefinitionNamesAsync(
        IReadOnlyCollection<Guid> definitionIds,
        string locale,
        CancellationToken cancellationToken)
    {
        if (definitionIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var normalizedLocale = locale.Trim();
        var localePrefix = normalizedLocale.Split('-')[0];
        var ids = definitionIds.Distinct().ToArray();
        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.AttributeDefinition
                && x.FieldKey == "name"
                && ids.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == normalizedLocale)
            .ThenByDescending(x => x.Locale.StartsWith(localePrefix))
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        var names = rows.GroupBy(x => x.OwnerId).ToDictionary(g => g.Key, g => g.First().Value);
        var definitions = await _db.AttributeDefinitions.AsNoTracking()
            .Where(x => ids.Contains(x.DefinitionId))
            .Select(x => new { x.DefinitionId, x.Code })
            .ToListAsync(cancellationToken);
        foreach (var def in definitions)
        {
            names.TryAdd(def.DefinitionId, def.Code);
        }

        return names;
    }

    private async Task EnsureAssignableProductCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var parentById = await _db.Categories.AsNoTracking()
            .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
        CatalogCategoryTreeRules.EnsureAssignableProductCategory(categoryId, parentById);
    }

    private async Task EnsureProductPrimaryCategoryAssignableAsync(Guid productId, CancellationToken cancellationToken)
    {
        var categoryIds = await _db.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.CategoryId)
            .ToListAsync(cancellationToken);
        if (categoryIds.Count == 0)
        {
            throw new InvalidOperationException(CatalogCategoryTreeRules.ProductAssignableLevelRequiredMessageFa);
        }

        var parentById = await _db.Categories.AsNoTracking()
            .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
        foreach (var categoryId in categoryIds)
        {
            CatalogCategoryTreeRules.EnsureAssignableProductCategory(categoryId, parentById);
        }
    }

    private static AttributeDefinitionView ToDefinitionView(CatalogAttributeDefinition definition) =>
        new(
            definition.DefinitionId,
            definition.Code,
            definition.ValueKind,
            definition.IsVariantAxisAllowed,
            definition.Unit,
            definition.IsRequired,
            definition.IsFilterable,
            definition.IsComparable,
            definition.IsMultivalue,
            definition.DisplayOrder,
            definition.ValidationMin,
            definition.ValidationMax,
            definition.ValidationMaxLength,
            definition.IsActive,
            definition.CreatedAt);

    private void AddLocalizedNames(CatalogLocalizedOwnerKind ownerKind, Guid ownerId, IReadOnlyDictionary<string, string> localizedNames)
    {
        if (localizedNames.Count == 0)
        {
            throw new InvalidOperationException("حداقل یک نام محلی برای موجودیت توصیفی Catalog لازم است.");
        }

        foreach (var pair in localizedNames)
        {
            _db.LocalizedTexts.Add(CatalogLocalizedText.Create(ownerKind, ownerId, "name", pair.Key, pair.Value));
        }
    }

    private async Task EnsureSlugAvailableAsync(
        string locale,
        string slug,
        Guid? excludeCategoryId,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = CatalogCategorySlugNormalizer.NormalizeLocale(locale);
        var normalizedSlug = CatalogCategorySlugNormalizer.NormalizeSlug(slug);
        var conflict = await _db.CategoryTranslations.AsNoTracking().AnyAsync(
            x => x.Locale == normalizedLocale
                && x.Slug == normalizedSlug
                && (excludeCategoryId == null || x.CategoryId != excludeCategoryId),
            cancellationToken);
        if (conflict)
        {
            throw new InvalidOperationException("slug رده برای این locale تکراری است.");
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

    private static void EnsureExpectedUpdatedAt(CatalogCategory category, DateTimeOffset? expectedUpdatedAt)
    {
        if (expectedUpdatedAt is { } expected
            && category.UpdatedAt != expected)
        {
            throw new InvalidOperationException("تعارض همزمانی روی رده؛ UpdatedAt تغییر کرده است.");
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

    private async Task UpsertMegaMenuTranslationAsync(
        Guid megaMenuItemId,
        string locale,
        CategoryMegaMenuBindingInput input,
        CancellationToken cancellationToken)
    {
        var existing = await _db.MegaMenuItemTranslations.SingleOrDefaultAsync(
            x => x.MegaMenuItemId == megaMenuItemId && x.Locale == locale,
            cancellationToken);
        if (existing is null)
        {
            if (string.IsNullOrWhiteSpace(input.TitleOverride)
                && string.IsNullOrWhiteSpace(input.BadgeText)
                && string.IsNullOrWhiteSpace(input.ShortLabel))
            {
                return;
            }

            _db.MegaMenuItemTranslations.Add(CatalogMegaMenuItemTranslation.Create(
                megaMenuItemId,
                locale,
                input.TitleOverride,
                input.BadgeText,
                input.ShortLabel));
            return;
        }

        existing.TitleOverride = string.IsNullOrWhiteSpace(input.TitleOverride) ? null : input.TitleOverride.Trim();
        existing.BadgeText = string.IsNullOrWhiteSpace(input.BadgeText) ? null : input.BadgeText.Trim();
        existing.ShortLabel = string.IsNullOrWhiteSpace(input.ShortLabel) ? null : input.ShortLabel.Trim();
        if (existing.TitleOverride is null && existing.BadgeText is null && existing.ShortLabel is null)
        {
            _db.MegaMenuItemTranslations.Remove(existing);
        }
    }

    private async Task<string> ResolveCategoryDisplayNameAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken)
    {
        var translation = await _db.CategoryTranslations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CategoryId == categoryId && x.Locale == locale, cancellationToken);
        if (translation is not null && !string.IsNullOrWhiteSpace(translation.Name))
        {
            return translation.Name;
        }

        var legacy = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Category
                && x.OwnerId == categoryId
                && x.FieldKey == "name"
                && x.Locale == locale)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);
        return legacy ?? "—";
    }

    private async Task<string> BuildMenuPathAsync(
        Guid megaMenuItemId,
        string locale,
        IReadOnlyDictionary<Guid, CatalogMegaMenuItem> itemsById,
        CancellationToken cancellationToken)
    {
        var segments = new List<string>();
        var current = megaMenuItemId;
        var seen = new HashSet<Guid>();
        while (itemsById.TryGetValue(current, out var item))
        {
            if (!seen.Add(current))
            {
                break;
            }

            var name = await ResolveCategoryDisplayNameAsync(item.CategoryId, locale, cancellationToken);
            segments.Add(name);
            if (item.ParentMegaMenuItemId is not Guid parent)
            {
                break;
            }

            current = parent;
        }

        segments.Reverse();
        return string.Join(" › ", segments);
    }

    private static int ComputePresentationLevel(
        Guid megaMenuItemId,
        IReadOnlyDictionary<Guid, CatalogMegaMenuItem> itemsById)
    {
        var depth = 0;
        var current = megaMenuItemId;
        var seen = new HashSet<Guid>();
        while (itemsById.TryGetValue(current, out var item))
        {
            depth++;
            if (item.ParentMegaMenuItemId is not Guid parent || !seen.Add(current))
            {
                break;
            }

            current = parent;
        }

        return depth;
    }

    private static string MapUiLocaleSegment(string locale)
    {
        if (locale.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        if (locale.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
        {
            return "ar";
        }

        return "fa";
    }

    private static string BuildUiCategoryRoute(string locale, string slug)
    {
        var clean = slug.Trim().Trim('/');
        if (string.IsNullOrWhiteSpace(clean))
        {
            return string.Empty;
        }

        return $"/{MapUiLocaleSegment(locale)}/category/{clean}";
    }
}
