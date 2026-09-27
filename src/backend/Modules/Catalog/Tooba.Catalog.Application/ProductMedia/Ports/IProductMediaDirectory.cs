using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.ProductMedia.Ports;

/// <summary>Catalog-owned port for Admin product media editor, readiness, and mutations.</summary>
public interface IProductMediaDirectory
{
    /// <summary>Ordered media list for the product gallery editor.</summary>
    Task<Result<IReadOnlyList<ProductMediaAssignment>>> ListAsync(
        Guid productId,
        CancellationToken cancellationToken);

    /// <summary>Gallery readiness (primary + count) for publish gates.</summary>
    Task<Result<ProductMediaReadiness>> GetReadinessAsync(
        Guid productId,
        CancellationToken cancellationToken);

    /// <summary>Attach an existing opaque media asset reference.</summary>
    Task<Result> AttachReferenceAsync(
        Guid productId,
        Guid mediaAssetId,
        string? altText,
        CancellationToken cancellationToken);

    /// <summary>Generate a placeholder asset id and attach it.</summary>
    Task<Result<Guid>> AttachPlaceholderAsync(
        Guid productId,
        string? altText,
        CancellationToken cancellationToken);

    /// <summary>Rewrite gallery order; list must be the exact current set.</summary>
    Task<Result> ReorderAsync(
        Guid productId,
        IReadOnlyList<Guid> orderedMediaAssetIds,
        CancellationToken cancellationToken);

    /// <summary>Set exactly one primary media reference.</summary>
    Task<Result> SetPrimaryAsync(
        Guid productId,
        Guid mediaAssetId,
        CancellationToken cancellationToken);

    /// <summary>Patch alt text on an assigned media reference.</summary>
    Task<Result> PatchAltAsync(
        Guid productId,
        Guid mediaAssetId,
        string? altText,
        CancellationToken cancellationToken);

    /// <summary>Unassign media reference; shared asset is not deleted.</summary>
    Task<Result> DetachAsync(
        Guid productId,
        Guid mediaAssetId,
        CancellationToken cancellationToken);
}
