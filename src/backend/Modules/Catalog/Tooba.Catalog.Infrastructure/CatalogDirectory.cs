using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.Definitions.Models;
using Tooba.Catalog.Application.Attributes.Definitions.Ports;
using Tooba.Catalog.Application.Attributes.ProductValues.Ports;
using Tooba.Catalog.Application.Attributes.Schema.Ports;
using Tooba.Catalog.Application.Categories.Models;
using Tooba.Catalog.Application.Categories.Ports;
using Tooba.Catalog.Application.Facets.Ports;
using Tooba.Catalog.Application.MegaMenu.Ports;
using Tooba.Catalog.Application.Tags.Models;
using Tooba.Catalog.Application.Tags.Ports;
using Tooba.Catalog.Application.Variants.Ports;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Errors;
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
public sealed class CatalogDirectory :
    ICatalogDirectory,
    ICatalogLookupGateway,
    ICatalogVariantLookup,
    ICatalogCartQuantityPolicyGateway,
    ICatalogCartPresentationLookup
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;
    private readonly ICatalogActorContext? _actor;

    /// <summary>
    /// دایرکتوری را به DbContext Tenant-aware وصل می‌کند.
    /// </summary>
    public CatalogDirectory(CatalogDbContext db, ICatalogUseCaseGuard guard, ICatalogActorContext? actor = null)
    {
        _db = db;
        _guard = guard;
        _actor = actor;
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

    async Task<CatalogVariantLookupResult?> ICatalogVariantLookup.FindVariantAsync(
        Guid variantId,
        CancellationToken cancellationToken)
    {
        var variant = await _db.Variants.AsNoTracking()
            .SingleOrDefaultAsync(x => x.VariantId == variantId, cancellationToken);
        return variant is null ? null : new CatalogVariantLookupResult(variant.VariantId, variant.ProductId);
    }

    async Task<IReadOnlyDictionary<Guid, Guid?>> ICatalogVariantLookup.GetPrimaryCategoryIdsByVariantIdsAsync(
        IReadOnlyList<Guid> variantIds,
        CancellationToken cancellationToken) =>
        await GetPrimaryCategoryIdsByVariantIdsAsync(variantIds, cancellationToken);

    async Task<IReadOnlyDictionary<Guid, string>> ICatalogVariantLookup.GetVariantTitlesAsync(
        IReadOnlyList<Guid> variantIds,
        CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var variants = await _db.Variants.AsNoTracking()
            .Where(x => variantIds.Contains(x.VariantId))
            .Select(x => new { x.VariantId, x.ProductId })
            .ToListAsync(cancellationToken);
        var productIds = variants.Select(x => x.ProductId).Distinct().ToList();
        var names = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                && productIds.Contains(x.OwnerId)
                && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        var productNames = names.GroupBy(x => x.OwnerId).ToDictionary(
            x => x.Key,
            x => x.OrderBy(row => row.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? 0 : 1).First().Value);
        return variants
            .Where(x => productNames.ContainsKey(x.ProductId))
            .ToDictionary(x => x.VariantId, x => productNames[x.ProductId]);
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
            .Where(pc => productIds.Contains(pc.ProductId) && pc.Role == CatalogProductCategoryRole.Primary)
            .Select(pc => new { pc.ProductId, pc.CategoryId })
            .ToListAsync(cancellationToken);
        var primaryByProduct = categoryLinks
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => (Guid?)g.First().CategoryId);

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
    public async Task<EffectiveQuantityPolicy?> GetEffectiveQuantityPolicyForVariantAsync(
        Guid variantId,
        CancellationToken cancellationToken)
    {
        var map = await GetEffectiveQuantityPoliciesForVariantIdsAsync([variantId], cancellationToken);
        return map.TryGetValue(variantId, out var policy) ? policy : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, EffectiveQuantityPolicy>> GetEffectiveQuantityPoliciesForVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0)
        {
            return new Dictionary<Guid, EffectiveQuantityPolicy>();
        }

        var distinct = variantIds.Distinct().ToArray();
        var variants = await _db.Variants.AsNoTracking()
            .Where(v => distinct.Contains(v.VariantId))
            .Select(v => new { v.VariantId, v.ProductId })
            .ToListAsync(cancellationToken);
        if (variants.Count == 0)
        {
            return new Dictionary<Guid, EffectiveQuantityPolicy>();
        }

        var productIds = variants.Select(v => v.ProductId).Distinct().ToArray();
        var products = await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.ProductId))
            .Select(p => new { p.ProductId, p.UnitOfMeasureId, p.QuantityDecimalPlaces, p.QuantityStep })
            .ToListAsync(cancellationToken);
        var unitIds = products.Select(p => p.UnitOfMeasureId).Distinct().ToArray();
        var units = await _db.UnitsOfMeasure.AsNoTracking()
            .Where(u => unitIds.Contains(u.UnitOfMeasureId))
            .ToListAsync(cancellationToken);
        var translations = await _db.UnitOfMeasureTranslations.AsNoTracking()
            .Where(t => unitIds.Contains(t.UnitOfMeasureId))
            .ToListAsync(cancellationToken);
        var settings = await _db.StoreQuantitySettings.AsNoTracking()
            .SingleOrDefaultAsync(s => s.SettingsId == StoreQuantitySettings.SingletonId, cancellationToken);
        var rounding = settings?.RoundingMode ?? QuantityRoundingMode.Nearest;

        var languagePreference = await LoadPreferredLanguageIdsAsync(cancellationToken);
        var unitById = units.ToDictionary(u => u.UnitOfMeasureId);
        var translationsByUnit = translations.GroupBy(t => t.UnitOfMeasureId).ToDictionary(g => g.Key, g => g.ToList());
        var productById = products.ToDictionary(p => p.ProductId);

        var result = new Dictionary<Guid, EffectiveQuantityPolicy>();
        foreach (var variant in variants)
        {
            if (!productById.TryGetValue(variant.ProductId, out var product))
            {
                continue;
            }

            unitById.TryGetValue(product.UnitOfMeasureId, out var unit);
            translationsByUnit.TryGetValue(product.UnitOfMeasureId, out var unitTranslations);
            var picked = PickTranslation(unitTranslations, languagePreference);
            var code = unit?.Code ?? "pcs";
            result[variant.VariantId] = new EffectiveQuantityPolicy(
                product.ProductId,
                product.UnitOfMeasureId,
                code,
                picked?.Name ?? code,
                picked?.ShortName ?? code,
                product.QuantityDecimalPlaces,
                product.QuantityStep,
                rounding);
        }

        return result;
    }

    /// <inheritdoc />
    async Task<IReadOnlyDictionary<Guid, EffectiveQuantityPolicy>> ICatalogCartPresentationLookup.GetEffectiveQuantityPoliciesForVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken) =>
        await GetEffectiveQuantityPoliciesForVariantIdsAsync(variantIds, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, CatalogCartVariantPresentation>> GetVariantPresentationsAsync(
        IReadOnlyList<Guid> variantIds,
        CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0)
        {
            return new Dictionary<Guid, CatalogCartVariantPresentation>();
        }

        var distinct = variantIds.Distinct().ToArray();
        var variants = await _db.Variants.AsNoTracking()
            .Where(item => distinct.Contains(item.VariantId))
            .Select(item => new { item.VariantId, item.ProductId })
            .ToListAsync(cancellationToken);
        if (variants.Count == 0)
        {
            return new Dictionary<Guid, CatalogCartVariantPresentation>();
        }

        var productIds = variants.Select(item => item.ProductId).Distinct().ToList();
        var products = await _db.Products.AsNoTracking()
            .Where(item => productIds.Contains(item.ProductId))
            .Select(item => new { item.ProductId, item.SlugSeam })
            .ToListAsync(cancellationToken);
        var productMap = products.ToDictionary(item => item.ProductId);
        var names = await GetProductTitlesAsync(productIds, cancellationToken);
        var media = await _db.MediaReferences.AsNoTracking()
            .Where(item => productIds.Contains(item.ProductId))
            .Select(item => new { item.ProductId, item.MediaAssetId })
            .ToListAsync(cancellationToken);
        var mediaMap = media
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.MediaAssetId).FirstOrDefault());

        var result = new Dictionary<Guid, CatalogCartVariantPresentation>();
        foreach (var variant in variants)
        {
            productMap.TryGetValue(variant.ProductId, out var product);
            var title = names.GetValueOrDefault(variant.ProductId)
                ?? product?.SlugSeam
                ?? "کالا";
            var slug = product is null || string.IsNullOrWhiteSpace(product.SlugSeam)
                ? (product?.ProductId.ToString("N") ?? variant.ProductId.ToString("N"))
                : product.SlugSeam;
            Guid? mediaId = mediaMap.GetValueOrDefault(variant.ProductId);
            if (mediaId == Guid.Empty)
            {
                mediaId = null;
            }

            result[variant.VariantId] = new CatalogCartVariantPresentation(
                variant.VariantId,
                variant.ProductId,
                slug,
                title,
                mediaId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<QuantityRoundingMode> GetGlobalRoundingModeAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.StoreQuantitySettings.AsNoTracking()
            .SingleOrDefaultAsync(s => s.SettingsId == StoreQuantitySettings.SingletonId, cancellationToken);
        return settings?.RoundingMode ?? QuantityRoundingMode.Nearest;
    }

    private async Task<IReadOnlyList<Guid>> LoadPreferredLanguageIdsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _db.Database
                .SqlQuery<LanguageCodeRow>(
                    $"""
                     SELECT language_id AS "LanguageId", code AS "Code"
                     FROM localization.languages
                     """)
                .ToListAsync(cancellationToken);
            return rows
                .OrderBy(r => LanguageRank(r.Code))
                .Select(r => r.LanguageId)
                .ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static int LanguageRank(string? code)
    {
        var normalized = (code ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized is "fa" or "fa-ir") return 0;
        if (normalized.StartsWith("fa", StringComparison.Ordinal)) return 1;
        if (normalized is "en" or "en-us") return 2;
        if (normalized.StartsWith("en", StringComparison.Ordinal)) return 3;
        return 8;
    }

    private static UnitOfMeasureTranslation? PickTranslation(
        IReadOnlyList<UnitOfMeasureTranslation>? translations,
        IReadOnlyList<Guid> preferredLanguageIds)
    {
        if (translations is null || translations.Count == 0)
        {
            return null;
        }

        foreach (var languageId in preferredLanguageIds)
        {
            var match = translations.FirstOrDefault(t => t.LanguageId == languageId);
            if (match is not null)
            {
                return match;
            }
        }

        return translations[0];
    }

    private sealed class LanguageCodeRow
    {
        public Guid LanguageId { get; set; }
        public string Code { get; set; } = string.Empty;
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
    /// <inheritdoc />
    public async Task<CategoryReference> CreateCategoryAsync(
        Guid? parentCategoryId,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken)
    {
        var names = localizedNames as Dictionary<string, string>
            ?? localizedNames.ToDictionary(x => x.Key, x => x.Value);
        var result = await CategoriesPort().CreateAsync(
            new CreateCategoryWriteModel(
                parentCategoryId,
                SortOrder: 0,
                IsVisible: true,
                ImageMediaAssetId: null,
                IconMediaAssetId: null,
                BannerMediaAssetId: null,
                Translations: null,
                LocalizedNames: names),
            cancellationToken);
        return Unwrap(result);
    }

    /// <inheritdoc />
    public async Task<CategoryReference> CreateCategoryAsync(
        CategoryCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await CategoriesPort().CreateAsync(
            new CreateCategoryWriteModel(
                request.ParentCategoryId,
                request.SortOrder,
                request.IsVisible,
                request.ImageMediaAssetId,
                request.IconMediaAssetId,
                request.BannerMediaAssetId,
                request.Translations.ToList(),
                LocalizedNames: null),
            cancellationToken);
        return Unwrap(result);
    }

    /// <inheritdoc />
    public async Task UpdateCategoryCoreAsync(
        Guid categoryId,
        CategoryCoreUpdateRequest request,
        CancellationToken cancellationToken) =>
        Unwrap(await CategoriesPort().UpdateCoreAsync(categoryId, request, cancellationToken));

    /// <inheritdoc />
    public async Task<CategoryTranslationDto> UpsertCategoryTranslationAsync(
        Guid categoryId,
        CategoryTranslationUpsertRequest request,
        CancellationToken cancellationToken) =>
        Unwrap(await CategoriesPort().UpsertTranslationAsync(categoryId, request, cancellationToken));

    /// <inheritdoc />
    public async Task MoveCategoryAsync(
        Guid categoryId,
        Guid? newParentId,
        DateTimeOffset? expectedUpdatedAt,
        CancellationToken cancellationToken) =>
        Unwrap(await CategoriesPort().MoveAsync(categoryId, newParentId, expectedUpdatedAt, cancellationToken));

    /// <inheritdoc />
    public async Task ReorderCategorySiblingsAsync(
        Guid? parentId,
        IReadOnlyList<Guid> orderedCategoryIds,
        CancellationToken cancellationToken) =>
        Unwrap(await CategoriesPort().ReorderAsync(parentId, orderedCategoryIds, cancellationToken));

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryTreeNodeDto>> GetCategoryTreeAsync(
        string locale,
        string? search,
        CancellationToken cancellationToken) =>
        Unwrap(await CategoriesPort().GetTreeAsync(locale, search, cancellationToken));

    /// <inheritdoc />
    public async Task<CategoryWorkspaceSummaryDto?> GetCategoryWorkspaceAsync(
        Guid categoryId,
        string? locale,
        CancellationToken cancellationToken)
    {
        var result = await CategoriesPort().GetWorkspaceAsync(categoryId, locale, cancellationToken);
        if (result.IsFailure && result.FirstError.Code == CatalogErrorCodes.CategoryMissing)
        {
            return null;
        }

        return Unwrap(result);
    }

    /// <inheritdoc />
    public async Task<CategoryRouteResolveResult?> ResolveCategoryRouteAsync(
        string locale,
        string slug,
        bool forStorefront,
        CancellationToken cancellationToken)
    {
        var result = await CategoriesPort().ResolveRouteAsync(locale, slug, forStorefront, cancellationToken);
        if (result.IsFailure && result.FirstError.Code == CatalogErrorCodes.CategoryRouteMissing)
        {
            return null;
        }

        return Unwrap(result);
    }

    /// <inheritdoc />
    public async Task ArchiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
        Unwrap(await CategoriesPort().ArchiveAsync(categoryId, cancellationToken));

    /// <inheritdoc />
    public async Task PublishCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
        Unwrap(await CategoriesPort().PublishAsync(categoryId, cancellationToken));
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
    public async Task<TagView> CreateTagAsync(
        string? code,
        string? slugSeam,
        IReadOnlyDictionary<string, string> localizedNames,
        string? displayLocale,
        CancellationToken cancellationToken)
    {
        var result = await TagsPort().CreateAsync(
            new CreateTagWriteModel(code, slugSeam, displayLocale, localizedNames),
            cancellationToken);
        return Unwrap(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TagView>> ListTagsAsync(
        string locale,
        string? search,
        CancellationToken cancellationToken) =>
        TagsPort().ListAsync(locale, search, cancellationToken);

    /// <inheritdoc />
    public async Task<TagView?> GetTagAsync(Guid tagId, string? locale, CancellationToken cancellationToken)
    {
        var result = await TagsPort().GetAsync(tagId, locale, cancellationToken);
        return result.IsSuccess ? result.Value : null;
    }

    /// <inheritdoc />
    public async Task PublishTagAsync(Guid tagId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var tag = await _db.Tags.SingleAsync(x => x.TagId == tagId, cancellationToken);
        tag.Publish(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AssignProductTagAsync(Guid productId, Guid tagId, CancellationToken cancellationToken)
    {
        Unwrap(await TagsPort().AssignProductTagAsync(productId, tagId, cancellationToken));
    }

    /// <inheritdoc />
    public async Task RemoveProductTagAsync(Guid productId, Guid tagId, CancellationToken cancellationToken)
    {
        Unwrap(await TagsPort().RemoveProductTagAsync(productId, tagId, cancellationToken));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TagView>> ListProductTagsAsync(
        Guid productId,
        string? locale,
        CancellationToken cancellationToken) =>
        TagsPort().ListProductTagsAsync(productId, locale, cancellationToken);

    /// <inheritdoc />
    public async Task AssignCategoryTagAsync(Guid categoryId, Guid tagId, CancellationToken cancellationToken)
    {
        Unwrap(await TagsPort().AssignCategoryTagAsync(categoryId, tagId, cancellationToken));
    }

    /// <inheritdoc />
    public async Task RemoveCategoryTagAsync(Guid categoryId, Guid tagId, CancellationToken cancellationToken)
    {
        Unwrap(await TagsPort().RemoveCategoryTagAsync(categoryId, tagId, cancellationToken));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TagView>> ListCategoryTagsAsync(
        Guid categoryId,
        string? locale,
        CancellationToken cancellationToken) =>
        TagsPort().ListCategoryTagsAsync(categoryId, locale, cancellationToken);

    private ITagDirectory TagsPort() => new TagDirectory(_db, _guard);

    private IMegaMenuDirectory MegaMenuPort() => new MegaMenuDirectory(_db, _guard);

    private IFacetDirectory FacetPort() => new FacetDirectory(_db, _guard);

    private ICategoryDirectory CategoriesPort() => new CategoryDirectory(_db, _guard);

    private IAttributeDefinitionDirectory AttributeDefinitionsPort() =>
        new AttributeDefinitionDirectory(_db, _guard);

    private ICategoryAttributeSchemaDirectory SchemaPort() =>
        new CategoryAttributeSchemaDirectory(_db, _guard);

    private IProductAttributeDirectory ProductAttributesPort() =>
        new ProductAttributeDirectory(_db, _guard, _actor);

    private IProductVariantDirectory ProductVariantsPort() =>
        new ProductVariantDirectory(_db, _guard, _actor);

    private static T Unwrap<T>(Result<T> result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.FirstError.Code);
        }

        return result.Value;
    }

    private static void Unwrap(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.FirstError.Code);
        }
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAttributeDefinitionAsync(
        string code,
        CatalogAttributeValueKind valueKind,
        bool isVariantAxis,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken)
    {
        var result = await AttributeDefinitionsPort().CreateAsync(
            code,
            valueKind,
            isVariantAxis,
            localizedNames,
            metadata: null,
            cancellationToken);
        return Unwrap(result).DefinitionId;
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
        _ = Unwrap(await AttributeDefinitionsPort().UpdateMetadataAsync(
            definitionId,
            new AttributeDefinitionMetadataWriteModel(
                unit,
                isRequired,
                isFilterable,
                isComparable,
                isMultivalue,
                displayOrder,
                validationMin,
                validationMax,
                validationMaxLength,
                isActive),
            cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AttributeDefinitionView>> ListAttributeDefinitionsAsync(
        CancellationToken cancellationToken) =>
        Unwrap(await AttributeDefinitionsPort().ListAsync(cancellationToken));

    /// <inheritdoc />
    public async Task<AttributeDefinitionView?> GetAttributeDefinitionAsync(
        Guid definitionId,
        CancellationToken cancellationToken)
    {
        var result = await AttributeDefinitionsPort().GetAsync(definitionId, cancellationToken);
        if (result.IsFailure && result.FirstError.Code == CatalogErrorCodes.AttributeMissing)
        {
            return null;
        }

        return Unwrap(result);
    }

    /// <inheritdoc />
    public async Task<VariantAxisCapabilityDisableImpactView> PreviewVariantAxisCapabilityDisableImpactAsync(
        Guid definitionId,
        CancellationToken cancellationToken) =>
        Unwrap(await AttributeDefinitionsPort().PreviewVariantAxisCapabilityDisableAsync(
            definitionId,
            cancellationToken));

    /// <inheritdoc />
    public async Task SetAttributeDefinitionVariantAxisCapabilityAsync(
        Guid definitionId,
        bool isVariantAxisAllowed,
        CancellationToken cancellationToken)
    {
        _ = Unwrap(await AttributeDefinitionsPort().SetVariantAxisCapabilityAsync(
            definitionId,
            isVariantAxisAllowed,
            cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Guid> AddAttributeOptionAsync(
        Guid definitionId,
        string code,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken) =>
        Unwrap(await AttributeDefinitionsPort().AddOptionAsync(
            definitionId,
            code,
            localizedNames,
            cancellationToken)).OptionId;

    /// <inheritdoc />
    public async Task BindCategoryAttributeAsync(
        Guid categoryId,
        Guid definitionId,
        int displayOrder,
        CategoryAttributeAssignmentFlags flags,
        CancellationToken cancellationToken) =>
        _ = Unwrap(await SchemaPort().BindAsync(
            categoryId,
            definitionId,
            displayOrder,
            flags,
            cancellationToken));

    /// <inheritdoc />
    public async Task UpdateCategoryAttributeBindingAsync(
        Guid categoryId,
        Guid definitionId,
        CategoryAttributeAssignmentFlags flags,
        CancellationToken cancellationToken) =>
        _ = Unwrap(await SchemaPort().UpdateBindingAsync(
            categoryId,
            definitionId,
            flags,
            cancellationToken));

    /// <inheritdoc />
    public async Task UnbindCategoryAttributeAsync(
        Guid categoryId,
        Guid definitionId,
        CancellationToken cancellationToken) =>
        _ = Unwrap(await SchemaPort().UnbindAsync(categoryId, definitionId, cancellationToken));

    /// <inheritdoc />
    public async Task ReorderCategoryAttributeBindingsAsync(
        Guid categoryId,
        IReadOnlyList<Guid> orderedDefinitionIds,
        CancellationToken cancellationToken) =>
        _ = Unwrap(await SchemaPort().ReorderAsync(categoryId, orderedDefinitionIds, cancellationToken));

    /// <inheritdoc />
    public async Task<IReadOnlyList<EffectiveSchemaEntry>> GetEffectiveCategorySchemaAsync(
        Guid categoryId,
        CancellationToken cancellationToken) =>
        Unwrap(await SchemaPort().GetEffectiveAsync(categoryId, cancellationToken));

    /// <inheritdoc />
    public async Task<IReadOnlyList<EffectiveCategoryFacet>> GetEffectiveCategoryFacetsAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken) =>
        Unwrap(await FacetPort().GetEffectiveFacetsAsync(categoryId, locale, cancellationToken));

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryFacetConfigurationView>> ListLocalFacetConfigurationsAsync(
        Guid categoryId,
        CancellationToken cancellationToken) =>
        Unwrap(await FacetPort().ListLocalConfigurationsAsync(categoryId, cancellationToken));

    /// <inheritdoc />
    public async Task UpsertCategoryFacetConfigurationAsync(
        Guid categoryId,
        Guid definitionId,
        CategoryFacetConfigurationInput input,
        CancellationToken cancellationToken) =>
        Unwrap(await FacetPort().UpsertConfigurationAsync(categoryId, definitionId, input, cancellationToken));

    /// <inheritdoc />
    public async Task RemoveCategoryFacetOverrideAsync(
        Guid categoryId,
        Guid definitionId,
        CancellationToken cancellationToken) =>
        Unwrap(await FacetPort().RemoveOverrideAsync(categoryId, definitionId, cancellationToken));

    /// <inheritdoc />
    public async Task ReorderCategoryFacetConfigurationsAsync(
        Guid categoryId,
        IReadOnlyList<Guid> orderedDefinitionIds,
        CancellationToken cancellationToken) =>
        Unwrap(await FacetPort().ReorderConfigurationsAsync(categoryId, orderedDefinitionIds, cancellationToken));

    /// <inheritdoc />
    public async Task<CategoryMegaMenuConfigurationView> GetCategoryMegaMenuConfigurationAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken) =>
        Unwrap(await MegaMenuPort().GetCategoryConfigurationAsync(categoryId, locale, cancellationToken));

    /// <inheritdoc />
    public async Task UpsertCategoryMegaMenuBindingAsync(
        Guid categoryId,
        string locale,
        CategoryMegaMenuBindingInput input,
        CancellationToken cancellationToken) =>
        Unwrap(await MegaMenuPort().UpsertBindingAsync(categoryId, locale, input, cancellationToken));

    /// <inheritdoc />
    public async Task RemoveCategoryMegaMenuBindingAsync(Guid categoryId, CancellationToken cancellationToken) =>
        Unwrap(await MegaMenuPort().RemoveBindingAsync(categoryId, cancellationToken));

    /// <inheritdoc />
    public Task<IReadOnlyList<MegaMenuPlacementOption>> ListMegaMenuPlacementOptionsAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken) =>
        MegaMenuPort().ListPlacementOptionsAsync(categoryId, locale, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<StorefrontMegaMenuItem>> GetStorefrontMegaMenuAsync(
        string locale,
        CancellationToken cancellationToken) =>
        MegaMenuPort().GetStorefrontMenuAsync(locale, cancellationToken);

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
        QueueProductHistory(
            product.ProductId,
            ProductHistoryRules.EventCreated,
            ProductHistoryRules.SectionGeneral,
            ProductHistoryRules.SummaryCreatedFa,
            null,
            product.SlugSeam);
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

        var links = await _db.ProductCategories.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        if (links.Any(x => x.Role == CatalogProductCategoryRole.Primary))
        {
            throw new InvalidOperationException("محصول هم‌اکنون دسته اصلی دارد؛ برای تغییر از Replace استفاده کنید.");
        }

        var asAdditional = links.FirstOrDefault(x => x.CategoryId == categoryId);
        if (asAdditional is not null)
        {
            _db.ProductCategories.Remove(asAdditional);
        }

        _db.ProductCategories.Add(CatalogProductCategory.Assign(productId, categoryId, CatalogProductCategoryRole.Primary));
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventCategoryChanged,
            ProductHistoryRules.SectionCategory,
            ProductHistoryRules.SummaryCategoryFa,
            null,
            null);
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
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventMediaChanged,
            ProductHistoryRules.SectionMedia,
            ProductHistoryRules.SummaryMediaFa,
            null,
            null);
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
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventMediaChanged,
            ProductHistoryRules.SectionMedia,
            ProductHistoryRules.SummaryMediaFa,
            null,
            null);
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
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventMediaChanged,
            ProductHistoryRules.SectionMedia,
            ProductHistoryRules.SummaryMediaPrimaryFa,
            null,
            null);
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
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventMediaChanged,
            ProductHistoryRules.SectionMedia,
            ProductHistoryRules.SummaryMediaFa,
            null,
            null);
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
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventSeoChanged,
            ProductHistoryRules.SectionSeo,
            ProductHistoryRules.SummarySeoFa,
            null,
            slug);
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
        CancellationToken cancellationToken) =>
        Unwrap(await ProductAttributesPort().SetSingleAsync(
            productId,
            definitionId,
            rawValue,
            enumOptionId,
            cancellationToken));

    /// <inheritdoc />
    public async Task<ProductAttributeEditorState> GetProductAttributeEditorStateAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken) =>
        Unwrap(await ProductAttributesPort().GetEditorStateAsync(productId, locale, cancellationToken));

    /// <inheritdoc />
    public async Task SetProductAttributesAsync(
        Guid productId,
        IReadOnlyList<ProductAttributeValueInput> values,
        CancellationToken cancellationToken)
    {
        _ = Unwrap(await ProductAttributesPort().SetBulkAsync(
            productId,
            values,
            "fa-IR",
            cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ProductAttributeReadiness> GetProductAttributeReadinessAsync(
        Guid productId,
        CancellationToken cancellationToken) =>
        Unwrap(await ProductAttributesPort().GetReadinessAsync(productId, cancellationToken));

    /// <inheritdoc />
    public async Task SetProductVariantAxesAsync(
        Guid productId,
        IReadOnlyList<Guid> orderedDefinitionIds,
        CancellationToken cancellationToken) =>
        Unwrap(await ProductVariantsPort().SetAxesAsync(productId, orderedDefinitionIds, cancellationToken));

    /// <inheritdoc />
    public async Task<ProductVariantEditorState> GetProductVariantEditorStateAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken) =>
        Unwrap(await ProductVariantsPort().GetEditorStateAsync(productId, locale, cancellationToken));

    /// <inheritdoc />
    public async Task<ProductVariantPreviewResult> PreviewProductVariantCombinationsAsync(
        Guid productId,
        IReadOnlyList<ProductVariantSelectedAxisInput> selectedAxes,
        string locale,
        CancellationToken cancellationToken) =>
        Unwrap(await ProductVariantsPort().PreviewCombinationsAsync(productId, selectedAxes, locale, cancellationToken));

    /// <inheritdoc />
    public async Task<ProductVariantApplyResult> ApplyProductVariantMatrixAsync(
        Guid productId,
        ProductVariantApplyInput input,
        CancellationToken cancellationToken) =>
        Unwrap(await ProductVariantsPort().ApplyMatrixAsync(productId, input, cancellationToken));

    /// <inheritdoc />
    public async Task<ProductVariantReadiness> GetProductVariantReadinessAsync(
        Guid productId,
        CancellationToken cancellationToken) =>
        Unwrap(await ProductVariantsPort().GetReadinessAsync(productId, cancellationToken));

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
        var compatibleValues = values.Where(v => allowed.Contains(v.DefinitionId)).ToList();
        var compatiblePreserved = compatibleValues.Count;

        var presentIds = values.Select(v => v.DefinitionId).ToHashSet();
        var newlyRequired = newSchema
            .Where(x => x.IsRequired && !x.IsVariantAxis && x.Definition.IsActive && !presentIds.Contains(x.DefinitionId))
            .ToList();

        var orphanDefIds = impact.OrphanAttributeValues.Select(x => x.DefinitionId).Distinct().ToArray();
        var preservedDefIds = compatibleValues.Select(v => v.DefinitionId).Distinct().ToArray();
        var labelIds = orphanDefIds
            .Concat(newlyRequired.Select(x => x.DefinitionId))
            .Concat(preservedDefIds)
            .Distinct()
            .ToArray();
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
        var preservedAttributes = preservedDefIds
            .Select(id => names.GetValueOrDefault(id) ?? id.ToString("N"))
            .ToList();
        var removedAttributes = orphanSummaries
            .Select(o => string.IsNullOrWhiteSpace(o.DisplayValue)
                ? o.LocalizedName
                : $"{o.LocalizedName}: {o.DisplayValue}")
            .ToList();

        var activeVariantCount = await _db.Variants.AsNoTracking()
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
        var variantCompatible = !axesChanged;
        var variantImpactCount = axesChanged ? activeVariantCount : 0;
        var preservedVariantCount = variantCompatible ? activeVariantCount : 0;
        var affectedVariantCount = variantCompatible ? 0 : activeVariantCount;
        var variantImpactFa = variantImpactCount > 0
            ? $"{ToPersianDigits(variantImpactCount)} تنوع تحت تأثیر تغییر محورها قرار می‌گیرد و حذف خودکار نمی‌شود"
            : null;

        var currentCategoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
        string? currentCategoryPath = null;
        if (currentCategoryId is Guid currentId)
        {
            currentCategoryPath = await BuildCategoryPathAsync(currentId, normalizedLocale, cancellationToken);
        }

        var targetCategoryPath = await BuildCategoryPathAsync(newCategoryId, normalizedLocale, cancellationToken);

        var memberships = await _db.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var additionalMembershipPromoted = memberships.Any(
            x => x.CategoryId == newCategoryId && x.Role == CatalogProductCategoryRole.Additional);
        var otherDisplayRemainCount = memberships.Count(
            x => x.Role == CatalogProductCategoryRole.Additional && x.CategoryId != newCategoryId);

        var readinessBlockers = new List<string>();
        foreach (var label in newlyRequiredLabels)
        {
            readinessBlockers.Add($"ویژگی الزامی «{label}» باید تکمیل شود");
        }

        if (!variantCompatible && affectedVariantCount > 0)
        {
            readinessBlockers.Add(
                $"{ToPersianDigits(affectedVariantCount)} تنوع نیاز به بازبینی دارند و آمادگی انتشار برقرار نیست");
        }

        var productStatus = await _db.Products.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => x.Status)
            .FirstOrDefaultAsync(cancellationToken);
        var structuralIncompatibility = impact.OrphanAttributeValues.Count > 0
            || newlyRequired.Count > 0
            || !variantCompatible;
        if (productStatus == CatalogPublicationStatus.Published && structuralIncompatibility)
        {
            readinessBlockers.Add("محصول پس از مهاجرت به‌خاطر ناسازگاری ساختاری از انتشار خارج می‌شود");
        }

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

        if (additionalMembershipPromoted)
        {
            messageParts.Add("دسته هدف از «نمایش در این دسته» به دسته اصلی ارتقا می‌یابد");
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
            variantImpactFa,
            currentCategoryId,
            currentCategoryPath,
            targetCategoryPath,
            preservedAttributes,
            newlyRequiredLabels,
            removedAttributes,
            newlyRequiredLabels,
            variantCompatible,
            preservedVariantCount,
            affectedVariantCount,
            additionalMembershipPromoted,
            otherDisplayRemainCount,
            readinessBlockers);
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
        var preview = await PreviewCategoryChangeReportAsync(productId, newCategoryId, "fa-IR", cancellationToken);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var existing = await _db.ProductCategories.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);

            // اگر دسته جدید قبلاً Additional بود، ردیف را بردار تا Unique (ProductId, CategoryId) نشکند (ارتقا اتمی).
            var existingAsAdditional = existing.FirstOrDefault(
                x => x.CategoryId == newCategoryId && x.Role == CatalogProductCategoryRole.Additional);
            if (existingAsAdditional is not null)
            {
                _db.ProductCategories.Remove(existingAsAdditional);
                existing.Remove(existingAsAdditional);
            }

            var currentPrimary = existing.FirstOrDefault(x => x.Role == CatalogProductCategoryRole.Primary);
            if (currentPrimary is not null)
            {
                if (currentPrimary.CategoryId == newCategoryId)
                {
                    await tx.CommitAsync(cancellationToken);
                    return impact;
                }

                _db.ProductCategories.Remove(currentPrimary);
            }

            _db.ProductCategories.Add(
                CatalogProductCategory.Assign(productId, newCategoryId, CatalogProductCategoryRole.Primary));

            // حذف orphanها فقط با تطبیق AttributeDefinition ID — بدون ساخت مقدار جعلی برای الزامی‌های جدید.
            var orphanDefIds = impact.OrphanAttributeValues.Select(x => x.DefinitionId).ToHashSet();
            if (orphanDefIds.Count > 0)
            {
                var orphanRows = await _db.ProductAttributeValues
                    .Where(x => x.ProductId == productId && orphanDefIds.Contains(x.DefinitionId))
                    .ToListAsync(cancellationToken);
                _db.ProductAttributeValues.RemoveRange(orphanRows);
            }

            var now = DateTimeOffset.UtcNow;
            var newSchema = await ResolveEffectiveBindingsAsync(newCategoryId, cancellationToken);
            var newAxisIds = newSchema.Where(x => x.IsVariantAxis).Select(x => x.DefinitionId).ToHashSet();
            var currentAxes = await _db.ProductVariantAxes
                .Where(x => x.ProductId == productId)
                .ToListAsync(cancellationToken);
            var axesChanged = !preview.VariantCompatible;
            var invalidAxes = currentAxes.Where(a => !newAxisIds.Contains(a.DefinitionId)).ToList();
            if (invalidAxes.Count > 0)
            {
                _db.ProductVariantAxes.RemoveRange(invalidAxes);
            }

            if (axesChanged)
            {
                var variants = await _db.Variants
                    .Where(x => x.ProductId == productId && x.Status != CatalogPublicationStatus.Archived)
                    .ToListAsync(cancellationToken);
                ClearDefaultFlags(variants, now);
                foreach (var variant in variants)
                {
                    // ترکیب‌های تجاری را حذف نمی‌کنیم؛ فقط Draft + نیاز به بازبینی.
                    if (variant.Status != CatalogPublicationStatus.Draft)
                    {
                        variant.SetStatus(CatalogPublicationStatus.Draft, now);
                    }
                    else
                    {
                        variant.UpdatedAt = now;
                    }
                }
            }

            var structuralIncompatibility = impact.OrphanAttributeValues.Count > 0
                || preview.NewlyRequiredMissingCount > 0
                || axesChanged;
            var unpublishedForSafety = false;
            var product = await _db.Products.SingleAsync(x => x.ProductId == productId, cancellationToken);
            if (product.Status == CatalogPublicationStatus.Published && structuralIncompatibility)
            {
                product.Unpublish(now);
                unpublishedForSafety = true;
                QueueProductHistory(
                    productId,
                    ProductHistoryRules.EventUnpublished,
                    ProductHistoryRules.SectionLifecycle,
                    ProductHistoryRules.SummaryUnpublishedByMigrationFa,
                    ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Published),
                    ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Draft));
            }

            var beforePath = preview.CurrentCategoryPath ?? "بدون دسته اصلی";
            var afterSummary = ProductHistoryRules.FormatCategoryMigrationAfterSummaryFa(
                preview.TargetCategoryPath ?? "دسته جدید",
                preview.CompatiblePreservedCount,
                preview.NewlyRequiredMissingCount,
                preview.OrphanCount,
                preview.AffectedVariantCount,
                unpublishedForSafety);
            QueueProductHistory(
                productId,
                ProductHistoryRules.EventCategoryChanged,
                ProductHistoryRules.SectionCategory,
                ProductHistoryRules.SummaryCategoryMigrationFa,
                beforePath,
                afterSummary);

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return impact;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task AddProductAdditionalCategoryAsync(
        Guid productId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken)
            || !await _db.Categories.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            throw new InvalidOperationException("محصول یا رده در Catalog این Tenant نیست.");
        }

        await EnsureAssignableProductCategoryAsync(categoryId, cancellationToken);

        var links = await _db.ProductCategories.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        if (links.Any(x => x.CategoryId == categoryId && x.Role == CatalogProductCategoryRole.Primary))
        {
            throw new InvalidOperationException("این دسته هم‌اکنون دسته اصلی محصول است.");
        }

        if (links.Any(x => x.CategoryId == categoryId && x.Role == CatalogProductCategoryRole.Additional))
        {
            throw new InvalidOperationException("این دسته قبلاً به‌عنوان دسته اضافی اضافه شده است.");
        }

        _db.ProductCategories.Add(
            CatalogProductCategory.Assign(productId, categoryId, CatalogProductCategoryRole.Additional));
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventCategoryChanged,
            ProductHistoryRules.SectionCategory,
            ProductHistoryRules.SummaryCategoryFa,
            null,
            null);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveProductAdditionalCategoryAsync(
        Guid productId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var link = await _db.ProductCategories.FirstOrDefaultAsync(
            x => x.ProductId == productId && x.CategoryId == categoryId,
            cancellationToken);
        if (link is null)
        {
            throw new InvalidOperationException("پیوند دسته برای این محصول یافت نشد.");
        }

        if (link.Role == CatalogProductCategoryRole.Primary)
        {
            throw new InvalidOperationException(
                "دسته اصلی را نمی‌توان مستقیم حذف کرد؛ ابتدا دسته اصلی دیگری انتخاب کنید.");
        }

        _db.ProductCategories.Remove(link);
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventCategoryChanged,
            ProductHistoryRules.SectionCategory,
            ProductHistoryRules.SummaryCategoryFa,
            null,
            null);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductCategoryAssignmentInfo>> ListProductCategoryAssignmentsAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken)
    {
        var links = await _db.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.Role)
            .ThenBy(x => x.AssignmentId)
            .ToListAsync(cancellationToken);
        if (links.Count == 0)
        {
            return [];
        }

        var result = new List<ProductCategoryAssignmentInfo>(links.Count);
        foreach (var link in links)
        {
            var path = await BuildCategoryPathAsync(link.CategoryId, locale, cancellationToken);
            result.Add(new ProductCategoryAssignmentInfo(
                link.CategoryId,
                path,
                link.Role == CatalogProductCategoryRole.Primary
                    ? ProductCategoryAssignmentRole.Primary
                    : ProductCategoryAssignmentRole.Additional));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task ValidateProductAttributesAsync(Guid productId, CancellationToken cancellationToken)
    {
        var categoryIds = await _db.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == productId && x.Role == CatalogProductCategoryRole.Primary)
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
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventPublished,
            ProductHistoryRules.SectionLifecycle,
            ProductHistoryRules.SummaryPublishedFa,
            ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Draft),
            ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Published));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UnpublishProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var product = await _db.Products.SingleAsync(x => x.ProductId == productId, cancellationToken);
        product.Unpublish(DateTimeOffset.UtcNow);
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventUnpublished,
            ProductHistoryRules.SectionLifecycle,
            ProductHistoryRules.SummaryUnpublishedFa,
            ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Published),
            ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Draft));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ArchiveProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var product = await _db.Products.SingleAsync(x => x.ProductId == productId, cancellationToken);
        var before = ProductPublishRules.LifecycleLabelFa(product.Status);
        product.Archive(DateTimeOffset.UtcNow);
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventArchived,
            ProductHistoryRules.SectionLifecycle,
            ProductHistoryRules.SummaryArchivedFa,
            before,
            ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Archived));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RestoreProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var product = await _db.Products.SingleAsync(x => x.ProductId == productId, cancellationToken);
        product.RestoreFromArchive(DateTimeOffset.UtcNow);
        QueueProductHistory(
            productId,
            ProductHistoryRules.EventRestored,
            ProductHistoryRules.SectionLifecycle,
            ProductHistoryRules.SummaryRestoredFa,
            ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Archived),
            ProductPublishRules.LifecycleLabelFa(CatalogPublicationStatus.Draft));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProductHistoryPage> ListProductHistoryAsync(
        Guid productId,
        string? section,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take <= 0 ? 50 : take, 1, 100);
        var query = _db.ProductHistoryEntries.AsNoTracking().Where(x => x.ProductId == productId);
        if (!string.IsNullOrWhiteSpace(section))
        {
            var normalized = section.Trim();
            query = query.Where(x => x.Section == normalized);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.HistoryId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return new ProductHistoryPage(rows.Select(ToHistoryDto).ToList(), total, skip, take);
    }

    /// <inheritdoc />
    public async Task AppendProductHistoryAsync(
        Guid productId,
        string eventType,
        string section,
        string summaryFa,
        string? beforeSummary,
        string? afterSummary,
        CancellationToken cancellationToken)
    {
        if (!await _db.Products.AnyAsync(x => x.ProductId == productId, cancellationToken))
        {
            throw new InvalidOperationException("محصول در Catalog این Tenant نیست.");
        }

        QueueProductHistory(productId, eventType, section, summaryFa, beforeSummary, afterSummary);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private void QueueProductHistory(
        Guid productId,
        string eventType,
        string section,
        string summaryFa,
        string? beforeSummary,
        string? afterSummary)
    {
        _db.ProductHistoryEntries.Add(CatalogProductHistoryEntry.Create(
            productId,
            eventType,
            section,
            summaryFa,
            DateTimeOffset.UtcNow,
            _actor?.ActorUserId,
            _actor?.ActorDisplayName,
            beforeSummary,
            afterSummary));
    }

    private static ProductHistoryEntryDto ToHistoryDto(CatalogProductHistoryEntry row) =>
        new(
            row.HistoryId,
            row.ProductId,
            row.EventType,
            row.Section,
            ProductHistoryRules.SectionLabelFa(row.Section),
            row.SummaryFa,
            row.BeforeSummary,
            row.AfterSummary,
            row.ActorUserId,
            string.IsNullOrWhiteSpace(row.ActorDisplayName)
                ? ProductHistoryRules.ActorSystemFa
                : row.ActorDisplayName!,
            row.OccurredAt);

    private async Task<bool> IsProductPrimaryCategoryAssignableAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var primaryCategoryId = await ResolvePrimaryCategoryIdAsync(productId, cancellationToken);
        if (primaryCategoryId is not Guid categoryId)
        {
            return false;
        }

        var parentById = await _db.Categories.AsNoTracking()
            .ToDictionaryAsync(x => x.CategoryId, x => x.ParentCategoryId, cancellationToken);
        try
        {
            CatalogCategoryTreeRules.EnsureAssignableProductCategory(categoryId, parentById);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
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


    private static void ClearDefaultFlags(IEnumerable<CatalogVariant> variants, DateTimeOffset now)
    {
        foreach (var variant in variants.Where(x => x.IsDefault))
        {
            variant.SetDefault(false, now);
        }
    }

    private async Task<Guid?> ResolvePrimaryCategoryIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        var categoryId = await _db.ProductCategories.AsNoTracking()
            .Where(x => x.ProductId == productId && x.Role == CatalogProductCategoryRole.Primary)
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
}
