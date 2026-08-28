using Microsoft.EntityFrameworkCore;
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

        var maxOrder = await _db.MediaReferences.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => (int?)x.DisplayOrder)
            .MaxAsync(cancellationToken) ?? -1;
        var isFirst = maxOrder < 0;
        _db.MediaReferences.Add(CatalogProductMediaReference.Link(
            productId,
            mediaAssetId,
            displayOrder: maxOrder + 1,
            isPrimary: isFirst,
            altText: altText));
        await _db.SaveChangesAsync(cancellationToken);
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
    public async Task<CategoryChangeImpact> ReplaceProductPrimaryCategoryAsync(
        Guid productId,
        Guid newCategoryId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
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
    public async Task PublishProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        await ValidateProductAttributesAsync(productId, cancellationToken);
        var product = await _db.Products.SingleAsync(x => x.ProductId == productId, cancellationToken);
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
            throw new InvalidOperationException("محصول والد گونه در Catalog این Tenant نیست.");
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
                    "وقتی محورهای محصول انتخاب شده‌اند، ترکیب گونه باید دقیقاً همان مجموعه‌محورها باشد.");
            }
        }

        var normalized = new List<(Guid DefinitionId, string Canonical)>();
        foreach (var axis in axes)
        {
            var definition = await _db.AttributeDefinitions.SingleAsync(x => x.DefinitionId == axis.DefinitionId, cancellationToken);
            if (!definition.IsVariantAxis)
            {
                throw new InvalidOperationException("فقط ویژگی محور Variant می‌تواند ترکیب گونه بسازد.");
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
            throw new InvalidOperationException("ترکیب محور این گونه برای همین محصول تکراری است؛ هویت Offer فروشنده نیست.");
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
}
