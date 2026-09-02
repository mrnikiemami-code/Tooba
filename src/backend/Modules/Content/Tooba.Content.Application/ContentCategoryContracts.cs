using Tooba.Content.Domain;

namespace Tooba.Content.Application;

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

/// <summary>دایرکتوری دسته‌بندی مقاله.</summary>
public interface IContentCategoryDirectory
{
    /// <summary>درخت دسته‌ها را برای یک زبان برمی‌گرداند.</summary>
    Task<IReadOnlyList<ContentCategoryTreeNodeDto>> GetTreeAsync(
        string languageCode,
        string? search,
        CancellationToken cancellationToken);

    /// <summary>workspace یک دسته را برمی‌گرداند.</summary>
    Task<ContentCategoryWorkspaceDto?> GetWorkspaceAsync(Guid categoryId, CancellationToken cancellationToken);

    /// <summary>دستهٔ جدید می‌سازد.</summary>
    Task<ContentCategoryWorkspaceDto> CreateAsync(CreateContentCategoryCommand command, CancellationToken cancellationToken);

    /// <summary>فیلدهای عمومی را به‌روزرسانی می‌کند.</summary>
    Task<ContentCategoryWorkspaceDto> UpdateAsync(
        Guid categoryId,
        UpdateContentCategoryCommand command,
        CancellationToken cancellationToken);

    /// <summary>SEO را به‌روزرسانی می‌کند.</summary>
    Task<ContentCategoryWorkspaceDto> UpdateSeoAsync(
        Guid categoryId,
        UpdateContentCategorySeoCommand command,
        CancellationToken cancellationToken);

    /// <summary>رسانه را به‌روزرسانی می‌کند.</summary>
    Task<ContentCategoryWorkspaceDto> UpdateMediaAsync(
        Guid categoryId,
        UpdateContentCategoryMediaCommand command,
        CancellationToken cancellationToken);

    /// <summary>والد را جابه‌جا می‌کند.</summary>
    Task<ContentCategoryWorkspaceDto> MoveAsync(
        Guid categoryId,
        MoveContentCategoryCommand command,
        CancellationToken cancellationToken);

    /// <summary>ترتیب خواهر/برادرها را به‌روزرسانی می‌کند.</summary>
    Task ReorderAsync(IReadOnlyList<ReorderContentCategoryItem> items, CancellationToken cancellationToken);

    /// <summary>دسته را بایگانی می‌کند.</summary>
    Task ArchiveAsync(Guid categoryId, CancellationToken cancellationToken);

    /// <summary>هم‌خوانی زبان مقاله و دسته را تضمین می‌کند.</summary>
    Task EnsureArticleCategoryLanguageMatchAsync(
        string articleLocale,
        Guid? categoryId,
        CancellationToken cancellationToken);
}
