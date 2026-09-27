using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Queries.GetAdminArticle;

public sealed record GetAdminArticleQuery(Guid ArticleId) : IRequest<Result<AdminArticleSnapshot>>;

public sealed class GetAdminArticleQueryHandler(IContentDirectory content)
    : IRequestHandler<GetAdminArticleQuery, Result<AdminArticleSnapshot>>
{
    public async Task<Result<AdminArticleSnapshot>> Handle(GetAdminArticleQuery request, CancellationToken cancellationToken)
    {
        var item = await content.GetByIdAsync(request.ArticleId, cancellationToken);
        return ContentOperation.NotFoundIfNull(item, ContentErrorCodes.ArticleMissing);
    }
}
