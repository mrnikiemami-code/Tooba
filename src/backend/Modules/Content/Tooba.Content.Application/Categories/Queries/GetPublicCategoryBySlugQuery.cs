using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Categories.Models;
using Tooba.Content.Application.Categories.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Categories.Queries;

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
