using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductMedia.Models;

namespace Tooba.Catalog.Application.ProductMedia.Commands;

/// <summary>Attach a generated placeholder media reference.</summary>
public sealed record AttachPlaceholderProductMediaCommand(
    Guid ProductId,
    AttachPlaceholderProductMediaWriteModel? Model)
    : IRequest<Result<IReadOnlyList<ProductMediaItemView>>>;
