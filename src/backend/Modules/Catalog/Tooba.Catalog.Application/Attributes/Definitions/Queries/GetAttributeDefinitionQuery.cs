using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Attributes.Definitions.Queries;

/// <summary>Gets one Attribute Definition by id.</summary>
public sealed record GetAttributeDefinitionQuery(Guid DefinitionId)
    : IRequest<Result<AttributeDefinitionView>>;
