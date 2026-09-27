using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Commands.CreateArticleComment;

public sealed record CreateArticleCommentCommand(
    Guid ArticleId, string DisplayName, string Body, Guid? AuthorPartyId)
    : IRequest<Result<ArticleCommentAdminDto>>;

public sealed class CreateArticleCommentCommandHandler(IArticleCommentDirectory comments)
    : IRequestHandler<CreateArticleCommentCommand, Result<ArticleCommentAdminDto>>
{
    public Task<Result<ArticleCommentAdminDto>> Handle(
        CreateArticleCommentCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => comments.CreateAsync(
                request.ArticleId,
                new Tooba.Content.Application.Models.CreateArticleCommentCommand(request.DisplayName, request.Body, request.AuthorPartyId),
                cancellationToken),
            ContentErrorCodes.CommentRejected);
}
