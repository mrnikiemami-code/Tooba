using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>خلاصهٔ workspace رده برای Admin.</summary>
public sealed record CategoryWorkspaceSummaryDto(
    Guid CategoryId,
    Guid? ParentCategoryId,
    CatalogPublicationStatus Status,
    int SortOrder,
    bool IsVisible,
    Guid? ImageMediaAssetId,
    Guid? IconMediaAssetId,
    Guid? BannerMediaAssetId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<CategoryTranslationDto> Translations);
