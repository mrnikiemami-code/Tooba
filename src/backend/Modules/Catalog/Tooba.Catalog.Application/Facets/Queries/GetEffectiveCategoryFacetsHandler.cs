using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Facets.Ports;

namespace Tooba.Catalog.Application.Facets.Queries;

/// <summary>Loads effective category facets for Admin.</summary>
public sealed class GetEffectiveCategoryFacetsHandler
    : IRequestHandler<GetEffectiveCategoryFacetsQuery, Result<IReadOnlyList<EffectiveCategoryFacet>>>
{
    private readonly IFacetDirectory _facets;

    /// <summary>Creates the handler.</summary>
    public GetEffectiveCategoryFacetsHandler(IFacetDirectory facets) => _facets = facets;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<EffectiveCategoryFacet>>> Handle(
        GetEffectiveCategoryFacetsQuery request,
        CancellationToken cancellationToken)
    {
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "fa-IR" : request.Locale;
        return _facets.GetEffectiveFacetsAsync(request.CategoryId, locale, cancellationToken);
    }
}
