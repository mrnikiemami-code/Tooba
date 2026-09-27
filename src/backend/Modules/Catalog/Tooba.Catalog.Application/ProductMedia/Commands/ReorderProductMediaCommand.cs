using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductMedia.Models;

namespace Tooba.Catalog.Application.ProductMedia.Commands;

/// <summary>Reorder product media to an exact existing set.</summary>
public sealed record ReorderProductMediaCommand(Guid ProductId, ReorderProductMediaWriteModel Model)
    : IRequest<Result<IReadOnlyList<ProductMediaItemView>>>;
