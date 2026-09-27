using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Attributes.Definitions.Models;

namespace Tooba.Catalog.Application.Attributes.Definitions.Commands;

/// <summary>Creates an Attribute Definition (optional metadata applied atomically).</summary>
public sealed record CreateAttributeDefinitionCommand(CreateAttributeDefinitionWriteModel Model)
    : IRequest<Result<AttributeDefinitionCreatedResult>>;
