using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductMedia.Models;
using Tooba.Catalog.Application.ProductMedia.Ports;
using Tooba.Catalog.Application.ProductMedia.Queries;

namespace Tooba.Catalog.Application.ProductMedia.Commands;

/// <summary>Handles SetPrimaryProductMediaCommand.</summary>
public sealed class SetPrimaryProductMediaHandler
    : IRequestHandler<SetPrimaryProductMediaCommand, Result<IReadOnlyList<ProductMediaItemView>>>
{
    private readonly IProductMediaDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public SetPrimaryProductMediaHandler(IProductMediaDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProductMediaItemView>>> Handle(
        SetPrimaryProductMediaCommand request,
        CancellationToken cancellationToken)
    {
        var set = await _directory.SetPrimaryAsync(request.ProductId, request.MediaAssetId, cancellationToken);
        if (set.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ProductMediaItemView>>(set.Errors);
        }

        var listed = await _directory.ListAsync(request.ProductId, cancellationToken);
        if (listed.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ProductMediaItemView>>(listed.Errors);
        }

        return Result.Success(GetProductMediaHandler.Map(listed.Value));
    }
}
