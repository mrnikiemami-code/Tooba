using MediatR;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Catalog.Application.Facets.Commands;

/// <summary>DELETE /v1/admin/catalog/categories/{categoryId}/facets/{definitionId}</summary>
public sealed record RemoveCategoryFacetOverrideCommand(Guid CategoryId, Guid DefinitionId)
    : IRequest<Result>;
