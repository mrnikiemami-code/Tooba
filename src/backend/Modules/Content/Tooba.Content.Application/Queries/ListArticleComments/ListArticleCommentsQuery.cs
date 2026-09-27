using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain;

namespace Tooba.Content.Application.Queries.ListArticleComments;

public sealed record ListArticleCommentsQuery(
    Guid ArticleId, ArticleCommentStatus? Status, string? Search, int Skip, int Take)
    : IRequest<Result<ArticleCommentPage>>;

public sealed class ListArticleCommentsQueryHandler(IArticleCommentDirectory comments)
    : IRequestHandler<ListArticleCommentsQuery, Result<ArticleCommentPage>>
{
    public Task<Result<ArticleCommentPage>> Handle(
        ListArticleCommentsQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => comments.ListForArticleAsync(
                request.ArticleId, request.Status, request.Search, request.Skip, request.Take, cancellationToken),
            ContentErrorCodes.CommentRejected);
}
