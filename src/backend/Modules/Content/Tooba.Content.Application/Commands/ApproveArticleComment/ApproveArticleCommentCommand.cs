using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.ApproveArticleComment;

public sealed record ApproveArticleCommentCommand(
    Guid ArticleId, Guid CommentId, Guid ActorUserId, string? Note)
    : IRequest<Result<ArticleCommentAdminDto>>;

public sealed class ApproveArticleCommentCommandHandler(IArticleCommentDirectory comments)
    : IRequestHandler<ApproveArticleCommentCommand, Result<ArticleCommentAdminDto>>
{
    public Task<Result<ArticleCommentAdminDto>> Handle(
        ApproveArticleCommentCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => comments.ApproveAsync(
                request.ArticleId, request.CommentId, request.ActorUserId,
                new ModerateArticleCommentCommand(request.Note), cancellationToken),
            ContentErrorCodes.CommentRejected);
}
