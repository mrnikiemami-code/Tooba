using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Facets.Ports;

namespace Tooba.Catalog.Application.Facets.Commands;

/// <summary>Removes a local facet override.</summary>
public sealed class RemoveCategoryFacetOverrideHandler
    : IRequestHandler<RemoveCategoryFacetOverrideCommand, Result>
{
    private readonly IFacetDirectory _facets;

    /// <summary>Creates the handler.</summary>
    public RemoveCategoryFacetOverrideHandler(IFacetDirectory facets) => _facets = facets;

    /// <inheritdoc />
    public Task<Result> Handle(RemoveCategoryFacetOverrideCommand request, CancellationToken cancellationToken) =>
        _facets.RemoveOverrideAsync(request.CategoryId, request.DefinitionId, cancellationToken);
}
