using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Queries.QueryAdminArticlesGrid;

public sealed record QueryAdminArticlesGridQuery(GridQueryRequest Request) : IRequest<Result<GridPageResponse<AdminArticleSnapshot>>>;

public sealed class QueryAdminArticlesGridQueryHandler(IContentArticleGridPort grid)
    : IRequestHandler<QueryAdminArticlesGridQuery, Result<GridPageResponse<AdminArticleSnapshot>>>
{
    public Task<Result<GridPageResponse<AdminArticleSnapshot>>> Handle(QueryAdminArticlesGridQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(() => grid.QueryAsync(request.Request, cancellationToken), ContentErrorCodes.ArticleMissing);
}
