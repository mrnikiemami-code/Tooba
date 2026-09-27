using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.MarkArticleCommentPending;

public sealed record MarkArticleCommentPendingCommand(
    Guid ArticleId, Guid CommentId, Guid ActorUserId, string? Note)
    : IRequest<Result<ArticleCommentAdminDto>>;

public sealed class MarkArticleCommentPendingCommandHandler(IArticleCommentDirectory comments)
    : IRequestHandler<MarkArticleCommentPendingCommand, Result<ArticleCommentAdminDto>>
{
    public Task<Result<ArticleCommentAdminDto>> Handle(
        MarkArticleCommentPendingCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => comments.MarkPendingAsync(
                request.ArticleId, request.CommentId, request.ActorUserId,
                new ModerateArticleCommentCommand(request.Note), cancellationToken),
            ContentErrorCodes.CommentRejected);
}
