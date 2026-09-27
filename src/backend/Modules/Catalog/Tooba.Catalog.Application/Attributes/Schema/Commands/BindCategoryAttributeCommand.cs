using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Attributes.Schema.Models;

namespace Tooba.Catalog.Application.Attributes.Schema.Commands;

/// <summary>POST /v1/admin/catalog/categories/{categoryId}/attribute-schema/bindings</summary>
public sealed record BindCategoryAttributeCommand(
    Guid CategoryId,
    BindCategoryAttributeWriteModel Model)
    : IRequest<Result<CategoryAttributeSchemaMutationResult>>;
