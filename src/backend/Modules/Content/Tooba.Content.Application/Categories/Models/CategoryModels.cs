namespace Tooba.Content.Application.Categories.Models;

/// <summary>گره درخت دسته‌بندی مقاله برای Admin.</summary>
public sealed record ContentCategoryTreeNodeDto(
    Guid Id,
    string LanguageCode,
    Guid? ParentId,
    string Name,
    string Slug,
    string Status,
    int SortOrder,
    bool HasChildren,
    int ArticleCount);

/// <summary>workspace دستهٔ انتخاب‌شده.</summary>
public sealed record ContentCategoryWorkspaceDto(
    Guid Id,
    string LanguageCode,
    Guid? ParentId,
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    string Status,
    int SortOrder,
    string? SeoTitle,
    string? SeoDescription,
    Guid? ImageMediaAssetId,
    int ArticleCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>آیتم مرتب‌سازی مجدد دسته‌ها.</summary>
public sealed record ReorderContentCategoryItem(Guid CategoryId, int SortOrder);

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
