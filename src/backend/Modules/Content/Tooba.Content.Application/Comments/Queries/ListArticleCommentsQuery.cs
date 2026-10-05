using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Comments.Models;
using Tooba.Content.Application.Comments.Ports;
using Tooba.Content.Contracts.Enums;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Comments.Queries;

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
