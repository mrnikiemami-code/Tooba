using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.Definitions.Ports;

namespace Tooba.Catalog.Application.Attributes.Definitions.Queries;

/// <summary>Previews impact of disabling variant-axis capability.</summary>
public sealed class PreviewVariantAxisCapabilityDisableHandler
    : IRequestHandler<PreviewVariantAxisCapabilityDisableQuery, Result<VariantAxisCapabilityDisableImpactView>>
{
    private readonly IAttributeDefinitionDirectory _definitions;

    /// <summary>Creates the handler.</summary>
    public PreviewVariantAxisCapabilityDisableHandler(IAttributeDefinitionDirectory definitions) =>
        _definitions = definitions;

    /// <inheritdoc />
    public Task<Result<VariantAxisCapabilityDisableImpactView>> Handle(
        PreviewVariantAxisCapabilityDisableQuery request,
        CancellationToken cancellationToken) =>
        _definitions.PreviewVariantAxisCapabilityDisableAsync(request.DefinitionId, cancellationToken);
}
