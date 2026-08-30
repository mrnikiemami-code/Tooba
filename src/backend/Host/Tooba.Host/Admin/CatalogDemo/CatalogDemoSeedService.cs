using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Host.Admin.CatalogDemo;

/// <summary>شمارش‌های پس از seed برای API و شواهد.</summary>
public sealed record CatalogDemoSeedCounts(
    int Roots,
    int L2,
    int L3,
    int Brands,
    int Tags,
    int AttributeDefinitions,
    int AttributeOptions,
    int CategoryAttributeBindings,
    int Facets,
    int CategoryMediaAssignments,
    int MegaMenuPlacements,
    int Products,
    bool IdempotentReplay);

/// <summary>
/// دانهٔ idempotent foundation از طریق ICatalogDirectory (+ publish).
/// </summary>
public sealed class CatalogDemoSeedService
{
    private readonly ICatalogDirectory _catalog;
    private readonly CatalogDbContext _db;
    private readonly CatalogDemoMediaFactory _mediaFactory;
    private readonly ILogger<CatalogDemoSeedService> _logger;

    /// <summary>وابستگی‌های Catalog و Media را تزریق می‌کند.</summary>
    public CatalogDemoSeedService(
        ICatalogDirectory catalog,
        CatalogDbContext db,
        CatalogDemoMediaFactory mediaFactory,
        ILogger<CatalogDemoSeedService> logger)
    {
        _catalog = catalog;
        _db = db;
        _mediaFactory = mediaFactory;
        _logger = logger;
    }

    /// <summary>دانه را اجرا می‌کند؛ اجرای دوم بدون تکرار موجودیت‌ها.</summary>
    public async Task<CatalogDemoSeedCounts> SeedAsync(CancellationToken cancellationToken)
    {
        var existingRoots = await CountDemoRootsAsync(cancellationToken);
        var idempotent = existingRoots >= CatalogDemoMatrix.Roots.Count;

        var attributeIds = await EnsureAttributesAsync(cancellationToken);
        var brandIds = await EnsureBrandsAsync(cancellationToken);
        var tagIds = await EnsureTagsAsync(cancellationToken);
        _ = brandIds;

        var categoryIdsByKey = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var megaRootIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var mediaAssignments = 0;
        var bindings = 0;
        var facets = 0;
        var mega = 0;

        foreach (var (rootIndex, root) in CatalogDemoMatrix.Roots.Select((r, i) => (i, r)))
        {
            var rootId = await EnsureCategoryAsync(
                null,
                root.Key,
                root.Name,
                root.DescriptionFa,
                root.DescriptionEn,
                rootIndex,
                requireFullMedia: true,
                cancellationToken);
            categoryIdsByKey[root.Key] = rootId;
            mediaAssignments += 3;

            foreach (var tagKey in root.TagKeys)
            {
                if (tagIds.TryGetValue(tagKey, out var tagId))
                {
                    await TryAssignCategoryTagAsync(rootId, tagId, cancellationToken);
                }
            }

            var rootMega = await EnsureMegaMenuAsync(
                rootId,
                parentMegaMenuItemId: null,
                sortOrder: rootIndex,
                featured: root.MegaMenuFeatured,
                cancellationToken);
            megaRootIds[root.Key] = rootMega;
            mega++;

            foreach (var (l2Index, l2) in root.Children.Select((c, i) => (i, c)))
            {
                var l2FullKey = $"{root.Key}--{l2.Key}";
                var l2Id = await EnsureCategoryAsync(
                    rootId,
                    l2FullKey,
                    l2.Name,
                    shortFa: null,
                    shortEn: null,
                    l2Index,
                    requireFullMedia: false,
                    cancellationToken);
                categoryIdsByKey[l2FullKey] = l2Id;
                mediaAssignments += 1;

                var l2Mega = await EnsureMegaMenuAsync(
                    l2Id,
                    rootMega,
                    l2Index,
                    featured: false,
                    cancellationToken);
                mega++;

                foreach (var (l3Index, l3) in l2.Children.Select((c, i) => (i, c)))
                {
                    var l3FullKey = $"{l2FullKey}--{l3.Key}";
                    var l3Id = await EnsureCategoryAsync(
                        l2Id,
                        l3FullKey,
                        l3.Name,
                        shortFa: null,
                        shortEn: null,
                        l3Index,
                        requireFullMedia: false,
                        cancellationToken);
                    categoryIdsByKey[l3FullKey] = l3Id;
                    if (l3Index == 0)
                    {
                        mediaAssignments += 1;
                    }

                    if (l3Index == 0 || root.MegaMenuFeatured)
                    {
                        await EnsureMegaMenuAsync(l3Id, l2Mega, l3Index, featured: false, cancellationToken);
                        mega++;
                    }

                    foreach (var tagKey in l3.TagKeys)
                    {
                        if (tagIds.TryGetValue(tagKey, out var tagId))
                        {
                            await TryAssignCategoryTagAsync(l3Id, tagId, cancellationToken);
                        }
                    }

                    foreach (var binding in l3.Bindings)
                    {
                        var code = CatalogDemoMatrix.AttributeCode(l3.AttributeDomain, binding.AttributeCodeSuffix);
                        if (!attributeIds.TryGetValue(code, out var defId))
                        {
                            continue;
                        }

                        if (await EnsureBindingAsync(l3Id, defId, binding, cancellationToken))
                        {
                            bindings++;
                        }
                    }

                    if (l3.SeedFacets)
                    {
                        facets += await EnsureFacetsAsync(l3Id, l3, attributeIds, cancellationToken);
                    }
                }
            }
        }

        _logger.LogInformation(
            "CatalogDemo seed finished. roots={Roots} idempotent={Idempotent}",
            CatalogDemoMatrix.Roots.Count,
            idempotent);

        return await BuildCountsAsync(idempotent, cancellationToken);
    }

    private async Task<Dictionary<string, Guid>> EnsureAttributesAsync(CancellationToken cancellationToken)
    {
        var existing = await _db.AttributeDefinitions.AsNoTracking()
            .ToDictionaryAsync(d => d.Code, d => d.DefinitionId, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var (domain, specs) in CatalogDemoMatrix.AttributesByDomain)
        {
            foreach (var spec in specs)
            {
                var stableCode = CatalogDemoMatrix.AttributeCode(domain, spec.CodeSuffix);
                if (map.TryGetValue(stableCode, out _))
                {
                    continue;
                }

                if (existing.TryGetValue(stableCode, out var existingId))
                {
                    map[stableCode] = existingId;
                    continue;
                }

                // نام‌ها با دامنه متمایز می‌شوند تا با Attributeهای قدیمی (color/ram/…) برخورد نکنند.
                var names = new Dictionary<string, string>
                {
                    [CatalogDemoMatrix.LocaleFa] = $"{spec.Name.Fa} ({domain})",
                    [CatalogDemoMatrix.LocaleEn] = $"{spec.Name.En} ({domain})",
                };

                var created = await _catalog.CreateAttributeDefinitionAsync(
                    stableCode,
                    spec.ValueKind,
                    spec.IsVariantAxis,
                    names,
                    cancellationToken);
                await _catalog.UpdateAttributeDefinitionAsync(
                    created,
                    spec.Unit,
                    isRequired: false,
                    isFilterable: spec.IsFilterable,
                    isComparable: spec.IsComparable,
                    isMultivalue: false,
                    displayOrder: 0,
                    validationMin: null,
                    validationMax: null,
                    validationMaxLength: null,
                    isActive: true,
                    cancellationToken);

                foreach (var opt in spec.Options)
                {
                    await _catalog.AddAttributeOptionAsync(
                        created,
                        opt.Code,
                        new Dictionary<string, string>
                        {
                            [CatalogDemoMatrix.LocaleFa] = opt.Fa,
                            [CatalogDemoMatrix.LocaleEn] = opt.En,
                        },
                        cancellationToken);
                }

                map[stableCode] = created;
                existing[stableCode] = created;
            }
        }

        return map;
    }

    private async Task<Dictionary<string, Guid>> EnsureBrandsAsync(CancellationToken cancellationToken)
    {
        var existing = await _db.Brands.AsNoTracking()
            .Where(b => b.SlugSeam != null && b.SlugSeam.StartsWith(CatalogDemoSeam.BrandSlugPrefix))
            .ToDictionaryAsync(b => b.SlugSeam!, b => b.BrandId, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var map = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (var brand in CatalogDemoMatrix.Brands)
        {
            var slug = CatalogDemoMatrix.BrandSlug(brand.Key);
            if (existing.TryGetValue(slug, out var id))
            {
                map[brand.Key] = id;
                continue;
            }

            var created = await _catalog.CreateBrandAsync(slug, Names(brand.Name), cancellationToken);
            await _catalog.PublishBrandAsync(created.BrandId, cancellationToken);
            map[brand.Key] = created.BrandId;
        }

        return map;
    }

    private async Task<Dictionary<string, Guid>> EnsureTagsAsync(CancellationToken cancellationToken)
    {
        var existing = await _db.Tags.AsNoTracking()
            .Where(t => t.Code.StartsWith(CatalogDemoSeam.TagCodePrefix))
            .ToDictionaryAsync(t => t.Code, t => t.TagId, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var map = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (var tag in CatalogDemoMatrix.Tags)
        {
            var code = CatalogDemoMatrix.TagCode(tag.Key);
            if (existing.TryGetValue(code, out var id))
            {
                map[tag.Key] = id;
                continue;
            }

            var created = await _catalog.CreateTagAsync(
                code,
                code,
                Names(tag.Name),
                CatalogDemoMatrix.LocaleFa,
                cancellationToken);
            await _catalog.PublishTagAsync(created.TagId, cancellationToken);
            map[tag.Key] = created.TagId;
        }

        return map;
    }

    private async Task<Guid> EnsureCategoryAsync(
        Guid? parentId,
        string key,
        CatalogDemoLocalizedName name,
        string? shortFa,
        string? shortEn,
        int sortOrder,
        bool requireFullMedia,
        CancellationToken cancellationToken)
    {
        var slug = CatalogCategorySlugNormalizer.NormalizeSlug(CatalogDemoMatrix.CategorySlug(key));
        var existing = await _db.CategoryTranslations.AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Locale == CatalogDemoMatrix.LocaleEn && t.Slug == slug,
                cancellationToken);
        if (existing is not null)
        {
            return existing.CategoryId;
        }

        Guid? image = null, icon = null, banner = null;
        if (requireFullMedia)
        {
            (image, icon, banner) = await _mediaFactory.EnsureCategoryMediaAsync(key, cancellationToken);
        }
        else
        {
            image = await _mediaFactory.EnsureSimpleImageAsync(key, cancellationToken);
            if (parentId is not null)
            {
                // برای بخشی از L2/L3 آیکن هم بگذار.
                icon = await _mediaFactory.EnsureSimpleIconAsync(key, cancellationToken);
            }
        }

        var translations = new List<CategoryTranslationUpsertRequest>
        {
            new(
                CatalogDemoMatrix.LocaleFa,
                name.Fa,
                slug,
                shortFa,
                shortFa,
                $"{name.Fa} | توبا",
                shortFa ?? name.Fa),
            new(
                CatalogDemoMatrix.LocaleEn,
                name.En,
                slug,
                shortEn,
                shortEn,
                $"{name.En} | Tooba",
                shortEn ?? name.En),
        };

        var created = await _catalog.CreateCategoryAsync(
            new CategoryCreateRequest(parentId, sortOrder, true, image, icon, banner, translations),
            cancellationToken);
        await _catalog.PublishCategoryAsync(created.CategoryId, cancellationToken);
        return created.CategoryId;
    }

    private async Task<Guid> EnsureMegaMenuAsync(
        Guid categoryId,
        Guid? parentMegaMenuItemId,
        int sortOrder,
        bool featured,
        CancellationToken cancellationToken)
    {
        var existing = await _db.MegaMenuItems.AsNoTracking()
            .FirstOrDefaultAsync(m => m.CategoryId == categoryId, cancellationToken);
        if (existing is not null)
        {
            return existing.MegaMenuItemId;
        }

        await _catalog.UpsertCategoryMegaMenuBindingAsync(
            categoryId,
            CatalogDemoMatrix.LocaleFa,
            new CategoryMegaMenuBindingInput(
                parentMegaMenuItemId,
                sortOrder,
                IsVisible: true,
                IsFeatured: featured,
                ImageMediaAssetId: null,
                IconMediaAssetId: null,
                TitleOverride: null,
                BadgeText: null,
                ShortLabel: null),
            cancellationToken);

        var created = await _db.MegaMenuItems.AsNoTracking()
            .SingleAsync(m => m.CategoryId == categoryId, cancellationToken);
        return created.MegaMenuItemId;
    }

    private async Task TryAssignCategoryTagAsync(Guid categoryId, Guid tagId, CancellationToken cancellationToken)
    {
        var exists = await _db.CategoryTagAssignments.AsNoTracking()
            .AnyAsync(x => x.CategoryId == categoryId && x.TagId == tagId, cancellationToken);
        if (exists)
        {
            return;
        }

        try
        {
            await _catalog.AssignCategoryTagAsync(categoryId, tagId, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // تکراری — idempotent.
        }
    }

    private async Task<bool> EnsureBindingAsync(
        Guid categoryId,
        Guid definitionId,
        CatalogDemoBindingSpec binding,
        CancellationToken cancellationToken)
    {
        var exists = await _db.CategoryAttributeBindings.AsNoTracking()
            .AnyAsync(x => x.CategoryId == categoryId && x.DefinitionId == definitionId, cancellationToken);
        if (exists)
        {
            return false;
        }

        var def = await _db.AttributeDefinitions.AsNoTracking()
            .SingleOrDefaultAsync(d => d.DefinitionId == definitionId, cancellationToken);
        if (def is null)
        {
            return false;
        }

        // اگر تعریف reuse‌شده قابلیت محور تنوع ندارد، binding را بدون variant اعمال کن.
        var variant =
            binding.IsVariantAxis
            && def.IsVariantAxisAllowed
            && def.ValueKind is not (CatalogAttributeValueKind.Text or CatalogAttributeValueKind.Boolean or CatalogAttributeValueKind.Instant);

        await _catalog.BindCategoryAttributeAsync(
            categoryId,
            definitionId,
            binding.DisplayOrder,
            new CategoryAttributeAssignmentFlags(
                binding.IsRequired,
                binding.IsFilterable,
                variant,
                binding.IsComparable),
            cancellationToken);
        return true;
    }

    private async Task<int> EnsureFacetsAsync(
        Guid categoryId,
        CatalogDemoL3Spec l3,
        IReadOnlyDictionary<string, Guid> attributeIds,
        CancellationToken cancellationToken)
    {
        var added = 0;
        var order = 0;
        foreach (var binding in l3.Bindings.Where(b => b.IsFilterable))
        {
            var code = CatalogDemoMatrix.AttributeCode(l3.AttributeDomain, binding.AttributeCodeSuffix);
            if (!attributeIds.TryGetValue(code, out var defId))
            {
                continue;
            }

            var def = await _db.AttributeDefinitions.AsNoTracking()
                .SingleOrDefaultAsync(d => d.DefinitionId == defId, cancellationToken);
            if (def is null || !def.IsFilterable && !binding.IsFilterable)
            {
                // binding محلی filterable کافی است؛ GetEffective facets از binding می‌خواند.
            }

            var exists = await _db.CategoryFacetConfigurations.AsNoTracking()
                .AnyAsync(f => f.CategoryId == categoryId && f.DefinitionId == defId, cancellationToken);
            if (exists)
            {
                continue;
            }

            if (def is null)
            {
                continue;
            }

            var display = CatalogCategoryFacetRules.SuggestDisplayType(def.ValueKind);
            try
            {
                await _catalog.UpsertCategoryFacetConfigurationAsync(
                    categoryId,
                    defId,
                    new CategoryFacetConfigurationInput(
                        display,
                        order,
                        IsVisible: true,
                        IsSearchable: CatalogCategoryFacetRules.IsSearchableAllowed(display),
                        IsCollapsedByDefault: false,
                        ShowCounts: true),
                    cancellationToken);
                added++;
                order += 10;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogDebug(ex, "Skip facet for {Code} on {CategoryId}", code, categoryId);
            }
        }

        return added;
    }

    private async Task<int> CountDemoRootsAsync(CancellationToken cancellationToken)
    {
        var rootIds = await _db.Categories.AsNoTracking()
            .Where(c => c.ParentCategoryId == null)
            .Select(c => c.CategoryId)
            .ToListAsync(cancellationToken);
        var demo = await _db.CategoryTranslations.AsNoTracking()
            .Where(t => rootIds.Contains(t.CategoryId) && t.Slug.StartsWith(CatalogDemoSeam.CategorySlugPrefix))
            .Select(t => t.CategoryId)
            .Distinct()
            .CountAsync(cancellationToken);
        return demo;
    }

    private async Task<CatalogDemoSeedCounts> BuildCountsAsync(bool idempotent, CancellationToken cancellationToken)
    {
        var parentMap = await _db.Categories.AsNoTracking()
            .ToDictionaryAsync(c => c.CategoryId, c => c.ParentCategoryId, cancellationToken);
        var demoCategoryIds = await _db.CategoryTranslations.AsNoTracking()
            .Where(t => t.Slug.StartsWith(CatalogDemoSeam.CategorySlugPrefix))
            .Select(t => t.CategoryId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var roots = 0;
        var l2 = 0;
        var l3 = 0;
        foreach (var id in demoCategoryIds)
        {
            var level = CatalogCategoryTreeRules.GetCategoryLevel(id, parentMap);
            switch (level)
            {
                case 1: roots++; break;
                case 2: l2++; break;
                case 3: l3++; break;
            }
        }

        var brands = await _db.Brands.AsNoTracking()
            .CountAsync(b => b.SlugSeam != null && b.SlugSeam.StartsWith(CatalogDemoSeam.BrandSlugPrefix), cancellationToken);
        var tags = await _db.Tags.AsNoTracking()
            .CountAsync(t => t.Code.StartsWith(CatalogDemoSeam.TagCodePrefix), cancellationToken);
        var defs = await _db.AttributeDefinitions.AsNoTracking()
            .CountAsync(d => d.Code.StartsWith(CatalogDemoSeam.AttributeCodePrefix), cancellationToken);
        var defIds = await _db.AttributeDefinitions.AsNoTracking()
            .Where(d => d.Code.StartsWith(CatalogDemoSeam.AttributeCodePrefix))
            .Select(d => d.DefinitionId)
            .ToListAsync(cancellationToken);
        var options = await _db.AttributeOptions.AsNoTracking()
            .CountAsync(o => defIds.Contains(o.DefinitionId), cancellationToken);
        var bindings = await _db.CategoryAttributeBindings.AsNoTracking()
            .CountAsync(b => demoCategoryIds.Contains(b.CategoryId), cancellationToken);
        var facets = await _db.CategoryFacetConfigurations.AsNoTracking()
            .CountAsync(f => demoCategoryIds.Contains(f.CategoryId), cancellationToken);
        var mega = await _db.MegaMenuItems.AsNoTracking()
            .CountAsync(m => demoCategoryIds.Contains(m.CategoryId), cancellationToken);
        var media = await _db.Categories.AsNoTracking()
            .Where(c => demoCategoryIds.Contains(c.CategoryId))
            .SumAsync(
                c => (c.ImageMediaAssetId != null ? 1 : 0)
                    + (c.IconMediaAssetId != null ? 1 : 0)
                    + (c.BannerMediaAssetId != null ? 1 : 0),
                cancellationToken);
        var products = await _db.Products.AsNoTracking()
            .CountAsync(
                p => p.SlugSeam != null && p.SlugSeam.StartsWith(CatalogDemoSeam.SmokeProductSlugPrefix),
                cancellationToken);

        return new CatalogDemoSeedCounts(
            roots, l2, l3, brands, tags, defs, options, bindings, facets, media, mega, products, idempotent);
    }

    private static Dictionary<string, string> Names(CatalogDemoLocalizedName name) =>
        new()
        {
            [CatalogDemoMatrix.LocaleFa] = name.Fa,
            [CatalogDemoMatrix.LocaleEn] = name.En,
        };
}
