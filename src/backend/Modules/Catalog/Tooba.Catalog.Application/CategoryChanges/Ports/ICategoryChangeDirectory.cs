using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.CategoryChanges.Ports;

/// <summary>Catalog-owned port for Admin category-change preview and primary-category replace.</summary>
public interface ICategoryChangeDirectory
{
    /// <summary>Structural impact of changing product primary category (no localization).</summary>
    Task<Result<CategoryChangeImpact>> PreviewAsync(
        Guid productId,
        Guid newCategoryId,
        CancellationToken cancellationToken);

    /// <summary>Localized Admin confirmation report for category change.</summary>
    Task<Result<CategoryChangeImpactReport>> PreviewReportAsync(
        Guid productId,
        Guid newCategoryId,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>Replaces primary category in one transaction (orphan cleanup, axes, variants, safety unpublish, history).</summary>
    Task<Result<CategoryChangeImpact>> ReplacePrimaryAsync(
        Guid productId,
        Guid newCategoryId,
        CancellationToken cancellationToken);
}
