using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Queries.GetCategoryWorkspace;

public sealed record GetCategoryWorkspaceQuery(Guid CategoryId)
    : IRequest<Result<ContentCategoryWorkspaceDto>>;

public sealed class GetCategoryWorkspaceQueryHandler(IContentCategoryDirectory categories)
    : IRequestHandler<GetCategoryWorkspaceQuery, Result<ContentCategoryWorkspaceDto>>
{
    public async Task<Result<ContentCategoryWorkspaceDto>> Handle(
        GetCategoryWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var item = await categories.GetWorkspaceAsync(request.CategoryId, cancellationToken);
        return ContentOperation.NotFoundIfNull(item, ContentErrorCodes.CategoryNotFound);
    }
}
