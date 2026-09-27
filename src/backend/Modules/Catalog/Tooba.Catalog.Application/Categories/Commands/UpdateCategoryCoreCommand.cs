using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Categories.Commands;

/// <summary>Updates non-local category core fields and returns workspace.</summary>
public sealed record UpdateCategoryCoreCommand(
    Guid CategoryId,
    CatalogPublicationStatus? Status,
    int? SortOrder,
    bool? IsVisible,
    Guid? ImageMediaAssetId,
    Guid? IconMediaAssetId,
    Guid? BannerMediaAssetId,
    bool ClearImage,
    bool ClearIcon,
    bool ClearBanner,
    DateTimeOffset? ExpectedUpdatedAt)
    : IRequest<Result<CategoryWorkspaceSummaryDto>>;
