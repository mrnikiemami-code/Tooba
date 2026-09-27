using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.Definitions.Models;

namespace Tooba.Catalog.Application.Attributes.Definitions.Commands;

/// <summary>Updates Attribute Definition metadata.</summary>
public sealed record UpdateAttributeDefinitionCommand(
    Guid DefinitionId,
    AttributeDefinitionMetadataWriteModel Metadata)
    : IRequest<Result<AttributeDefinitionView>>;
