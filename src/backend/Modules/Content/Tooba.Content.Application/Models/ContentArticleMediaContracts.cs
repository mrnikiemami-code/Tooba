namespace Tooba.Content.Application.Models;

/// <summary>ردیف گالری رسانهٔ مقاله.</summary>
public sealed record ArticleGalleryItemDto(
    Guid MediaAssetId,
    int DisplayOrder,
    string? AltText,
    string? Caption);

/// <summary>workspace رسانهٔ مقاله برای Admin.</summary>
public sealed record ArticleMediaWorkspaceDto(
    Guid ArticleId,
    Guid? FeaturedMediaAssetId,
    Guid? SeoImageMediaAssetId,
    Guid? EffectiveSeoImageMediaAssetId,
    IReadOnlyList<ArticleGalleryItemDto> Gallery);
