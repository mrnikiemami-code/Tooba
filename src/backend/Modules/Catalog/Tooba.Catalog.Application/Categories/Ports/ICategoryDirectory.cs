using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Categories.Models;

namespace Tooba.Catalog.Application.Categories.Ports;

/// <summary>Catalog-owned port for Category Admin + Storefront operations.</summary>
public interface ICategoryDirectory
{
    /// <summary>Reads the category tree for a locale (optional search).</summary>
    Task<Result<IReadOnlyList<CategoryTreeNodeDto>>> GetTreeAsync(
        string locale,
        string? search,
        CancellationToken cancellationToken);

    /// <summary>Reads category workspace; missing → catalog.category.missing.</summary>
    Task<Result<CategoryWorkspaceSummaryDto>> GetWorkspaceAsync(
        Guid categoryId,
        string? locale,
        CancellationToken cancellationToken);

    /// <summary>Creates a category (structured Translations and/or legacy LocalizedNames).</summary>
    Task<Result<CategoryReference>> CreateAsync(
        CreateCategoryWriteModel model,
        CancellationToken cancellationToken);

    /// <summary>Updates non-local core fields.</summary>
    Task<Result> UpdateCoreAsync(
        Guid categoryId,
        CategoryCoreUpdateRequest request,
        CancellationToken cancellationToken);

    /// <summary>Inserts or updates a locale translation; slug changes write history.</summary>
    Task<Result<CategoryTranslationDto>> UpsertTranslationAsync(
        Guid categoryId,
        CategoryTranslationUpsertRequest request,
        CancellationToken cancellationToken);

    /// <summary>Moves a category under a new parent with cycle/depth checks.</summary>
    Task<Result> MoveAsync(
        Guid categoryId,
        Guid? newParentId,
        DateTimeOffset? expectedUpdatedAt,
        CancellationToken cancellationToken);

    /// <summary>Rewrites sibling SortOrder for the exact sibling set.</summary>
    Task<Result> ReorderAsync(
        Guid? parentId,
        IReadOnlyList<Guid> orderedCategoryIds,
        CancellationToken cancellationToken);

    /// <summary>Publishes a category.</summary>
    Task<Result> PublishAsync(Guid categoryId, CancellationToken cancellationToken);

    /// <summary>Archives a category.</summary>
    Task<Result> ArchiveAsync(Guid categoryId, CancellationToken cancellationToken);

    /// <summary>Resolves locale+slug to current category or historical redirect.</summary>
    Task<Result<CategoryRouteResolveResult>> ResolveRouteAsync(
        string locale,
        string slug,
        bool forStorefront,
        CancellationToken cancellationToken);
}
