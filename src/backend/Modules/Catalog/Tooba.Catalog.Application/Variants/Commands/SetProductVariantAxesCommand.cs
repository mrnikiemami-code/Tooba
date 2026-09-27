using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Variants.Models;

namespace Tooba.Catalog.Application.Variants.Commands;

/// <summary>Replaces product variant axes.</summary>
public sealed record SetProductVariantAxesCommand(Guid ProductId, SetProductVariantAxesWriteModel Model)
    : IRequest<Result<ProductVariantAxesMutationOk>>;
