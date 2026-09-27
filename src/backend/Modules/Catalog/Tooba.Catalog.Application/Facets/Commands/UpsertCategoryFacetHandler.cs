using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Facets.Ports;

namespace Tooba.Catalog.Application.Facets.Commands;

/// <summary>Upserts a local category facet configuration.</summary>
public sealed class UpsertCategoryFacetHandler : IRequestHandler<UpsertCategoryFacetCommand, Result>
{
    private readonly IFacetDirectory _facets;

    /// <summary>Creates the handler.</summary>
    public UpsertCategoryFacetHandler(IFacetDirectory facets) => _facets = facets;

    /// <inheritdoc />
    public Task<Result> Handle(UpsertCategoryFacetCommand request, CancellationToken cancellationToken) =>
        _facets.UpsertConfigurationAsync(
            request.CategoryId,
            request.DefinitionId,
            request.Input,
            cancellationToken);
}
