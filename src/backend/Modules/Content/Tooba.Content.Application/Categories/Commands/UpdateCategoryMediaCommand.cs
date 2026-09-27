using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Categories.Models;
using Tooba.Content.Application.Categories.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Categories.Commands;

public sealed record UpdateCategoryMediaCommand(Guid CategoryId, Guid? ImageMediaAssetId)
    : IRequest<Result<ContentCategoryWorkspaceDto>>;

public sealed class UpdateCategoryMediaCommandHandler(IContentCategoryDirectory categories)
    : IRequestHandler<UpdateCategoryMediaCommand, Result<ContentCategoryWorkspaceDto>>
{
    public Task<Result<ContentCategoryWorkspaceDto>> Handle(
        UpdateCategoryMediaCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => categories.UpdateMediaAsync(request, cancellationToken),
            ContentErrorCodes.UpdateRejected);
}
