using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductMedia.Models;

namespace Tooba.Catalog.Application.ProductMedia.Commands;

/// <summary>Patch alt text on a product media reference.</summary>
public sealed record PatchProductMediaCommand(
    Guid ProductId,
    Guid MediaAssetId,
    PatchProductMediaWriteModel Model)
    : IRequest<Result<IReadOnlyList<ProductMediaItemView>>>;
