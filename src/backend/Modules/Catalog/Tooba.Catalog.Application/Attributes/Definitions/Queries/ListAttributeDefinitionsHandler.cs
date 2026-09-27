using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.Definitions.Ports;

namespace Tooba.Catalog.Application.Attributes.Definitions.Queries;

/// <summary>Lists Attribute Definitions for Admin.</summary>
public sealed class ListAttributeDefinitionsHandler
    : IRequestHandler<ListAttributeDefinitionsQuery, Result<IReadOnlyList<AttributeDefinitionView>>>
{
    private readonly IAttributeDefinitionDirectory _definitions;

    /// <summary>Creates the handler.</summary>
    public ListAttributeDefinitionsHandler(IAttributeDefinitionDirectory definitions) =>
        _definitions = definitions;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AttributeDefinitionView>>> Handle(
        ListAttributeDefinitionsQuery request,
        CancellationToken cancellationToken) =>
        _definitions.ListAsync(cancellationToken);
}
