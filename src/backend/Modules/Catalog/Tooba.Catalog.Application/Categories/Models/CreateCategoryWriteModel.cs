using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Categories.Models;

/// <summary>Create Category write model (structured Translations and/or legacy LocalizedNames).</summary>
public sealed record CreateCategoryWriteModel(
    Guid? ParentCategoryId,
    int SortOrder,
    bool IsVisible,
    Guid? ImageMediaAssetId,
    Guid? IconMediaAssetId,
    Guid? BannerMediaAssetId,
    List<CategoryTranslationUpsertRequest>? Translations,
    Dictionary<string, string>? LocalizedNames);
