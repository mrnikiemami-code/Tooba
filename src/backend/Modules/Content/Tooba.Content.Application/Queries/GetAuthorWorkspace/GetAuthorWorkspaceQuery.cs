using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Queries.GetAuthorWorkspace;

public sealed record GetAuthorWorkspaceQuery(Guid AuthorId)
    : IRequest<Result<ContentAuthorWorkspaceDto>>;

public sealed class GetAuthorWorkspaceQueryHandler(IContentAuthorDirectory authors)
    : IRequestHandler<GetAuthorWorkspaceQuery, Result<ContentAuthorWorkspaceDto>>
{
    public async Task<Result<ContentAuthorWorkspaceDto>> Handle(
        GetAuthorWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var item = await authors.GetWorkspaceAsync(request.AuthorId, cancellationToken);
        return ContentOperation.NotFoundIfNull(item, ContentErrorCodes.AuthorNotFound);
    }
}
