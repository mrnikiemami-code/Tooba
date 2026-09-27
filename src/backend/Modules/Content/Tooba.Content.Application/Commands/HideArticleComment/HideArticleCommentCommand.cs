using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.HideArticleComment;

public sealed record HideArticleCommentCommand(
    Guid ArticleId, Guid CommentId, Guid ActorUserId, string? Note)
    : IRequest<Result<ArticleCommentAdminDto>>;

public sealed class HideArticleCommentCommandHandler(IArticleCommentDirectory comments)
    : IRequestHandler<HideArticleCommentCommand, Result<ArticleCommentAdminDto>>
{
    public Task<Result<ArticleCommentAdminDto>> Handle(
        HideArticleCommentCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => comments.HideAsync(
                request.ArticleId, request.CommentId, request.ActorUserId,
                new ModerateArticleCommentCommand(request.Note), cancellationToken),
            ContentErrorCodes.CommentRejected);
}
