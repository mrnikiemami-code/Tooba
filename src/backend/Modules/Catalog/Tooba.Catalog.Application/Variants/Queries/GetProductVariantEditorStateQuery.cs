using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Variants.Queries;

/// <summary>Loads product variant editor state.</summary>
public sealed record GetProductVariantEditorStateQuery(Guid ProductId, string? Locale)
    : IRequest<Result<ProductVariantEditorState>>;
