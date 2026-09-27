using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain;
using Tooba.BuildingBlocks.Grid;

namespace Tooba.Content.Application.Queries.ListAdminArticles;

public sealed record ListAdminArticlesQuery(int Page, int PageSize) : IRequest<Result<PagedResult<AdminArticleSnapshot>>>;

public sealed class ListAdminArticlesQueryHandler(IContentDirectory content)
    : IRequestHandler<ListAdminArticlesQuery, Result<PagedResult<AdminArticleSnapshot>>>
{
    public Task<Result<PagedResult<AdminArticleSnapshot>>> Handle(ListAdminArticlesQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(() => content.ListAllAsync(request.Page, request.PageSize, cancellationToken), ContentErrorCodes.ArticleMissing);
}
