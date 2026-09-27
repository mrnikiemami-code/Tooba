using Tooba.Content.Application.Models;

namespace Tooba.Content.Application.Ports;

/// <summary>دایرکتوری نویسندهٔ مقاله.</summary>
public interface IContentAuthorDirectory
{
    /// <summary>نویسندهٔ Active عمومی را با slug برمی‌گرداند.</summary>
    Task<PublishedContentAuthorItem?> GetPublicBySlugAsync(
        string slug,
        string routeLocale,
        CancellationToken cancellationToken);

    /// <summary>فهرست نویسندگان Active برای sitemap.</summary>
    Task<IReadOnlyList<PublishedContentAuthorItem>> ListPublicAsync(
        string routeLocale,
        CancellationToken cancellationToken);

    /// <summary>workspace یک نویسنده را برمی‌گرداند.</summary>
    Task<ContentAuthorWorkspaceDto?> GetWorkspaceAsync(Guid authorId, CancellationToken cancellationToken);

    /// <summary>نویسندهٔ جدید می‌سازد.</summary>
    Task<ContentAuthorWorkspaceDto> CreateAsync(CreateContentAuthorCommand command, CancellationToken cancellationToken);

    /// <summary>فیلدهای عمومی را به‌روزرسانی می‌کند.</summary>
    Task<ContentAuthorWorkspaceDto> UpdateAsync(
        Guid authorId,
        UpdateContentAuthorCommand command,
        CancellationToken cancellationToken);

    /// <summary>نویسنده را غیرفعال می‌کند.</summary>
    Task DeactivateAsync(Guid authorId, CancellationToken cancellationToken);

    /// <summary>فهرست picker نویسنده‌ها را برمی‌گرداند.</summary>
    Task<IReadOnlyList<ContentAuthorPickerItemDto>> GetPickerListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken);

    /// <summary>انتساب نویسنده به مقاله را تضمین می‌کند.</summary>
    Task EnsureArticleAuthorAssignmentAsync(
        Guid? authorId,
        bool isNewAssignment,
        CancellationToken cancellationToken);

    /// <summary>قابلیت انتشار مقاله با نویسندهٔ فعلی را تضمین می‌کند.</summary>
    Task EnsurePublishableAuthorAsync(Guid? authorId, CancellationToken cancellationToken);
}
