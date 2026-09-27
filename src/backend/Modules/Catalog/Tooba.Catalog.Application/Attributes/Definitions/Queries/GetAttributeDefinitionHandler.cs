using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.Definitions.Ports;

namespace Tooba.Catalog.Application.Attributes.Definitions.Queries;

/// <summary>Gets one Attribute Definition by id.</summary>
public sealed class GetAttributeDefinitionHandler
    : IRequestHandler<GetAttributeDefinitionQuery, Result<AttributeDefinitionView>>
{
    private readonly IAttributeDefinitionDirectory _definitions;

    /// <summary>Creates the handler.</summary>
    public GetAttributeDefinitionHandler(IAttributeDefinitionDirectory definitions) =>
        _definitions = definitions;

    /// <inheritdoc />
    public Task<Result<AttributeDefinitionView>> Handle(
        GetAttributeDefinitionQuery request,
        CancellationToken cancellationToken) =>
        _definitions.GetAsync(request.DefinitionId, cancellationToken);
}
