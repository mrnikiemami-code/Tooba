using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Contracts;

/// <summary>
/// Cart-facing Catalog enrichment without Catalog.Application or DbContext leakage.
/// </summary>
public interface ICatalogCartPresentationLookup
{
    /// <summary>
    /// Variant→product presentation (slug, localized title, primary media) keyed by variant id.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, CatalogCartVariantPresentation>> GetVariantPresentationsAsync(
        IReadOnlyList<Guid> variantIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Effective quantity policies keyed by variant id.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, EffectiveQuantityPolicy>> GetEffectiveQuantityPoliciesForVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken);
}

/// <summary>Minimal product presentation for a Cart line's catalog variant.</summary>
public sealed record CatalogCartVariantPresentation(
    Guid VariantId,
    Guid ProductId,
    string? Slug,
    string LocalizedTitle,
    Guid? MediaAssetId);
