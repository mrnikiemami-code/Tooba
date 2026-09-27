using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Queries.ListAdminArticles;

public sealed record ListAdminArticlesQuery(int Page, int PageSize) : IRequest<Result<PagedResult<AdminArticleSnapshot>>>;

public sealed class ListAdminArticlesQueryHandler(IContentDirectory content)
    : IRequestHandler<ListAdminArticlesQuery, Result<PagedResult<AdminArticleSnapshot>>>
{
    public Task<Result<PagedResult<AdminArticleSnapshot>>> Handle(ListAdminArticlesQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(() => content.ListAllAsync(request.Page, request.PageSize, cancellationToken), ContentErrorCodes.ArticleMissing);
}
