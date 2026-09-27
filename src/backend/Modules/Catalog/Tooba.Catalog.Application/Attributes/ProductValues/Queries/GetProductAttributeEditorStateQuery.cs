using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Attributes.ProductValues.Queries;

/// <summary>Reads product attribute editor state.</summary>
public sealed record GetProductAttributeEditorStateQuery(Guid ProductId, string? Locale)
    : IRequest<Result<ProductAttributeEditorState>>;
