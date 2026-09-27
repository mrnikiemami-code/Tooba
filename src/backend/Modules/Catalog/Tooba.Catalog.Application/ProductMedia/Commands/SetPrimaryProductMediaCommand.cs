using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductMedia.Models;

namespace Tooba.Catalog.Application.ProductMedia.Commands;

/// <summary>Set the primary media asset for a product.</summary>
public sealed record SetPrimaryProductMediaCommand(Guid ProductId, Guid MediaAssetId)
    : IRequest<Result<IReadOnlyList<ProductMediaItemView>>>;
