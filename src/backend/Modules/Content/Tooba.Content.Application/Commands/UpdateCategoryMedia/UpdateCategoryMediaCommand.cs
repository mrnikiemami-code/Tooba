using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Commands.UpdateCategoryMedia;

public sealed record UpdateCategoryMediaCommand(Guid CategoryId, Guid? ImageMediaAssetId)
    : IRequest<Result<ContentCategoryWorkspaceDto>>;

public sealed class UpdateCategoryMediaCommandHandler(IContentCategoryDirectory categories)
    : IRequestHandler<UpdateCategoryMediaCommand, Result<ContentCategoryWorkspaceDto>>
{
    public Task<Result<ContentCategoryWorkspaceDto>> Handle(
        UpdateCategoryMediaCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => categories.UpdateMediaAsync(request.CategoryId,
                new UpdateContentCategoryMediaCommand(request.ImageMediaAssetId), cancellationToken),
            ContentErrorCodes.UpdateRejected);
}
