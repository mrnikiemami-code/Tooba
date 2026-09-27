using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Queries.GetPublishReadiness;

public sealed record GetPublishReadinessQuery(Guid ArticleId) : IRequest<Result<ArticlePublicationReadiness>>;

public sealed class GetPublishReadinessQueryHandler(IContentDirectory content)
    : IRequestHandler<GetPublishReadinessQuery, Result<ArticlePublicationReadiness>>
{
    public Task<Result<ArticlePublicationReadiness>> Handle(GetPublishReadinessQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(() => content.GetPublishReadinessAsync(request.ArticleId, cancellationToken), ContentErrorCodes.ArticleMissing);
}
