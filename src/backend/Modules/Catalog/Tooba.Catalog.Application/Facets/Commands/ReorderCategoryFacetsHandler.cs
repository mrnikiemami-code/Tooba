using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Facets.Ports;

namespace Tooba.Catalog.Application.Facets.Commands;

/// <summary>Reorders local facet configurations for a category.</summary>
public sealed class ReorderCategoryFacetsHandler : IRequestHandler<ReorderCategoryFacetsCommand, Result>
{
    private readonly IFacetDirectory _facets;

    /// <summary>Creates the handler.</summary>
    public ReorderCategoryFacetsHandler(IFacetDirectory facets) => _facets = facets;

    /// <inheritdoc />
    public Task<Result> Handle(ReorderCategoryFacetsCommand request, CancellationToken cancellationToken) =>
        _facets.ReorderConfigurationsAsync(
            request.CategoryId,
            request.OrderedDefinitionIds ?? Array.Empty<Guid>(),
            cancellationToken);
}
