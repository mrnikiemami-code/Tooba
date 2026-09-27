using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Facets.Ports;

namespace Tooba.Catalog.Application.Facets.Queries;

/// <summary>Loads effective category facets for Storefront (same semantics as Admin effective).</summary>
public sealed class GetStorefrontCategoryFacetsHandler
    : IRequestHandler<GetStorefrontCategoryFacetsQuery, Result<IReadOnlyList<EffectiveCategoryFacet>>>
{
    private readonly IFacetDirectory _facets;

    /// <summary>Creates the handler.</summary>
    public GetStorefrontCategoryFacetsHandler(IFacetDirectory facets) => _facets = facets;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<EffectiveCategoryFacet>>> Handle(
        GetStorefrontCategoryFacetsQuery request,
        CancellationToken cancellationToken)
    {
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "fa-IR" : request.Locale;
        return _facets.GetEffectiveFacetsAsync(request.CategoryId, locale, cancellationToken);
    }
}
