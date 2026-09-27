using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Attributes.Definitions.Queries;

/// <summary>Lists Attribute Definitions for Admin.</summary>
public sealed record ListAttributeDefinitionsQuery()
    : IRequest<Result<IReadOnlyList<AttributeDefinitionView>>>;
