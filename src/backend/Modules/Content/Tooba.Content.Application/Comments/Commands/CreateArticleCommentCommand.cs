using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Comments.Models;
using Tooba.Content.Application.Comments.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Comments.Commands;

public sealed record CreateArticleCommentCommand(
    Guid ArticleId, string DisplayName, string Body, Guid? AuthorPartyId)
    : IRequest<Result<ArticleCommentAdminDto>>;

public sealed class CreateArticleCommentCommandHandler(IArticleCommentDirectory comments)
    : IRequestHandler<CreateArticleCommentCommand, Result<ArticleCommentAdminDto>>
{
    public Task<Result<ArticleCommentAdminDto>> Handle(
        CreateArticleCommentCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => comments.CreateAsync(request, cancellationToken),
            ContentErrorCodes.CommentRejected);
}
