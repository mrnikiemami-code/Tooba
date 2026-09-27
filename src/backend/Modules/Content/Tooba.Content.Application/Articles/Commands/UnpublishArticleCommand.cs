using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Articles.Models;
using Tooba.Content.Application.Articles.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Articles.Commands;

public sealed record UnpublishArticleCommand(Guid ArticleId) : IRequest<Result<AdminArticleSnapshot>>;

public sealed class UnpublishArticleCommandHandler(IContentDirectory content)
    : IRequestHandler<UnpublishArticleCommand, Result<AdminArticleSnapshot>>
{
    public Task<Result<AdminArticleSnapshot>> Handle(UnpublishArticleCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(() => content.UnpublishAsync(request.ArticleId, cancellationToken), ContentErrorCodes.UpdateRejected);
}
