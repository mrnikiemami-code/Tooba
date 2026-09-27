using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Queries.ListArticleHistory;

public sealed record ListArticleHistoryQuery(Guid ArticleId, int Skip, int Take) : IRequest<Result<ArticleHistoryPage>>;

public sealed class ListArticleHistoryQueryHandler(IContentDirectory content)
    : IRequestHandler<ListArticleHistoryQuery, Result<ArticleHistoryPage>>
{
    public Task<Result<ArticleHistoryPage>> Handle(ListArticleHistoryQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(() => content.ListHistoryAsync(request.ArticleId, request.Skip, request.Take, cancellationToken), ContentErrorCodes.ArticleMissing);
}
