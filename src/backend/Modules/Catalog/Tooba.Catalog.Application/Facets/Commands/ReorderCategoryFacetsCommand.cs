using MediatR;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Catalog.Application.Facets.Commands;

/// <summary>PUT /v1/admin/catalog/categories/{categoryId}/facets/order</summary>
public sealed record ReorderCategoryFacetsCommand(
    Guid CategoryId,
    IReadOnlyList<Guid>? OrderedDefinitionIds) : IRequest<Result>;
