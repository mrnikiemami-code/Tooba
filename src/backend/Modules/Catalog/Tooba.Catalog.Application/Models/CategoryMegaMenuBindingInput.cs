using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>ورودی bind/update مگامنو برای یک رده.</summary>
public sealed record CategoryMegaMenuBindingInput(
    Guid? ParentMegaMenuItemId,
    int SortOrder,
    bool IsVisible,
    bool IsFeatured,
    Guid? ImageMediaAssetId,
    Guid? IconMediaAssetId,
    string? TitleOverride,
    string? BadgeText,
    string? ShortLabel);
