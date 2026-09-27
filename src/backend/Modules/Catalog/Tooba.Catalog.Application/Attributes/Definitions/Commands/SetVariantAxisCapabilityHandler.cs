using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.Definitions.Ports;

namespace Tooba.Catalog.Application.Attributes.Definitions.Commands;

/// <summary>Sets variant-axis capability on an Attribute Definition.</summary>
public sealed class SetVariantAxisCapabilityHandler
    : IRequestHandler<SetVariantAxisCapabilityCommand, Result<AttributeDefinitionView>>
{
    private readonly IAttributeDefinitionDirectory _definitions;

    /// <summary>Creates the handler.</summary>
    public SetVariantAxisCapabilityHandler(IAttributeDefinitionDirectory definitions) =>
        _definitions = definitions;

    /// <inheritdoc />
    public Task<Result<AttributeDefinitionView>> Handle(
        SetVariantAxisCapabilityCommand request,
        CancellationToken cancellationToken) =>
        _definitions.SetVariantAxisCapabilityAsync(
            request.DefinitionId,
            request.IsVariantAxisAllowed,
            cancellationToken);
}
