using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.Definitions.Ports;

namespace Tooba.Catalog.Application.Attributes.Definitions.Commands;

/// <summary>Updates Attribute Definition metadata.</summary>
public sealed class UpdateAttributeDefinitionHandler
    : IRequestHandler<UpdateAttributeDefinitionCommand, Result<AttributeDefinitionView>>
{
    private readonly IAttributeDefinitionDirectory _definitions;

    /// <summary>Creates the handler.</summary>
    public UpdateAttributeDefinitionHandler(IAttributeDefinitionDirectory definitions) =>
        _definitions = definitions;

    /// <inheritdoc />
    public Task<Result<AttributeDefinitionView>> Handle(
        UpdateAttributeDefinitionCommand request,
        CancellationToken cancellationToken) =>
        _definitions.UpdateMetadataAsync(request.DefinitionId, request.Metadata, cancellationToken);
}
