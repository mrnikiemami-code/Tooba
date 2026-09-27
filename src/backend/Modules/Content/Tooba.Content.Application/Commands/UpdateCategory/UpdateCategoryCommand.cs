using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Commands.UpdateCategory;

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
            () => categories.UpdateAsync(request.CategoryId, new UpdateContentCategoryCommand(
                request.Name, request.Slug, request.ShortDescription, request.Description,
                request.SortOrder, request.Status), cancellationToken),
            ContentErrorCodes.UpdateRejected);
}
