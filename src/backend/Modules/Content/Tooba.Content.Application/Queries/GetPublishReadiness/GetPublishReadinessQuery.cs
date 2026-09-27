using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain;
using Tooba.BuildingBlocks.Grid;

namespace Tooba.Content.Application.Queries.GetPublishReadiness;

public sealed record GetPublishReadinessQuery(Guid ArticleId) : IRequest<Result<ArticlePublicationReadiness>>;

public sealed class GetPublishReadinessQueryHandler(IContentDirectory content)
    : IRequestHandler<GetPublishReadinessQuery, Result<ArticlePublicationReadiness>>
{
    public Task<Result<ArticlePublicationReadiness>> Handle(GetPublishReadinessQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(() => content.GetPublishReadinessAsync(request.ArticleId, cancellationToken), ContentErrorCodes.ArticleMissing);
}
