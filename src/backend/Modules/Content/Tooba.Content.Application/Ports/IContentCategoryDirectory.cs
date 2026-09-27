using Tooba.Content.Application.Models;

namespace Tooba.Content.Application.Ports;

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

    /// <summary>دستهٔ Active عمومی را با زبان+slug برمی‌گرداند.</summary>
    Task<PublishedContentCategoryItem?> GetPublicBySlugAsync(
        string languageCode,
        string slug,
        CancellationToken cancellationToken);

    /// <summary>فهرست دسته‌های Active یک زبان برای sitemap.</summary>
    Task<IReadOnlyList<PublishedContentCategoryItem>> ListPublicAsync(
        string languageCode,
        CancellationToken cancellationToken);

    /// <summary>هم‌خوانی زبان مقاله و دسته را تضمین می‌کند.</summary>
    Task EnsureArticleCategoryLanguageMatchAsync(
        string articleLocale,
        Guid? categoryId,
        CancellationToken cancellationToken,
        bool isNewAssignment = true);
}
