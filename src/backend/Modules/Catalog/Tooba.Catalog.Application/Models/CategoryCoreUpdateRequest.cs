using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>به‌روزرسانی هستهٔ غیرمحلی.</summary>
public sealed record CategoryCoreUpdateRequest(
    CatalogPublicationStatus? Status,
    int? SortOrder,
    bool? IsVisible,
    Guid? ImageMediaAssetId,
    Guid? IconMediaAssetId,
    Guid? BannerMediaAssetId = null,
    bool ClearImage = false,
    bool ClearIcon = false,
    bool ClearBanner = false,
    DateTimeOffset? ExpectedUpdatedAt = null);
