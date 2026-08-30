using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Host.Admin.CatalogDemo;

/// <summary>خلاصهٔ دانهٔ محصولات غنی برای شواهد و API.</summary>
public sealed record CatalogDemoProductSeedReport(
    int ProductsCreatedOrEnsured,
    int BrandlessCount,
    int WithVariants,
    int WithoutVariants,
    int VariantTotal,
    int MediaAssignments,
    int TagAssignments,
    int AdditionalCategoryAssignments,
    int ReadinessFailures,
    IReadOnlyDictionary<int, int> CountByLeafSize);

/// <summary>
/// دانهٔ idempotent محصولات غنی روی foundation T033 (TB-P07-T034).
/// </summary>
public sealed class CatalogDemoProductSeedService
{
    private static readonly string[] DomainBrandKeys =
    [
        "samsung", "apple", "xiaomi", "huawei", "sony", "lg", "asus", "lenovo", "hp", "dell",
        "bosch", "philips", "jbl", "nike", "adidas", "zara", "loreal", "panasonic", "canon", "garmin",
        "kitchenaid", "nestle",
    ];

    private static readonly IReadOnlyDictionary<string, string[]> BrandsByDomain =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["mobile"] = ["samsung", "apple", "xiaomi", "huawei", "sony"],
            ["laptop"] = ["asus", "lenovo", "hp", "dell", "apple"],
            ["clothing"] = ["nike", "adidas", "zara"],
            ["home"] = ["bosch", "philips", "lg", "kitchenaid", "panasonic"],
            ["books"] = ["nestle"], // rarely used; books often brandless
            ["food"] = ["nestle"],
        };

    private readonly ICatalogDirectory _catalog;
    private readonly CatalogDbContext _db;
    private readonly CatalogDemoMediaFactory _mediaFactory;
    private readonly ILogger<CatalogDemoProductSeedService> _logger;

    /// <summary>وابستگی‌ها را تزریق می‌کند.</summary>
    public CatalogDemoProductSeedService(
        ICatalogDirectory catalog,
        CatalogDbContext db,
        CatalogDemoMediaFactory mediaFactory,
        ILogger<CatalogDemoProductSeedService> logger)
    {
        _catalog = catalog;
        _db = db;
        _mediaFactory = mediaFactory;
        _logger = logger;
    }

    /// <summary>برای هر L3 دانه ۳–۵ محصول Draft و Publish-Ready می‌سازد.</summary>
    public async Task<CatalogDemoProductSeedReport> SeedProductsAsync(CancellationToken cancellationToken)
    {
        var brandIds = await LoadBrandMapAsync(cancellationToken);
        var tagIds = await LoadTagMapAsync(cancellationToken);
        var categoryByFullKey = await LoadCategoryKeyMapAsync(cancellationToken);
        var mediaPools = new Dictionary<string, IReadOnlyList<Guid>>(StringComparer.Ordinal);
        var leafSizes = new Dictionary<int, int> { [3] = 0, [4] = 0, [5] = 0 };

        var created = 0;
        var brandless = 0;
        var withVariants = 0;
        var withoutVariants = 0;
        var variantTotal = 0;
        var mediaAssignments = 0;
        var tagAssignments = 0;
        var additionalAssignments = 0;
        var readinessFailures = 0;

        var leaves = EnumerateLeaves().ToList();
        if (leaves.Count == 0)
        {
            throw new InvalidOperationException("No demo L3 leaves found; foundation seed must run first.");
        }

        var siblingMap = BuildSiblingMap(leaves);

        foreach (var leaf in leaves)
        {
            if (!categoryByFullKey.TryGetValue(leaf.FullKey, out var categoryId))
            {
                throw new InvalidOperationException($"Missing seeded category for key '{leaf.FullKey}'.");
            }

            var productCount = DeterministicLeafProductCount(leaf.FullKey);
            leafSizes[productCount] = leafSizes[productCount] + 1;

            if (!mediaPools.TryGetValue(leaf.Domain, out var pool))
            {
                pool = await _mediaFactory.EnsureProductMediaPoolAsync(leaf.Domain, poolSize: 8, cancellationToken);
                mediaPools[leaf.Domain] = pool;
            }

            for (var index = 1; index <= productCount; index++)
            {
                var slug = CatalogCategorySlugNormalizer.NormalizeSlug(
                    CatalogDemoSeam.ProductSlugPrefix + SanitizeSlugKey(leaf.FullKey) + "-" + index);
                var existing = await _db.Products.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.SlugSeam == slug, cancellationToken);
                if (existing is not null)
                {
                    created++;
                    continue;
                }

                var brandlessPick = IsBrandless(slug, leaf.Domain);
                Guid? brandId = null;
                if (!brandlessPick)
                {
                    brandId = PickBrandId(leaf.Domain, index, brandIds);
                }
                else
                {
                    brandless++;
                }

                var names = BuildProductNames(leaf, index, brandId, brandIds);
                var product = await _catalog.CreateProductAsync(
                    CatalogProductKind.PhysicalGood,
                    slug,
                    brandId,
                    names,
                    cancellationToken);

                await _catalog.AssignCategoryAsync(product.ProductId, categoryId, cancellationToken);

                if (ShouldAddAdditional(slug) && siblingMap.TryGetValue(leaf.FullKey, out var siblings) && siblings.Count > 0)
                {
                    var siblingKey = siblings[StableHash(slug + ":add") % siblings.Count];
                    if (categoryByFullKey.TryGetValue(siblingKey, out var additionalId))
                    {
                        await _catalog.AddProductAdditionalCategoryAsync(product.ProductId, additionalId, cancellationToken);
                        additionalAssignments++;
                    }
                }

                await UpsertDescriptionsAsync(product.ProductId, leaf, index, names, cancellationToken);

                var tagCount = 2 + (StableHash(slug + ":tags") % 4); // 2–5
                var assignedTags = PickTags(leaf.Spec.TagKeys, tagCount, slug, tagIds);
                foreach (var tagId in assignedTags)
                {
                    await _catalog.AssignProductTagAsync(product.ProductId, tagId, cancellationToken);
                    tagAssignments++;
                }

                var attrState = await _catalog.GetProductAttributeEditorStateAsync(
                    product.ProductId,
                    CatalogDemoMatrix.LocaleFa,
                    cancellationToken);
                await FillAttributesAsync(product.ProductId, attrState, index, cancellationToken);

                var variantState = await _catalog.GetProductVariantEditorStateAsync(
                    product.ProductId,
                    CatalogDemoMatrix.LocaleFa,
                    cancellationToken);
                var variantCount = await ApplyVariantsIfNeededAsync(
                    product.ProductId,
                    slug,
                    index,
                    variantState,
                    cancellationToken);
                if (variantCount > 0)
                {
                    withVariants++;
                    variantTotal += variantCount;
                }
                else
                {
                    withoutVariants++;
                }

                var orderedMedia = PickMediaSet(pool, index);
                for (var m = 0; m < orderedMedia.Count; m++)
                {
                    var alt = $"{names[CatalogDemoMatrix.LocaleFa]} — تصویر {m + 1}";
                    await _catalog.AttachMediaReferenceAsync(product.ProductId, orderedMedia[m], alt, cancellationToken);
                    mediaAssignments++;
                }

                await _catalog.SetProductPrimaryMediaAsync(product.ProductId, orderedMedia[0], cancellationToken);
                await _catalog.ReorderProductMediaAsync(product.ProductId, orderedMedia, cancellationToken);

                await UpsertSeoAsync(product.ProductId, slug, names, leaf, cancellationToken);

                var ready = await _catalog.GetProductPublishReadinessAsync(
                    product.ProductId,
                    CatalogDemoMatrix.LocaleFa,
                    cancellationToken);
                if (!ready.IsReady)
                {
                    readinessFailures++;
                    var missing = string.Join("; ", ready.MissingRequirements.Select(x => x.Code + ":" + x.MessageFa));
                    throw new InvalidOperationException(
                        $"Demo product '{slug}' is not publish-ready: {missing}");
                }

                if (product.Status != CatalogPublicationStatus.Draft)
                {
                    throw new InvalidOperationException($"Demo product '{slug}' must remain Draft.");
                }

                created++;
                if (created % 25 == 0)
                {
                    _logger.LogInformation("CatalogDemo products progress: {Count}/{Approx}", created, leaves.Count * 4);
                }
            }
        }

        _logger.LogInformation(
            "CatalogDemo products finished. products={Products} brandless={Brandless} variants={Variants} readinessFailures={Failures}",
            created,
            brandless,
            variantTotal,
            readinessFailures);

        return new CatalogDemoProductSeedReport(
            created,
            brandless,
            withVariants,
            withoutVariants,
            variantTotal,
            mediaAssignments,
            tagAssignments,
            additionalAssignments,
            readinessFailures,
            leafSizes);
    }

    private async Task UpsertDescriptionsAsync(
        Guid productId,
        LeafSpec leaf,
        int index,
        IReadOnlyDictionary<string, string> names,
        CancellationToken cancellationToken)
    {
        var shortFa = Truncate($"مناسب {leaf.Spec.Name.Fa}؛ گزینهٔ {index} برای خرید آگاهانه.", 480);
        var shortEn = Truncate($"Built for {leaf.Spec.Name.En}; option {index} with practical everyday features.", 480);
        var fullFa =
            $"<p>{names[CatalogDemoMatrix.LocaleFa]} برای دستهٔ <strong>{leaf.Spec.Name.Fa}</strong> طراحی شده است.</p>" +
            $"<ul><li>مشخصات واقعی و قابل مقایسه</li><li>مناسب استفادهٔ روزمره و حرفه‌ای</li><li>بدون دادهٔ قیمت یا موجودی در Catalog</li></ul>" +
            $"<p>این نسخهٔ دانهٔ نمایشی برای بررسی UX تجاری آماده است.</p>";
        var fullEn =
            $"<p>{names[CatalogDemoMatrix.LocaleEn]} is curated for <strong>{leaf.Spec.Name.En}</strong>.</p>" +
            $"<ul><li>Realistic comparable specs</li><li>Everyday and professional use cases</li><li>No Catalog Price/Stock fields</li></ul>" +
            $"<p>This demo draft is publish-ready for commercial UX review.</p>";

        await _catalog.UpsertProductLocalizedFieldAsync(
            productId,
            "short_description",
            new Dictionary<string, string>
            {
                [CatalogDemoMatrix.LocaleFa] = shortFa,
                [CatalogDemoMatrix.LocaleEn] = shortEn,
            },
            cancellationToken);
        await _catalog.UpsertProductLocalizedFieldAsync(
            productId,
            "full_description",
            new Dictionary<string, string>
            {
                [CatalogDemoMatrix.LocaleFa] = fullFa,
                [CatalogDemoMatrix.LocaleEn] = fullEn,
            },
            cancellationToken);
    }

    private async Task FillAttributesAsync(
        Guid productId,
        ProductAttributeEditorState state,
        int index,
        CancellationToken cancellationToken)
    {
        // تعریف‌های IsVariantAxisAllowed هرگز روی Product ذخیره نمی‌شوند (حتی اگر binding محور نباشد).
        var variantAllowedIds = await _db.AttributeDefinitions.AsNoTracking()
            .Where(d => d.IsVariantAxis)
            .Select(d => d.DefinitionId)
            .ToListAsync(cancellationToken);
        var variantAllowed = variantAllowedIds.ToHashSet();

        var values = new List<ProductAttributeValueInput>();
        foreach (var field in state.Fields)
        {
            if (field.IsVariantAxis || variantAllowed.Contains(field.DefinitionId))
            {
                continue;
            }

            if (!field.IsRequired && StableHash(field.Code + index) % 3 == 0 && StableHash(field.Code + ":skip") % 2 == 0)
            {
                continue;
            }

            switch (field.ValueKind)
            {
                case CatalogAttributeValueKind.Enumeration:
                {
                    var opts = field.Options.Where(o => o.IsActive).ToList();
                    if (opts.Count == 0)
                    {
                        break;
                    }

                    var pick = opts[(index - 1 + StableHash(field.Code)) % opts.Count];
                    values.Add(new ProductAttributeValueInput(field.DefinitionId, null, pick.OptionId, Clear: false));
                    break;
                }
                case CatalogAttributeValueKind.Boolean:
                    values.Add(new ProductAttributeValueInput(
                        field.DefinitionId,
                        (index % 2 == 0).ToString().ToLowerInvariant(),
                        null,
                        Clear: false));
                    break;
                case CatalogAttributeValueKind.Number:
                {
                    var raw = field.Code.Contains("screen", StringComparison.OrdinalIgnoreCase)
                              || field.Code.Contains("display", StringComparison.OrdinalIgnoreCase)
                        ? (6.0 + (index % 5) * 0.2).ToString("0.0", CultureInfo.InvariantCulture)
                        : field.Code.Contains("battery", StringComparison.OrdinalIgnoreCase)
                            ? (3000 + index * 250).ToString(CultureInfo.InvariantCulture)
                            : field.Code.Contains("weight", StringComparison.OrdinalIgnoreCase)
                                ? (1.2 + index * 0.1).ToString("0.0", CultureInfo.InvariantCulture)
                                : field.Code.Contains("power", StringComparison.OrdinalIgnoreCase)
                                    ? (800 + index * 100).ToString(CultureInfo.InvariantCulture)
                                    : field.Code.Contains("capacity", StringComparison.OrdinalIgnoreCase)
                                        ? (5 + index).ToString(CultureInfo.InvariantCulture)
                                        : field.Code.Contains("pages", StringComparison.OrdinalIgnoreCase)
                                            ? (120 + index * 40).ToString(CultureInfo.InvariantCulture)
                                            : field.Code.Contains("volume", StringComparison.OrdinalIgnoreCase)
                                                ? (250 + index * 50).ToString(CultureInfo.InvariantCulture)
                                                : (10 + index).ToString(CultureInfo.InvariantCulture);
                    values.Add(new ProductAttributeValueInput(field.DefinitionId, raw, null, Clear: false));
                    break;
                }
                case CatalogAttributeValueKind.Text:
                {
                    var text = field.Code.Contains("author", StringComparison.OrdinalIgnoreCase)
                        ? $"نویسنده نمونه {index}"
                        : field.Code.Contains("publisher", StringComparison.OrdinalIgnoreCase)
                            ? $"نشر نمونه {index}"
                            : field.Code.Contains("gpu", StringComparison.OrdinalIgnoreCase)
                                ? $"Integrated GPU {index}"
                                : $"value-{index}";
                    values.Add(new ProductAttributeValueInput(field.DefinitionId, text, null, Clear: false));
                    break;
                }
                default:
                    break;
            }
        }

        // Always fill required missing fields even if optional-skip logic ran.
        foreach (var field in state.Fields.Where(f => f.IsRequired && !f.IsVariantAxis && !variantAllowed.Contains(f.DefinitionId)))
        {
            if (values.Any(v => v.DefinitionId == field.DefinitionId))
            {
                continue;
            }

            if (field.ValueKind == CatalogAttributeValueKind.Enumeration)
            {
                var opt = field.Options.FirstOrDefault(o => o.IsActive);
                if (opt is not null)
                {
                    values.Add(new ProductAttributeValueInput(field.DefinitionId, null, opt.OptionId, Clear: false));
                }
            }
            else if (field.ValueKind == CatalogAttributeValueKind.Boolean)
            {
                values.Add(new ProductAttributeValueInput(field.DefinitionId, "true", null, Clear: false));
            }
            else if (field.ValueKind == CatalogAttributeValueKind.Number)
            {
                values.Add(new ProductAttributeValueInput(field.DefinitionId, "1", null, Clear: false));
            }
            else
            {
                values.Add(new ProductAttributeValueInput(field.DefinitionId, "n/a", null, Clear: false));
            }
        }

        if (values.Count > 0)
        {
            await _catalog.SetProductAttributesAsync(productId, values, cancellationToken);
        }
    }

    private async Task<int> ApplyVariantsIfNeededAsync(
        Guid productId,
        string slug,
        int index,
        ProductVariantEditorState state,
        CancellationToken cancellationToken)
    {
        var axes = state.Axes.Where(a => a.Options.Any(o => o.IsActive)).ToList();
        if (axes.Count == 0)
        {
            return 0;
        }

        var selected = new List<ProductVariantSelectedAxisInput>();
        var projected = 1;
        foreach (var axis in axes)
        {
            var active = axis.Options.Where(o => o.IsActive).ToList();
            // Keep combinations modest: prefer 2–3 options per axis.
            var take = Math.Min(active.Count, axes.Count >= 3 ? 2 : 3);
            take = Math.Max(1, take);
            var offset = StableHash(slug + axis.Code) % active.Count;
            var picks = new List<Guid>();
            for (var i = 0; i < take; i++)
            {
                picks.Add(active[(offset + i) % active.Count].OptionId);
            }

            projected *= picks.Count;
            if (projected > 48)
            {
                // shrink last axis
                picks = picks.Take(1).ToList();
                projected = Math.Max(1, projected / take);
            }

            selected.Add(new ProductVariantSelectedAxisInput(axis.DefinitionId, picks));
        }

        var first = await _catalog.ApplyProductVariantMatrixAsync(
            productId,
            new ProductVariantApplyInput(CatalogDemoMatrix.LocaleFa, selected, null, null),
            cancellationToken);

        var defaultVariant = first.Variants
            .Where(v => v.Status != CatalogPublicationStatus.Archived)
            .OrderBy(v => v.SortOrder)
            .FirstOrDefault();
        if (defaultVariant is null)
        {
            return 0;
        }

        var patches = first.Variants
            .Where(v => v.Status != CatalogPublicationStatus.Archived)
            .Select((v, i) => new ProductVariantPatchInput(
                v.VariantId,
                Status: null,
                CatalogCodeSeam: $"DEMO-{SanitizeSlugKey(slug)}-{i + 1}",
                SortOrder: null,
                IsDefault: v.VariantId == defaultVariant.VariantId))
            .ToList();

        await _catalog.ApplyProductVariantMatrixAsync(
            productId,
            new ProductVariantApplyInput(
                CatalogDemoMatrix.LocaleFa,
                selected,
                defaultVariant.VariantId,
                patches),
            cancellationToken);

        return first.Variants.Count(v => v.Status != CatalogPublicationStatus.Archived);
    }

    private async Task UpsertSeoAsync(
        Guid productId,
        string slug,
        IReadOnlyDictionary<string, string> names,
        LeafSpec leaf,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = CatalogCategorySlugNormalizer.NormalizeSlug(slug);
        foreach (var locale in new[] { CatalogDemoMatrix.LocaleFa, CatalogDemoMatrix.LocaleEn })
        {
            var current = await _catalog.GetProductSeoAsync(productId, locale, cancellationToken);
            var title = locale == CatalogDemoMatrix.LocaleFa
                ? $"{names[CatalogDemoMatrix.LocaleFa]} | {leaf.Spec.Name.Fa}"
                : $"{names[CatalogDemoMatrix.LocaleEn]} | {leaf.Spec.Name.En}";
            var description = locale == CatalogDemoMatrix.LocaleFa
                ? $"خرید {names[CatalogDemoMatrix.LocaleFa]} در دسته {leaf.Spec.Name.Fa}. مشخصات کامل، گالری تصویر و آمادگی انتشار."
                : $"Shop {names[CatalogDemoMatrix.LocaleEn]} in {leaf.Spec.Name.En}. Full specs, gallery, and publish readiness.";

            // اگر slug از Create همین است و SEO قبلاً پر شده، فقط title/description را تازه کن.
            await _catalog.UpdateProductSeoAsync(
                productId,
                new ProductSeoUpdateInput(
                    locale,
                    normalizedSlug,
                    Truncate(title, 180),
                    Truncate(description, 320),
                    current.UpdatedAt),
                cancellationToken);
        }
    }

    private async Task<Dictionary<string, Guid>> LoadBrandMapAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.Brands.AsNoTracking()
            .Where(b => b.SlugSeam != null && b.SlugSeam.StartsWith(CatalogDemoSeam.BrandSlugPrefix))
            .Select(b => new { b.SlugSeam, b.BrandId })
            .ToListAsync(cancellationToken);
        var map = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var brand in CatalogDemoMatrix.Brands)
        {
            var slug = CatalogDemoMatrix.BrandSlug(brand.Key);
            var hit = rows.FirstOrDefault(r => string.Equals(r.SlugSeam, slug, StringComparison.OrdinalIgnoreCase));
            if (hit is not null)
            {
                map[brand.Key] = hit.BrandId;
            }
        }

        return map;
    }

    private async Task<Dictionary<string, Guid>> LoadTagMapAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.Tags.AsNoTracking()
            .Where(t => t.Code.StartsWith(CatalogDemoSeam.TagCodePrefix))
            .Select(t => new { t.Code, t.TagId })
            .ToListAsync(cancellationToken);
        var map = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var tag in CatalogDemoMatrix.Tags)
        {
            var code = CatalogDemoMatrix.TagCode(tag.Key);
            var hit = rows.FirstOrDefault(r => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase));
            if (hit is not null)
            {
                map[tag.Key] = hit.TagId;
            }
        }

        return map;
    }

    private async Task<Dictionary<string, Guid>> LoadCategoryKeyMapAsync(CancellationToken cancellationToken)
    {
        var translations = await _db.CategoryTranslations.AsNoTracking()
            .Where(t => t.Locale == CatalogDemoMatrix.LocaleEn && t.Slug.StartsWith(CatalogDemoSeam.CategorySlugPrefix))
            .Select(t => new { t.Slug, t.CategoryId })
            .ToListAsync(cancellationToken);
        var map = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var leaf in EnumerateLeaves())
        {
            var slug = CatalogCategorySlugNormalizer.NormalizeSlug(CatalogDemoMatrix.CategorySlug(leaf.FullKey));
            var hit = translations.FirstOrDefault(t => string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase));
            if (hit is not null)
            {
                map[leaf.FullKey] = hit.CategoryId;
            }
        }

        return map;
    }

    private static IEnumerable<LeafSpec> EnumerateLeaves()
    {
        foreach (var root in CatalogDemoMatrix.Roots)
        {
            foreach (var l2 in root.Children)
            {
                foreach (var l3 in l2.Children)
                {
                    var fullKey = $"{root.Key}--{l2.Key}--{l3.Key}";
                    yield return new LeafSpec(fullKey, root.Key, l2.Key, l3.AttributeDomain, l3);
                }
            }
        }
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildSiblingMap(IReadOnlyList<LeafSpec> leaves)
    {
        var byParent = leaves
            .GroupBy(l => $"{l.RootKey}--{l.L2Key}", StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(x => x.FullKey).ToList(),
                StringComparer.Ordinal);

        var map = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var leaf in leaves)
        {
            var parent = $"{leaf.RootKey}--{leaf.L2Key}";
            var siblings = byParent[parent].Where(k => !string.Equals(k, leaf.FullKey, StringComparison.Ordinal)).ToList();
            map[leaf.FullKey] = siblings;
        }

        return map;
    }

    private static Dictionary<string, string> BuildProductNames(
        LeafSpec leaf,
        int index,
        Guid? brandId,
        IReadOnlyDictionary<string, Guid> brandIds)
    {
        var brandFa = "";
        var brandEn = "";
        if (brandId is Guid id)
        {
            var key = brandIds.FirstOrDefault(kv => kv.Value == id).Key;
            var brand = CatalogDemoMatrix.Brands.FirstOrDefault(b => b.Key == key);
            if (brand is not null)
            {
                brandFa = brand.Name.Fa + " ";
                brandEn = brand.Name.En + " ";
            }
        }

        var adjectivesFa = new[] { "سری", "مدل", "نسخه", "مجموعه", "انتخاب" };
        var adjectivesEn = new[] { "Series", "Model", "Edition", "Collection", "Select" };
        var adj = (index - 1) % adjectivesFa.Length;
        var fa = $"{brandFa}{leaf.Spec.Name.Fa} {adjectivesFa[adj]} {index}";
        var en = $"{brandEn}{leaf.Spec.Name.En} {adjectivesEn[adj]} {index}";
        return new Dictionary<string, string>
        {
            [CatalogDemoMatrix.LocaleFa] = fa,
            [CatalogDemoMatrix.LocaleEn] = en,
        };
    }

    private static IReadOnlyList<Guid> PickTags(
        IReadOnlyList<string> preferred,
        int count,
        string slug,
        IReadOnlyDictionary<string, Guid> tagIds)
    {
        var keys = preferred.Concat(CatalogDemoMatrix.Tags.Select(t => t.Key)).Distinct(StringComparer.Ordinal).ToList();
        var result = new List<Guid>();
        for (var i = 0; i < keys.Count && result.Count < count; i++)
        {
            var key = keys[(StableHash(slug) + i) % keys.Count];
            if (tagIds.TryGetValue(key, out var id) && !result.Contains(id))
            {
                result.Add(id);
            }
        }

        return result;
    }

    private Guid? PickBrandId(string domain, int index, IReadOnlyDictionary<string, Guid> brandIds)
    {
        var keys = BrandsByDomain.TryGetValue(domain, out var domainKeys)
            ? domainKeys
            : DomainBrandKeys;
        var key = keys[(index - 1) % keys.Length];
        return brandIds.TryGetValue(key, out var id) ? id : brandIds.Values.FirstOrDefault();
    }

    private static bool IsBrandless(string slug, string domain)
    {
        // Books/food lean more brandless; overall target ~10–20%.
        var threshold = domain is "books" or "food" ? 55 : 15;
        return StableHash(slug + ":brand") % 100 < threshold;
    }

    private static bool ShouldAddAdditional(string slug) => StableHash(slug + ":extra-cat") % 100 < 18;

    private static int DeterministicLeafProductCount(string fullKey)
    {
        // Varied 3/4/5 distribution (not uniform).
        var bucket = StableHash(fullKey) % 10;
        return bucket switch
        {
            < 4 => 3,
            < 8 => 4,
            _ => 5,
        };
    }

    private static IReadOnlyList<Guid> PickMediaSet(IReadOnlyList<Guid> pool, int index)
    {
        var start = (index - 1) % pool.Count;
        var list = new List<Guid>(5);
        for (var i = 0; i < 5; i++)
        {
            list.Add(pool[(start + i) % pool.Count]);
        }

        return list;
    }

    private static string SanitizeSlugKey(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
            else if (ch is '-' or '_')
            {
                sb.Append('-');
            }
        }

        var raw = sb.ToString().Trim('-');
        while (raw.Contains("--", StringComparison.Ordinal))
        {
            raw = raw.Replace("--", "-", StringComparison.Ordinal);
        }

        return raw;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static int StableHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return BinaryPrimitivesReadInt32(bytes) & 0x7fffffff;
    }

    private static int BinaryPrimitivesReadInt32(byte[] bytes) =>
        bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24);

    private readonly record struct LeafSpec(
        string FullKey,
        string RootKey,
        string L2Key,
        string Domain,
        CatalogDemoL3Spec Spec);
}
