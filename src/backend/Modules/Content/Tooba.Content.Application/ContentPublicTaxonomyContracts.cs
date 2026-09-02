namespace Tooba.Content.Application;

/// <summary>نمای عمومی دستهٔ مقاله.</summary>
public sealed record PublishedContentCategoryItem(
    Guid CategoryId,
    string LanguageCode,
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    Guid? ImageMediaAssetId,
    string CanonicalPath);

/// <summary>نمای عمومی نویسندهٔ مقاله.</summary>
public sealed record PublishedContentAuthorItem(
    Guid AuthorId,
    string DisplayName,
    string Slug,
    string? ShortBio,
    string? FullBio,
    Guid? ProfileImageMediaAssetId,
    Guid? CoverImageMediaAssetId,
    string CanonicalPath);
