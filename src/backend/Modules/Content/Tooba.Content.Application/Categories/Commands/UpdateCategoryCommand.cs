using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Categories.Models;
using Tooba.Content.Application.Categories.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Categories.Commands;

public sealed record UpdateCategoryCommand(
    Guid CategoryId, string Name, string Slug, string? ShortDescription,
    string? Description, int SortOrder, string Status)
    : IRequest<Result<ContentCategoryWorkspaceDto>>;

public sealed class UpdateCategoryCommandHandler(IContentCategoryDirectory categories)
    : IRequestHandler<UpdateCategoryCommand, Result<ContentCategoryWorkspaceDto>>
{
    public Task<Result<ContentCategoryWorkspaceDto>> Handle(
        UpdateCategoryCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => categories.UpdateAsync(request, cancellationToken),
            ContentErrorCodes.UpdateRejected);
}
