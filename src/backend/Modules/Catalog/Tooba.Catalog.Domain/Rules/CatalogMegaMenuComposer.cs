using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Rules;

/// <summary>
/// حل عنوان و eligibility آیتم مگامنو برای locale.
/// </summary>
public static class CatalogMegaMenuComposer
{
    /// <summary>
    /// آیتم‌های قابل نمایش در ویترین را فیلتر و مرتب می‌کند.
    /// </summary>
    public static IReadOnlyList<CatalogMegaMenuRenderableItem> ComposeStorefrontMenu(
        IReadOnlyList<CatalogMegaMenuItem> items,
        IReadOnlyDictionary<Guid, CatalogCategory> categoriesById,
        IReadOnlyDictionary<Guid, CatalogCategoryTranslation> translationsByCategoryId,
        IReadOnlyDictionary<Guid, CatalogMegaMenuItemTranslation> overridesByItemId,
        string locale,
        string uiLocaleSegment)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(categoriesById);
        ArgumentNullException.ThrowIfNull(translationsByCategoryId);
        ArgumentNullException.ThrowIfNull(overridesByItemId);

        var normalizedLocale = locale.Trim();
        var result = new List<CatalogMegaMenuRenderableItem>();
        foreach (var item in items.Where(x => x.IsVisible && x.ItemType == CatalogMegaMenuItemType.Category))
        {
            if (!categoriesById.TryGetValue(item.CategoryId, out var category))
            {
                continue;
            }

            if (category.Status != CatalogPublicationStatus.Published || !category.IsVisible)
            {
                continue;
            }

            if (!translationsByCategoryId.TryGetValue(item.CategoryId, out var translation)
                || string.IsNullOrWhiteSpace(translation.Slug))
            {
                continue;
            }

            overridesByItemId.TryGetValue(item.MegaMenuItemId, out var overrideRow);
            var title = overrideRow?.TitleOverride ?? translation.Name;
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var destination = $"/{uiLocaleSegment}/category/{translation.Slug.Trim()}";
            result.Add(new CatalogMegaMenuRenderableItem(
                item.MegaMenuItemId,
                item.ParentMegaMenuItemId,
                item.CategoryId,
                title,
                destination,
                item.IsFeatured,
                item.IconMediaAssetId ?? category.IconMediaAssetId,
                item.ImageMediaAssetId ?? category.ImageMediaAssetId,
                item.SortOrder));
        }

        return result
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title, StringComparer.Ordinal)
            .ToList();
    }
}
