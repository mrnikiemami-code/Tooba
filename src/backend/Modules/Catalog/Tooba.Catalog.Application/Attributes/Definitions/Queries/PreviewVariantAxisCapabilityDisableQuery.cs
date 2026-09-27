using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Attributes.Definitions.Queries;

/// <summary>Previews impact of disabling variant-axis capability.</summary>
public sealed record PreviewVariantAxisCapabilityDisableQuery(Guid DefinitionId)
    : IRequest<Result<VariantAxisCapabilityDisableImpactView>>;
