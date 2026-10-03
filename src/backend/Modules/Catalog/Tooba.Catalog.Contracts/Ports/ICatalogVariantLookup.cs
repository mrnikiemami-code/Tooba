namespace Tooba.Catalog.Contracts.Ports;

/// <summary>Provides a stable cross-module Catalog variant lookup.</summary>
public interface ICatalogVariantLookup
{
    /// <summary>Finds a variant by its stable identifier.</summary>
    Task<CatalogVariantLookupResult?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken);

    /// <summary>Primary category ids for variants (seller auth / catalog snapshot fallback).</summary>
    Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
        IReadOnlyList<Guid> variantIds,
        CancellationToken cancellationToken);

    /// <summary>Localized product titles keyed by variant id (fa preferred).</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetVariantTitlesAsync(
        IReadOnlyList<Guid> variantIds,
        CancellationToken cancellationToken);

    /// <summary>Variant ids whose owning product is Published (Development / merchandising seed).</summary>
    Task<IReadOnlyList<Guid>> GetPublishedVariantIdsAsync(CancellationToken cancellationToken);
}

/// <summary>Minimal Catalog variant identity required by consumers.</summary>
public sealed record CatalogVariantLookupResult(Guid VariantId, Guid ProductId);
