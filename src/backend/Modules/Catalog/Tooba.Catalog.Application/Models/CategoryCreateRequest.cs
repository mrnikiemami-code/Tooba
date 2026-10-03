using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>درخواست ایجاد رده با ترجمه‌های صریح.</summary>
public sealed record CategoryCreateRequest(
    Guid? ParentCategoryId,
    int SortOrder,
    bool IsVisible,
    Guid? ImageMediaAssetId,
    Guid? IconMediaAssetId,
    Guid? BannerMediaAssetId,
    IReadOnlyList<CategoryTranslationUpsertRequest> Translations);
