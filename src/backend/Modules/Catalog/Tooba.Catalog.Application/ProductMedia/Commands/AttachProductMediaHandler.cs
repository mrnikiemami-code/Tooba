using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductMedia.Models;
using Tooba.Catalog.Application.ProductMedia.Ports;
using Tooba.Catalog.Application.ProductMedia.Queries;

namespace Tooba.Catalog.Application.ProductMedia.Commands;

/// <summary>Handles AttachProductMediaCommand.</summary>
public sealed class AttachProductMediaHandler
    : IRequestHandler<AttachProductMediaCommand, Result<IReadOnlyList<ProductMediaItemView>>>
{
    private readonly IProductMediaDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public AttachProductMediaHandler(IProductMediaDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProductMediaItemView>>> Handle(
        AttachProductMediaCommand request,
        CancellationToken cancellationToken)
    {
        var attached = await _directory.AttachReferenceAsync(
            request.ProductId,
            request.Model.MediaAssetId,
            request.Model.AltText,
            cancellationToken);
        if (attached.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ProductMediaItemView>>(attached.Errors);
        }

        var listed = await _directory.ListAsync(request.ProductId, cancellationToken);
        if (listed.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ProductMediaItemView>>(listed.Errors);
        }

        return Result.Success(GetProductMediaHandler.Map(listed.Value));
    }
}
