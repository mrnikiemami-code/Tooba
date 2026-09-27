using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Attributes.Schema.Models;

namespace Tooba.Catalog.Application.Attributes.Schema.Commands;

/// <summary>DELETE /v1/admin/catalog/categories/{categoryId}/attribute-schema/bindings/{definitionId}</summary>
public sealed record UnbindCategoryAttributeCommand(Guid CategoryId, Guid DefinitionId)
    : IRequest<Result<CategoryAttributeSchemaMutationResult>>;
