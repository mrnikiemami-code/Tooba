using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Attributes.Schema.Models;

namespace Tooba.Catalog.Application.Attributes.Schema.Commands;

/// <summary>PATCH /v1/admin/catalog/categories/{categoryId}/attribute-schema/bindings/{definitionId}</summary>
public sealed record UpdateCategoryAttributeBindingCommand(
    Guid CategoryId,
    Guid DefinitionId,
    UpdateCategoryAttributeBindingWriteModel Model)
    : IRequest<Result<CategoryAttributeSchemaMutationResult>>;
