using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Categories.Ports;

namespace Tooba.Catalog.Application.Categories.Commands;

/// <summary>Updates category core then returns workspace.</summary>
public sealed class UpdateCategoryCoreHandler
    : IRequestHandler<UpdateCategoryCoreCommand, Result<CategoryWorkspaceSummaryDto>>
{
    private readonly ICategoryDirectory _categories;

    /// <summary>Creates the handler.</summary>
    public UpdateCategoryCoreHandler(ICategoryDirectory categories) => _categories = categories;

    /// <inheritdoc />
    public async Task<Result<CategoryWorkspaceSummaryDto>> Handle(
        UpdateCategoryCoreCommand request,
        CancellationToken cancellationToken)
    {
        var update = await _categories.UpdateCoreAsync(
            request.CategoryId,
            new CategoryCoreUpdateRequest(
                request.Status,
                request.SortOrder,
                request.IsVisible,
                request.ImageMediaAssetId,
                request.IconMediaAssetId,
                request.BannerMediaAssetId,
                request.ClearImage,
                request.ClearIcon,
                request.ClearBanner,
                request.ExpectedUpdatedAt),
            cancellationToken);
        if (update.IsFailure)
        {
            return Result.Failure<CategoryWorkspaceSummaryDto>(update.Errors);
        }

        return await _categories.GetWorkspaceAsync(request.CategoryId, null, cancellationToken);
    }
}
