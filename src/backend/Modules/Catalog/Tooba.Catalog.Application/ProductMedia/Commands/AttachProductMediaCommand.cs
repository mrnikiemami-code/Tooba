using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductMedia.Models;

namespace Tooba.Catalog.Application.ProductMedia.Commands;

/// <summary>Attach an existing media asset reference to a product.</summary>
public sealed record AttachProductMediaCommand(Guid ProductId, AttachProductMediaWriteModel Model)
    : IRequest<Result<IReadOnlyList<ProductMediaItemView>>>;
