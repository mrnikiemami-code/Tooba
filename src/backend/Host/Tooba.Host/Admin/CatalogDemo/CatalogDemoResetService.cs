using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Media.Application;
using Tooba.Media.Infrastructure.Persistence;

namespace Tooba.Host.Admin.CatalogDemo;

/// <summary>نتیجهٔ شمارشی reset.</summary>
public sealed record CatalogDemoResetResult(
    int ProductsRemoved,
    int CategoriesRemoved,
    int BrandsRemoved,
    int TagsRemoved,
    int AttributesRemoved,
    int MediaRemoved,
    int MegaMenuRemoved);

/// <summary>
/// بازنشانی Catalog demo در محیط غیر Production:
/// همهٔ Products Catalog را پاک می‌کند (نه فقط پیشوند demo)، سپس foundation/demo seam را.
/// بدون دست زدن به auth/orders/seller.
/// </summary>
public sealed class CatalogDemoResetService
{
    private readonly CatalogDbContext _catalog;
    private readonly MediaDbContext _media;
    private readonly IMediaObjectStore _store;

    /// <summary>DbContextهای Catalog/Media را تزریق می‌کند.</summary>
    public CatalogDemoResetService(CatalogDbContext catalog, MediaDbContext media, IMediaObjectStore store)
    {
        _catalog = catalog;
        _media = media;
        _store = store;
    }

    /// <summary>بازنشانی دمو را در یک تراکنش Catalog اجرا می‌کند؛ Media جداگانه پاک می‌شود.</summary>
    public async Task<CatalogDemoResetResult> ResetAsync(CancellationToken cancellationToken)
    {
        await using var tx = await _catalog.Database.BeginTransactionAsync(cancellationToken);

        // T034-R1: همهٔ Products Catalog (شامل residual Published بدون پیشوند demo) پاک می‌شوند.
        var allProductIds = await _catalog.Products.AsNoTracking()
            .Select(p => p.ProductId)
            .ToListAsync(cancellationToken);
        var productIdSet = allProductIds.ToHashSet();

        var categoryIdsFromProducts = await _catalog.ProductCategories.AsNoTracking()
            .Where(pc => productIdSet.Contains(pc.ProductId))
            .Select(pc => pc.CategoryId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var demoCategoryIds = await ResolveDemoCategoryIdsAsync(categoryIdsFromProducts, cancellationToken);

        var megaRemoved = await DeleteMegaMenuForCategoriesAsync(demoCategoryIds, cancellationToken);
        var productsRemoved = await DeleteProductsAsync(productIdSet, cancellationToken);
        var categoriesRemoved = await DeleteCategoriesAsync(demoCategoryIds, cancellationToken);

        var brandsRemoved = await DeleteDemoBrandsAsync(cancellationToken);
        var tagsRemoved = await DeleteDemoTagsAsync(cancellationToken);
        var attrsRemoved = await DeleteDemoAttributesAsync(cancellationToken);

        await _catalog.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var mediaRemoved = await DeleteDemoMediaAsync(cancellationToken);

        return new CatalogDemoResetResult(
            productsRemoved,
            categoriesRemoved,
            brandsRemoved,
            tagsRemoved,
            attrsRemoved,
            mediaRemoved,
            megaRemoved);
    }

    private async Task<HashSet<Guid>> ResolveDemoCategoryIdsAsync(
        IReadOnlyList<Guid> fromProducts,
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<Guid>(fromProducts);
        var demoSlugs = await _catalog.CategoryTranslations.AsNoTracking()
            .Where(t => t.Slug.StartsWith(CatalogDemoSeam.CategorySlugPrefix))
            .Select(t => t.CategoryId)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var id in demoSlugs)
        {
            ids.Add(id);
        }

        // بستن درخت: همهٔ اجداد رده‌های هدف تا ریشه (برای پاکسازی درخت junk قدیمی).
        var parentMap = await _catalog.Categories.AsNoTracking()
            .ToDictionaryAsync(c => c.CategoryId, c => c.ParentCategoryId, cancellationToken);
        var queue = new Queue<Guid>(ids);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!parentMap.TryGetValue(current, out var parent) || parent is not Guid p || !ids.Add(p))
            {
                continue;
            }

            queue.Enqueue(p);
        }

        // فرزندان رده‌های demo-cat-* را هم جمع کن.
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var (id, parent) in parentMap)
            {
                if (parent is Guid p && ids.Contains(p) && ids.Add(id))
                {
                    changed = true;
                }
            }
        }

        return ids;
    }

    private async Task<int> DeleteMegaMenuForCategoriesAsync(HashSet<Guid> categoryIds, CancellationToken cancellationToken)
    {
        if (categoryIds.Count == 0)
        {
            return 0;
        }

        var items = await _catalog.MegaMenuItems
            .Where(m => categoryIds.Contains(m.CategoryId))
            .ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            return 0;
        }

        var itemIds = items.Select(i => i.MegaMenuItemId).ToHashSet();
        // ابتدا فرزند presentation، سپس والد (Restrict روی ParentMegaMenuItemId).
        var ordered = TopologicalMegaMenu(items);
        var translations = await _catalog.MegaMenuItemTranslations
            .Where(t => itemIds.Contains(t.MegaMenuItemId))
            .ToListAsync(cancellationToken);
        _catalog.MegaMenuItemTranslations.RemoveRange(translations);
        foreach (var item in ordered)
        {
            _catalog.MegaMenuItems.Remove(item);
        }

        await _catalog.SaveChangesAsync(cancellationToken);
        return items.Count;
    }

    private static List<CatalogMegaMenuItem> TopologicalMegaMenu(List<CatalogMegaMenuItem> items)
    {
        var byId = items.ToDictionary(i => i.MegaMenuItemId);
        var remaining = items.ToHashSet();
        var result = new List<CatalogMegaMenuItem>();
        while (remaining.Count > 0)
        {
            var leaves = remaining
                .Where(i => remaining.All(o => o.ParentMegaMenuItemId != i.MegaMenuItemId))
                .ToList();
            if (leaves.Count == 0)
            {
                result.AddRange(remaining);
                break;
            }

            foreach (var leaf in leaves)
            {
                result.Add(leaf);
                remaining.Remove(leaf);
            }
        }

        return result;
    }

    private async Task<int> DeleteProductsAsync(HashSet<Guid> productIds, CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return 0;
        }

        _catalog.ProductTagAssignments.RemoveRange(
            await _catalog.ProductTagAssignments.Where(x => productIds.Contains(x.ProductId)).ToListAsync(cancellationToken));
        _catalog.ProductCategories.RemoveRange(
            await _catalog.ProductCategories.Where(x => productIds.Contains(x.ProductId)).ToListAsync(cancellationToken));
        _catalog.MediaReferences.RemoveRange(
            await _catalog.MediaReferences.Where(x => productIds.Contains(x.ProductId)).ToListAsync(cancellationToken));
        _catalog.ProductAttributeValues.RemoveRange(
            await _catalog.ProductAttributeValues.Where(x => productIds.Contains(x.ProductId)).ToListAsync(cancellationToken));
        _catalog.ProductVariantAxes.RemoveRange(
            await _catalog.ProductVariantAxes.Where(x => productIds.Contains(x.ProductId)).ToListAsync(cancellationToken));
        _catalog.ProductHistoryEntries.RemoveRange(
            await _catalog.ProductHistoryEntries.Where(x => productIds.Contains(x.ProductId)).ToListAsync(cancellationToken));

        var variants = await _catalog.Variants.Where(v => productIds.Contains(v.ProductId)).ToListAsync(cancellationToken);
        var variantIds = variants.Select(v => v.VariantId).ToHashSet();
        _catalog.VariantAttributeValues.RemoveRange(
            await _catalog.VariantAttributeValues.Where(x => variantIds.Contains(x.VariantId)).ToListAsync(cancellationToken));
        _catalog.Variants.RemoveRange(variants);

        _catalog.LocalizedTexts.RemoveRange(
            await _catalog.LocalizedTexts
                .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Product && productIds.Contains(t.OwnerId))
                .ToListAsync(cancellationToken));

        var products = await _catalog.Products.Where(p => productIds.Contains(p.ProductId)).ToListAsync(cancellationToken);
        _catalog.Products.RemoveRange(products);
        await _catalog.SaveChangesAsync(cancellationToken);
        return products.Count;
    }

    /// <summary>
    /// حذف انتخابی محصولات Catalog و وابسته‌های Catalog-owned (برای پاکسازی انتساب نامعتبر غیر Production).
    /// </summary>
    public Task<int> DeleteProductsByIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken) =>
        DeleteProductsAsync(productIds.ToHashSet(), cancellationToken);

    private async Task<int> DeleteCategoriesAsync(HashSet<Guid> categoryIds, CancellationToken cancellationToken)
    {
        if (categoryIds.Count == 0)
        {
            return 0;
        }

        _catalog.CategoryTagAssignments.RemoveRange(
            await _catalog.CategoryTagAssignments.Where(x => categoryIds.Contains(x.CategoryId)).ToListAsync(cancellationToken));
        _catalog.CategoryAttributeBindings.RemoveRange(
            await _catalog.CategoryAttributeBindings.Where(x => categoryIds.Contains(x.CategoryId)).ToListAsync(cancellationToken));
        _catalog.CategoryFacetConfigurations.RemoveRange(
            await _catalog.CategoryFacetConfigurations.Where(x => categoryIds.Contains(x.CategoryId)).ToListAsync(cancellationToken));
        _catalog.ProductCategories.RemoveRange(
            await _catalog.ProductCategories.Where(x => categoryIds.Contains(x.CategoryId)).ToListAsync(cancellationToken));

        // حذف از عمیق‌ترین سطح به ریشه (Restrict روی ParentCategoryId).
        var categories = await _catalog.Categories.Where(c => categoryIds.Contains(c.CategoryId)).ToListAsync(cancellationToken);
        var parentMap = categories.ToDictionary(c => c.CategoryId, c => c.ParentCategoryId);
        var ordered = categories
            .OrderByDescending(c => CatalogCategoryTreeRules.GetCategoryLevel(c.CategoryId, parentMap))
            .ToList();

        _catalog.CategoryTranslations.RemoveRange(
            await _catalog.CategoryTranslations.Where(t => categoryIds.Contains(t.CategoryId)).ToListAsync(cancellationToken));
        _catalog.CategorySlugHistories.RemoveRange(
            await _catalog.CategorySlugHistories.Where(t => categoryIds.Contains(t.CategoryId)).ToListAsync(cancellationToken));
        _catalog.LocalizedTexts.RemoveRange(
            await _catalog.LocalizedTexts
                .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Category && categoryIds.Contains(t.OwnerId))
                .ToListAsync(cancellationToken));

        foreach (var category in ordered)
        {
            _catalog.Categories.Remove(category);
        }

        await _catalog.SaveChangesAsync(cancellationToken);
        return ordered.Count;
    }

    private async Task<int> DeleteDemoBrandsAsync(CancellationToken cancellationToken)
    {
        var brands = await _catalog.Brands.ToListAsync(cancellationToken);
        var toRemove = brands.Where(b => CatalogDemoSeam.IsDemoOrJunkBrandSlug(b.SlugSeam)).ToList();
        if (toRemove.Count == 0)
        {
            return 0;
        }

        var ids = toRemove.Select(b => b.BrandId).ToHashSet();
        _catalog.LocalizedTexts.RemoveRange(
            await _catalog.LocalizedTexts
                .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Brand && ids.Contains(t.OwnerId))
                .ToListAsync(cancellationToken));
        _catalog.Brands.RemoveRange(toRemove);
        await _catalog.SaveChangesAsync(cancellationToken);
        return toRemove.Count;
    }

    private async Task<int> DeleteDemoTagsAsync(CancellationToken cancellationToken)
    {
        var tags = await _catalog.Tags
            .Where(t => t.Code.StartsWith(CatalogDemoSeam.TagCodePrefix))
            .ToListAsync(cancellationToken);
        if (tags.Count == 0)
        {
            return 0;
        }

        var ids = tags.Select(t => t.TagId).ToHashSet();
        _catalog.ProductTagAssignments.RemoveRange(
            await _catalog.ProductTagAssignments.Where(x => ids.Contains(x.TagId)).ToListAsync(cancellationToken));
        _catalog.CategoryTagAssignments.RemoveRange(
            await _catalog.CategoryTagAssignments.Where(x => ids.Contains(x.TagId)).ToListAsync(cancellationToken));
        _catalog.LocalizedTexts.RemoveRange(
            await _catalog.LocalizedTexts
                .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Tag && ids.Contains(t.OwnerId))
                .ToListAsync(cancellationToken));
        _catalog.Tags.RemoveRange(tags);
        await _catalog.SaveChangesAsync(cancellationToken);
        return tags.Count;
    }

    private async Task<int> DeleteDemoAttributesAsync(CancellationToken cancellationToken)
    {
        var defs = await _catalog.AttributeDefinitions.ToListAsync(cancellationToken);
        var toRemove = defs.Where(d => CatalogDemoSeam.IsDemoOrJunkAttributeCode(d.Code)).ToList();
        if (toRemove.Count == 0)
        {
            return 0;
        }

        var defIds = toRemove.Select(d => d.DefinitionId).ToHashSet();
        _catalog.CategoryAttributeBindings.RemoveRange(
            await _catalog.CategoryAttributeBindings.Where(x => defIds.Contains(x.DefinitionId)).ToListAsync(cancellationToken));
        _catalog.CategoryFacetConfigurations.RemoveRange(
            await _catalog.CategoryFacetConfigurations.Where(x => defIds.Contains(x.DefinitionId)).ToListAsync(cancellationToken));
        _catalog.ProductAttributeValues.RemoveRange(
            await _catalog.ProductAttributeValues.Where(x => defIds.Contains(x.DefinitionId)).ToListAsync(cancellationToken));
        _catalog.VariantAttributeValues.RemoveRange(
            await _catalog.VariantAttributeValues.Where(x => defIds.Contains(x.DefinitionId)).ToListAsync(cancellationToken));
        _catalog.ProductVariantAxes.RemoveRange(
            await _catalog.ProductVariantAxes.Where(x => defIds.Contains(x.DefinitionId)).ToListAsync(cancellationToken));

        var options = await _catalog.AttributeOptions.Where(o => defIds.Contains(o.DefinitionId)).ToListAsync(cancellationToken);
        var optionIds = options.Select(o => o.OptionId).ToHashSet();
        _catalog.LocalizedTexts.RemoveRange(
            await _catalog.LocalizedTexts
                .Where(t =>
                    (t.OwnerKind == CatalogLocalizedOwnerKind.AttributeDefinition && defIds.Contains(t.OwnerId))
                    || (t.OwnerKind == CatalogLocalizedOwnerKind.AttributeOption && optionIds.Contains(t.OwnerId)))
                .ToListAsync(cancellationToken));
        _catalog.AttributeOptions.RemoveRange(options);
        _catalog.AttributeDefinitions.RemoveRange(toRemove);
        await _catalog.SaveChangesAsync(cancellationToken);
        return toRemove.Count;
    }

    private async Task<int> DeleteDemoMediaAsync(CancellationToken cancellationToken)
    {
        var assets = await _media.Assets
            .Where(a => a.OriginalFileName.StartsWith(CatalogDemoSeam.MediaFilePrefix))
            .ToListAsync(cancellationToken);
        if (assets.Count == 0)
        {
            return 0;
        }

        foreach (var asset in assets)
        {
            try
            {
                await _store.DeleteAsync(asset.StorageKey, cancellationToken);
            }
            catch
            {
                // فایل ممکن است از قبل حذف شده باشد.
            }
        }

        _media.Assets.RemoveRange(assets);
        await _media.SaveChangesAsync(cancellationToken);
        return assets.Count;
    }
}
