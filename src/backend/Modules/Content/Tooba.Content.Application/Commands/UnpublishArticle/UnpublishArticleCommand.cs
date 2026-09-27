using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain;
using Tooba.BuildingBlocks.Grid;

namespace Tooba.Content.Application.Commands.UnpublishArticle;

public sealed record UnpublishArticleCommand(Guid ArticleId) : IRequest<Result<AdminArticleSnapshot>>;

public sealed class UnpublishArticleCommandHandler(IContentDirectory content)
    : IRequestHandler<UnpublishArticleCommand, Result<AdminArticleSnapshot>>
{
    public Task<Result<AdminArticleSnapshot>> Handle(UnpublishArticleCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(() => content.UnpublishAsync(request.ArticleId, cancellationToken), ContentErrorCodes.UpdateRejected);
}
