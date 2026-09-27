using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Categories.Models;
using Tooba.Content.Application.Categories.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Categories.Commands;

public sealed record MoveCategoryCommand(Guid CategoryId, Guid? NewParentId)
    : IRequest<Result<ContentCategoryWorkspaceDto>>;

public sealed class MoveCategoryCommandHandler(IContentCategoryDirectory categories)
    : IRequestHandler<MoveCategoryCommand, Result<ContentCategoryWorkspaceDto>>
{
    public Task<Result<ContentCategoryWorkspaceDto>> Handle(
        MoveCategoryCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => categories.MoveAsync(request, cancellationToken),
            ContentErrorCodes.UpdateRejected);
}
