using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain;

namespace Tooba.Content.Application.Queries.GetPublicCategoryBySlug;

public sealed record GetPublicCategoryBySlugQuery(string Slug, string? Locale)
    : IRequest<Result<PublishedContentCategoryItem>>;

public sealed class GetPublicCategoryBySlugQueryHandler(IContentCategoryDirectory categories)
    : IRequestHandler<GetPublicCategoryBySlugQuery, Result<PublishedContentCategoryItem>>
{
    public async Task<Result<PublishedContentCategoryItem>> Handle(
        GetPublicCategoryBySlugQuery request, CancellationToken cancellationToken)
    {
        var item = await categories.GetPublicBySlugAsync(
            ContentTaxonomySeoRules.ResolveContentLocale(request.Locale), request.Slug, cancellationToken);
        return ContentOperation.NotFoundIfNull(item, ContentErrorCodes.CategoryNotFound);
    }
}
