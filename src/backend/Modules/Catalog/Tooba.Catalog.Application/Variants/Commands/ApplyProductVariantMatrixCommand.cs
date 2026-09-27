using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Variants.Models;

namespace Tooba.Catalog.Application.Variants.Commands;

/// <summary>Applies the product variant matrix.</summary>
public sealed record ApplyProductVariantMatrixCommand(Guid ProductId, ProductVariantApplyWriteModel Model)
    : IRequest<Result<ProductVariantApplyResult>>;
