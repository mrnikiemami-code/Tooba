using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.ValueObjects;

/// <summary>ردیف renderable مگامنو برای Storefront.</summary>
public sealed record CatalogMegaMenuRenderableItem(
    Guid MegaMenuItemId,
    Guid? ParentMegaMenuItemId,
    Guid CategoryId,
    string Title,
    string Destination,
    bool IsFeatured,
    Guid? IconMediaAssetId,
    Guid? ImageMediaAssetId,
    int SortOrder);
