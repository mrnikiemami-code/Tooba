using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Attributes.Schema.Models;

namespace Tooba.Catalog.Application.Attributes.Schema.Commands;

/// <summary>PUT /v1/admin/catalog/categories/{categoryId}/attribute-schema/bindings/order</summary>
public sealed record ReorderCategoryAttributeBindingsCommand(
    Guid CategoryId,
    IReadOnlyList<Guid>? OrderedDefinitionIds)
    : IRequest<Result<CategoryAttributeSchemaMutationResult>>;
