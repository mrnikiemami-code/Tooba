using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;

namespace Tooba.Content.Application.Models;

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

/// <summary>فرمان ایجاد دسته.</summary>
public sealed record CreateContentCategoryCommand(
    string LanguageCode,
    Guid? ParentCategoryId,
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    int SortOrder);

/// <summary>فرمان به‌روزرسانی عمومی دسته.</summary>
public sealed record UpdateContentCategoryCommand(
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    int SortOrder,
    string Status);

/// <summary>فرمان به‌روزرسانی SEO دسته.</summary>
public sealed record UpdateContentCategorySeoCommand(string? SeoTitle, string? SeoDescription);

/// <summary>فرمان به‌روزرسانی رسانه دسته.</summary>
public sealed record UpdateContentCategoryMediaCommand(Guid? ImageMediaAssetId);

/// <summary>فرمان جابه‌جایی والد.</summary>
public sealed record MoveContentCategoryCommand(Guid? NewParentId);

/// <summary>آیتم مرتب‌سازی مجدد.</summary>
public sealed record ReorderContentCategoryItem(Guid CategoryId, int SortOrder);
