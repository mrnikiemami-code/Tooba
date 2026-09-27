namespace Tooba.Content.Application.Authors.Models;

/// <summary>ردیف گرید نویسندهٔ مقاله برای Admin.</summary>
public sealed record ContentAuthorGridRowDto(
    Guid Id,
    string DisplayName,
    string Slug,
    bool IsActive,
    int ArticleCount,
    DateTimeOffset UpdatedAt);

/// <summary>workspace نویسندهٔ انتخاب‌شده.</summary>
public sealed record ContentAuthorWorkspaceDto(
    Guid Id,
    string DisplayName,
    string Slug,
    bool IsActive,
    Guid? ProfileImageMediaAssetId,
    Guid? CoverImageMediaAssetId,
    string? ShortBio,
    string? FullBio,
    string? WebsiteUrl,
    string? InstagramUrl,
    string? TwitterUrl,
    string? LinkedInUrl,
    int ArticleCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>آیتم picker نویسنده.</summary>
public sealed record ContentAuthorPickerItemDto(
    Guid Id,
    string DisplayName,
    string Slug,
    bool IsActive);

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
