using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>نمای Admin پیکربندی مگامنو یک رده.</summary>
public sealed record CategoryMegaMenuConfigurationView(
    Guid CategoryId,
    bool IsBound,
    Guid? MegaMenuItemId,
    Guid? ParentMegaMenuItemId,
    string? ParentMenuPath,
    int SortOrder,
    bool IsVisible,
    bool IsFeatured,
    Guid? ImageMediaAssetId,
    Guid? IconMediaAssetId,
    string DisplayTitle,
    string? TitleOverride,
    string? BadgeText,
    string? ShortLabel,
    string DestinationPreview,
    int PresentationLevel,
    bool CategoryPublished,
    bool CategoryVisible);
