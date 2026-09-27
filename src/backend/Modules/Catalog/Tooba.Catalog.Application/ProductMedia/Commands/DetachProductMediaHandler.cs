using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductMedia.Models;
using Tooba.Catalog.Application.ProductMedia.Ports;
using Tooba.Catalog.Application.ProductMedia.Queries;

namespace Tooba.Catalog.Application.ProductMedia.Commands;

/// <summary>Handles DetachProductMediaCommand.</summary>
public sealed class DetachProductMediaHandler
    : IRequestHandler<DetachProductMediaCommand, Result<IReadOnlyList<ProductMediaItemView>>>
{
    private readonly IProductMediaDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public DetachProductMediaHandler(IProductMediaDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProductMediaItemView>>> Handle(
        DetachProductMediaCommand request,
        CancellationToken cancellationToken)
    {
        var detached = await _directory.DetachAsync(request.ProductId, request.MediaAssetId, cancellationToken);
        if (detached.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ProductMediaItemView>>(detached.Errors);
        }

        var listed = await _directory.ListAsync(request.ProductId, cancellationToken);
        if (listed.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ProductMediaItemView>>(listed.Errors);
        }

        return Result.Success(GetProductMediaHandler.Map(listed.Value));
    }
}
