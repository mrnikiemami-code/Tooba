using Tooba.Content.Application.Authors.Commands;
using Tooba.Content.Application.Authors.Models;

namespace Tooba.Content.Application.Authors.Ports;

/// <summary>دایرکتوری نویسندهٔ مقاله.</summary>
public interface IContentAuthorDirectory
{
    Task<PublishedContentAuthorItem?> GetPublicBySlugAsync(
        string slug, string routeLocale, CancellationToken cancellationToken);

    Task<IReadOnlyList<PublishedContentAuthorItem>> ListPublicAsync(
        string routeLocale, CancellationToken cancellationToken);

    Task<ContentAuthorWorkspaceDto?> GetWorkspaceAsync(Guid authorId, CancellationToken cancellationToken);

    Task<ContentAuthorWorkspaceDto> CreateAsync(CreateAuthorCommand command, CancellationToken cancellationToken);

    Task<ContentAuthorWorkspaceDto> UpdateAsync(UpdateAuthorCommand command, CancellationToken cancellationToken);

    Task DeactivateAsync(Guid authorId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ContentAuthorPickerItemDto>> GetPickerListAsync(
        string? search, bool activeOnly, CancellationToken cancellationToken);

    Task EnsureArticleAuthorAssignmentAsync(
        Guid? authorId, bool isNewAssignment, CancellationToken cancellationToken);

    Task EnsurePublishableAuthorAsync(Guid? authorId, CancellationToken cancellationToken);
}
