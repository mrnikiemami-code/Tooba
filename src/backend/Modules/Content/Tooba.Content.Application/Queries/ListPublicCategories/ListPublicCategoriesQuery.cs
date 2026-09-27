using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Queries.ListPublicCategories;

public sealed record ListPublicCategoriesQuery(string? Locale)
    : IRequest<Result<IReadOnlyList<PublishedContentCategoryItem>>>;

public sealed class ListPublicCategoriesQueryHandler(IContentCategoryDirectory categories)
    : IRequestHandler<ListPublicCategoriesQuery, Result<IReadOnlyList<PublishedContentCategoryItem>>>
{
    public Task<Result<IReadOnlyList<PublishedContentCategoryItem>>> Handle(
        ListPublicCategoriesQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => categories.ListPublicAsync(ContentTaxonomySeoRules.ResolveContentLocale(request.Locale), cancellationToken),
            ContentErrorCodes.CategoryNotFound);
}
