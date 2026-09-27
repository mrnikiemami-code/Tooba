using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Commands.PublishArticle;

public sealed record PublishArticleCommand(Guid ArticleId) : IRequest<Result<AdminArticleSnapshot>>;

public sealed class PublishArticleCommandHandler(IContentDirectory content)
    : IRequestHandler<PublishArticleCommand, Result<AdminArticleSnapshot>>
{
    public Task<Result<AdminArticleSnapshot>> Handle(PublishArticleCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(() => content.PublishAsync(request.ArticleId, cancellationToken), ContentErrorCodes.UpdateRejected);
}
