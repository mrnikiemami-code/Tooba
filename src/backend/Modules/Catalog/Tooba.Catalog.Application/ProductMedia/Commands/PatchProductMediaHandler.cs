using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductMedia.Models;
using Tooba.Catalog.Application.ProductMedia.Ports;
using Tooba.Catalog.Application.ProductMedia.Queries;

namespace Tooba.Catalog.Application.ProductMedia.Commands;

/// <summary>Handles PatchProductMediaCommand.</summary>
public sealed class PatchProductMediaHandler
    : IRequestHandler<PatchProductMediaCommand, Result<IReadOnlyList<ProductMediaItemView>>>
{
    private readonly IProductMediaDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public PatchProductMediaHandler(IProductMediaDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProductMediaItemView>>> Handle(
        PatchProductMediaCommand request,
        CancellationToken cancellationToken)
    {
        var patched = await _directory.PatchAltAsync(
            request.ProductId,
            request.MediaAssetId,
            request.Model.AltText,
            cancellationToken);
        if (patched.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ProductMediaItemView>>(patched.Errors);
        }

        var listed = await _directory.ListAsync(request.ProductId, cancellationToken);
        if (listed.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ProductMediaItemView>>(listed.Errors);
        }

        return Result.Success(GetProductMediaHandler.Map(listed.Value));
    }
}
