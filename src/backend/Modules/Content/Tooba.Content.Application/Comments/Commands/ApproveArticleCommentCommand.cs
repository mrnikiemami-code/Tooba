using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Comments.Models;
using Tooba.Content.Application.Comments.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Comments.Commands;

public sealed record ApproveArticleCommentCommand(
    Guid ArticleId, Guid CommentId, Guid ActorUserId, string? Note)
    : IRequest<Result<ArticleCommentAdminDto>>;

public sealed class ApproveArticleCommentCommandHandler(IArticleCommentDirectory comments)
    : IRequestHandler<ApproveArticleCommentCommand, Result<ArticleCommentAdminDto>>
{
    public Task<Result<ArticleCommentAdminDto>> Handle(
        ApproveArticleCommentCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => comments.ApproveAsync(request, cancellationToken),
            ContentErrorCodes.CommentRejected);
}
