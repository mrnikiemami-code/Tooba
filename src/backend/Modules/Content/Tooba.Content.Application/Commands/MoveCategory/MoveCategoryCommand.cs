using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.MoveCategory;

public sealed record MoveCategoryCommand(Guid CategoryId, Guid? NewParentId)
    : IRequest<Result<ContentCategoryWorkspaceDto>>;

public sealed class MoveCategoryCommandHandler(IContentCategoryDirectory categories)
    : IRequestHandler<MoveCategoryCommand, Result<ContentCategoryWorkspaceDto>>
{
    public Task<Result<ContentCategoryWorkspaceDto>> Handle(
        MoveCategoryCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => categories.MoveAsync(request.CategoryId, new MoveContentCategoryCommand(request.NewParentId), cancellationToken),
            ContentErrorCodes.UpdateRejected);
}
