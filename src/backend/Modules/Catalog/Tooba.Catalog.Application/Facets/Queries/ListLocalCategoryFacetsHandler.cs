using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Facets.Ports;

namespace Tooba.Catalog.Application.Facets.Queries;

/// <summary>Lists local facet configurations for a category.</summary>
public sealed class ListLocalCategoryFacetsHandler
    : IRequestHandler<ListLocalCategoryFacetsQuery, Result<IReadOnlyList<CategoryFacetConfigurationView>>>
{
    private readonly IFacetDirectory _facets;

    /// <summary>Creates the handler.</summary>
    public ListLocalCategoryFacetsHandler(IFacetDirectory facets) => _facets = facets;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<CategoryFacetConfigurationView>>> Handle(
        ListLocalCategoryFacetsQuery request,
        CancellationToken cancellationToken) =>
        _facets.ListLocalConfigurationsAsync(request.CategoryId, cancellationToken);
}
