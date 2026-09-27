using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductMedia.Models;

namespace Tooba.Catalog.Application.ProductMedia.Commands;

/// <summary>Detach (unassign) a media reference from a product.</summary>
public sealed record DetachProductMediaCommand(Guid ProductId, Guid MediaAssetId)
    : IRequest<Result<IReadOnlyList<ProductMediaItemView>>>;
