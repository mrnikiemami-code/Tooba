using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Attributes.Definitions.Commands;

/// <summary>Sets variant-axis capability on an Attribute Definition.</summary>
public sealed record SetVariantAxisCapabilityCommand(Guid DefinitionId, bool IsVariantAxisAllowed)
    : IRequest<Result<AttributeDefinitionView>>;
