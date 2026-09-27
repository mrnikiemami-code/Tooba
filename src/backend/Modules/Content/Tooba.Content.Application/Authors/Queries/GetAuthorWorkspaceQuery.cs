using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Authors.Models;
using Tooba.Content.Application.Authors.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Authors.Queries;

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
