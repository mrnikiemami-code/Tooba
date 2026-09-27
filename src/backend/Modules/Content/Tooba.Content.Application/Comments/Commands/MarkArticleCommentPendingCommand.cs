using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Comments.Models;
using Tooba.Content.Application.Comments.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Comments.Commands;

public sealed record MarkArticleCommentPendingCommand(
    Guid ArticleId, Guid CommentId, Guid ActorUserId, string? Note)
    : IRequest<Result<ArticleCommentAdminDto>>;

public sealed class MarkArticleCommentPendingCommandHandler(IArticleCommentDirectory comments)
    : IRequestHandler<MarkArticleCommentPendingCommand, Result<ArticleCommentAdminDto>>
{
    public Task<Result<ArticleCommentAdminDto>> Handle(
        MarkArticleCommentPendingCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => comments.MarkPendingAsync(request, cancellationToken),
            ContentErrorCodes.CommentRejected);
}
