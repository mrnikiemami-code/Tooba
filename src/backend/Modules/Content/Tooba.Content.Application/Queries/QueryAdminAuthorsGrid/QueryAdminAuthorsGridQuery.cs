using MediatR;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Queries.QueryAdminAuthorsGrid;

public sealed record QueryAdminAuthorsGridQuery(GridQueryRequest Request)
    : IRequest<Result<GridPageResponse<ContentAuthorGridRowDto>>>;

public sealed class QueryAdminAuthorsGridQueryHandler(IContentAuthorGridPort grid)
    : IRequestHandler<QueryAdminAuthorsGridQuery, Result<GridPageResponse<ContentAuthorGridRowDto>>>
{
    public Task<Result<GridPageResponse<ContentAuthorGridRowDto>>> Handle(
        QueryAdminAuthorsGridQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(() => grid.QueryAsync(request.Request, cancellationToken), ContentErrorCodes.AuthorNotFound);
}
