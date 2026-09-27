using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Commands.DeleteArticle;

public sealed record DeleteArticleCommand(Guid ArticleId) : IRequest<Result>;

public sealed class DeleteArticleCommandHandler(IContentDirectory content)
    : IRequestHandler<DeleteArticleCommand, Result>
{
    public Task<Result> Handle(DeleteArticleCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(() => content.DeleteDraftAsync(request.ArticleId, cancellationToken), ContentErrorCodes.DeleteRejected);
}
