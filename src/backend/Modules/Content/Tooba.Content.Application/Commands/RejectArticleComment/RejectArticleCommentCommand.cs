using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.RejectArticleComment;

public sealed record RejectArticleCommentCommand(
    Guid ArticleId, Guid CommentId, Guid ActorUserId, string? Note)
    : IRequest<Result<ArticleCommentAdminDto>>;

public sealed class RejectArticleCommentCommandHandler(IArticleCommentDirectory comments)
    : IRequestHandler<RejectArticleCommentCommand, Result<ArticleCommentAdminDto>>
{
    public Task<Result<ArticleCommentAdminDto>> Handle(
        RejectArticleCommentCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => comments.RejectAsync(
                request.ArticleId, request.CommentId, request.ActorUserId,
                new ModerateArticleCommentCommand(request.Note), cancellationToken),
            ContentErrorCodes.CommentRejected);
}
