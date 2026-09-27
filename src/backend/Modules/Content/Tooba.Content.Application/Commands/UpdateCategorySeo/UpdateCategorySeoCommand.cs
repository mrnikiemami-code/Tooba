using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.UpdateCategorySeo;

public sealed record UpdateCategorySeoCommand(Guid CategoryId, string? SeoTitle, string? SeoDescription)
    : IRequest<Result<ContentCategoryWorkspaceDto>>;

public sealed class UpdateCategorySeoCommandHandler(IContentCategoryDirectory categories)
    : IRequestHandler<UpdateCategorySeoCommand, Result<ContentCategoryWorkspaceDto>>
{
    public Task<Result<ContentCategoryWorkspaceDto>> Handle(
        UpdateCategorySeoCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => categories.UpdateSeoAsync(request.CategoryId,
                new UpdateContentCategorySeoCommand(request.SeoTitle, request.SeoDescription), cancellationToken),
            ContentErrorCodes.UpdateRejected);
}
