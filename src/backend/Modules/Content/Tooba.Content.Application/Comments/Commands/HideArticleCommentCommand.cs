using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Comments.Models;
using Tooba.Content.Application.Comments.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Comments.Commands;

public sealed record HideArticleCommentCommand(
    Guid ArticleId, Guid CommentId, Guid ActorUserId, string? Note)
    : IRequest<Result<ArticleCommentAdminDto>>;

public sealed class HideArticleCommentCommandHandler(IArticleCommentDirectory comments)
    : IRequestHandler<HideArticleCommentCommand, Result<ArticleCommentAdminDto>>
{
    public Task<Result<ArticleCommentAdminDto>> Handle(
        HideArticleCommentCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => comments.HideAsync(request, cancellationToken),
            ContentErrorCodes.CommentRejected);
}
