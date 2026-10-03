using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>آیتم flat مگامنو برای Storefront.</summary>
public sealed record StorefrontMegaMenuItem(
    Guid MegaMenuItemId,
    Guid? ParentMegaMenuItemId,
    Guid CategoryId,
    string Title,
    string Destination,
    bool IsFeatured,
    Guid? IconMediaAssetId,
    Guid? ImageMediaAssetId,
    int SortOrder);
