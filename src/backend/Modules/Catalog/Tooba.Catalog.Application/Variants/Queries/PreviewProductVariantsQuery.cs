using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Variants.Models;

namespace Tooba.Catalog.Application.Variants.Queries;

/// <summary>Previews product variant combinations.</summary>
public sealed record PreviewProductVariantsQuery(Guid ProductId, ProductVariantPreviewWriteModel Model)
    : IRequest<Result<ProductVariantPreviewResult>>;
