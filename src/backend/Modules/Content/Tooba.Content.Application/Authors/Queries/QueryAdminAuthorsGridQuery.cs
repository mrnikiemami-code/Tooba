using MediatR;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Authors.Models;
using Tooba.Content.Application.Authors.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Authors.Queries;

public sealed record QueryAdminAuthorsGridQuery(GridQueryRequest Request)
    : IRequest<Result<GridPageResponse<ContentAuthorGridRowDto>>>;

public sealed class QueryAdminAuthorsGridQueryHandler(IContentAuthorGridPort grid)
    : IRequestHandler<QueryAdminAuthorsGridQuery, Result<GridPageResponse<ContentAuthorGridRowDto>>>
{
    public Task<Result<GridPageResponse<ContentAuthorGridRowDto>>> Handle(
        QueryAdminAuthorsGridQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(() => grid.QueryAsync(request.Request, cancellationToken), ContentErrorCodes.AuthorNotFound);
}
