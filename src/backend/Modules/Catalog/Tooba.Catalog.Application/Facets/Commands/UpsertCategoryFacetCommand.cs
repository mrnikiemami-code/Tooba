using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Facets.Commands;

/// <summary>PUT /v1/admin/catalog/categories/{categoryId}/facets/{definitionId}</summary>
public sealed record UpsertCategoryFacetCommand(
    Guid CategoryId,
    Guid DefinitionId,
    CategoryFacetConfigurationInput Input) : IRequest<Result>;
