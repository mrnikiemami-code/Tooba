using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Aggregates;

/// <summary>
/// آیتم presentation مگامنو؛ hierarchy منو از ParentMegaMenuItemId جدا از taxonomy رده است.
/// </summary>
public sealed class CatalogMegaMenuItem
{
    /// <summary>شناسهٔ آیتم منو.</summary>
    public Guid MegaMenuItemId { get; init; }

    /// <summary>نوع آیتم.</summary>
    public CatalogMegaMenuItemType ItemType { get; init; }

    /// <summary>ردهٔ مقصد (برای Category-backed).</summary>
    public Guid CategoryId { get; init; }

    /// <summary>والد presentation در درخت منو (نه ParentCategoryId).</summary>
    public Guid? ParentMegaMenuItemId { get; set; }

    /// <summary>ترتیب بین خواهران presentation.</summary>
    public int SortOrder { get; set; }

    /// <summary>نمایش در مگامنو.</summary>
    public bool IsVisible { get; set; }

    /// <summary>برجسته در منو.</summary>
    public bool IsFeatured { get; set; }

    /// <summary>تصویر تبلیغاتی اختیاری.</summary>
    public Guid? ImageMediaAssetId { get; set; }

    /// <summary>آیکن اختیاری.</summary>
    public Guid? IconMediaAssetId { get; set; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>ایجاد آیتم منو برای یک رده.</summary>
    public static CatalogMegaMenuItem BindCategory(
        Guid categoryId,
        Guid? parentMegaMenuItemId,
        int sortOrder,
        bool isVisible,
        bool isFeatured,
        Guid? imageMediaAssetId,
        Guid? iconMediaAssetId,
        DateTimeOffset now) =>
        new()
        {
            MegaMenuItemId = UuidV7.New(),
            ItemType = CatalogMegaMenuItemType.Category,
            CategoryId = categoryId,
            ParentMegaMenuItemId = parentMegaMenuItemId,
            SortOrder = sortOrder,
            IsVisible = isVisible,
            IsFeatured = isFeatured,
            ImageMediaAssetId = imageMediaAssetId,
            IconMediaAssetId = iconMediaAssetId,
            CreatedAt = now,
        };
}
