using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Queries.GetPublishedArticleBySlug;

public sealed record GetPublishedArticleBySlugQuery(string Slug, string? Locale)
    : IRequest<Result<PublishedArticleItem>>;

public sealed class GetPublishedArticleBySlugQueryHandler(IContentDirectory content)
    : IRequestHandler<GetPublishedArticleBySlugQuery, Result<PublishedArticleItem>>
{
    public async Task<Result<PublishedArticleItem>> Handle(
        GetPublishedArticleBySlugQuery request, CancellationToken cancellationToken)
    {
        var item = await content.GetPublishedBySlugAsync(request.Slug, request.Locale, cancellationToken);
        return ContentOperation.NotFoundIfNull(item, ContentErrorCodes.ArticleMissing);
    }
}
