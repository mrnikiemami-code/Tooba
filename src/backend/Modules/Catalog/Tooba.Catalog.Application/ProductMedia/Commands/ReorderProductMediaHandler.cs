using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductMedia.Models;
using Tooba.Catalog.Application.ProductMedia.Ports;
using Tooba.Catalog.Application.ProductMedia.Queries;

namespace Tooba.Catalog.Application.ProductMedia.Commands;

/// <summary>Handles ReorderProductMediaCommand.</summary>
public sealed class ReorderProductMediaHandler
    : IRequestHandler<ReorderProductMediaCommand, Result<IReadOnlyList<ProductMediaItemView>>>
{
    private readonly IProductMediaDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public ReorderProductMediaHandler(IProductMediaDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProductMediaItemView>>> Handle(
        ReorderProductMediaCommand request,
        CancellationToken cancellationToken)
    {
        var reordered = await _directory.ReorderAsync(
            request.ProductId,
            request.Model.OrderedMediaAssetIds,
            cancellationToken);
        if (reordered.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ProductMediaItemView>>(reordered.Errors);
        }

        var listed = await _directory.ListAsync(request.ProductId, cancellationToken);
        if (listed.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ProductMediaItemView>>(listed.Errors);
        }

        return Result.Success(GetProductMediaHandler.Map(listed.Value));
    }
}
