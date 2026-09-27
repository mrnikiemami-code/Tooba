using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Categories.Models;
using Tooba.Content.Application.Categories.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Categories.Commands;

public sealed record UpdateCategorySeoCommand(Guid CategoryId, string? SeoTitle, string? SeoDescription)
    : IRequest<Result<ContentCategoryWorkspaceDto>>;

public sealed class UpdateCategorySeoCommandHandler(IContentCategoryDirectory categories)
    : IRequestHandler<UpdateCategorySeoCommand, Result<ContentCategoryWorkspaceDto>>
{
    public Task<Result<ContentCategoryWorkspaceDto>> Handle(
        UpdateCategorySeoCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => categories.UpdateSeoAsync(request, cancellationToken),
            ContentErrorCodes.UpdateRejected);
}
