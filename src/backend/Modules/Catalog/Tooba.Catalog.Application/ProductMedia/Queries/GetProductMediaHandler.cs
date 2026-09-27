using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductMedia.Models;
using Tooba.Catalog.Application.ProductMedia.Ports;

namespace Tooba.Catalog.Application.ProductMedia.Queries;

/// <summary>Handles GetProductMediaQuery.</summary>
public sealed class GetProductMediaHandler
    : IRequestHandler<GetProductMediaQuery, Result<IReadOnlyList<ProductMediaItemView>>>
{
    private readonly IProductMediaDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public GetProductMediaHandler(IProductMediaDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProductMediaItemView>>> Handle(
        GetProductMediaQuery request,
        CancellationToken cancellationToken)
    {
        var listed = await _directory.ListAsync(request.ProductId, cancellationToken);
        if (listed.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ProductMediaItemView>>(listed.Errors);
        }

        return Result.Success(Map(listed.Value));
    }

    internal static IReadOnlyList<ProductMediaItemView> Map(IReadOnlyList<ProductMediaAssignment> items) =>
        items
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayOrder)
            .Select(m => new ProductMediaItemView(m.MediaAssetId, m.IsPrimary, m.DisplayOrder, m.AltText))
            .ToList();
}
