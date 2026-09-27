using Tooba.Content.Application.Categories.Commands;
using Tooba.Content.Application.Categories.Models;

namespace Tooba.Content.Application.Categories.Ports;

/// <summary>دایرکتوری دسته‌بندی مقاله.</summary>
public interface IContentCategoryDirectory
{
    Task<IReadOnlyList<ContentCategoryTreeNodeDto>> GetTreeAsync(
        string languageCode, string? search, CancellationToken cancellationToken);

    Task<ContentCategoryWorkspaceDto?> GetWorkspaceAsync(Guid categoryId, CancellationToken cancellationToken);

    Task<ContentCategoryWorkspaceDto> CreateAsync(CreateCategoryCommand command, CancellationToken cancellationToken);

    Task<ContentCategoryWorkspaceDto> UpdateAsync(UpdateCategoryCommand command, CancellationToken cancellationToken);

    Task<ContentCategoryWorkspaceDto> UpdateSeoAsync(UpdateCategorySeoCommand command, CancellationToken cancellationToken);

    Task<ContentCategoryWorkspaceDto> UpdateMediaAsync(UpdateCategoryMediaCommand command, CancellationToken cancellationToken);

    Task<ContentCategoryWorkspaceDto> MoveAsync(MoveCategoryCommand command, CancellationToken cancellationToken);

    Task ReorderAsync(IReadOnlyList<ReorderContentCategoryItem> items, CancellationToken cancellationToken);

    Task ArchiveAsync(Guid categoryId, CancellationToken cancellationToken);

    Task<PublishedContentCategoryItem?> GetPublicBySlugAsync(
        string languageCode, string slug, CancellationToken cancellationToken);

    Task<IReadOnlyList<PublishedContentCategoryItem>> ListPublicAsync(
        string languageCode, CancellationToken cancellationToken);

    Task EnsureArticleCategoryLanguageMatchAsync(
        string articleLocale, Guid? categoryId, CancellationToken cancellationToken, bool isNewAssignment = true);
}
