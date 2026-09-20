namespace Tooba.Catalog.Contracts;

/// <summary>Provides a stable cross-module Catalog variant lookup.</summary>
public interface ICatalogVariantLookup
{
    /// <summary>Finds a variant by its stable identifier.</summary>
    Task<CatalogVariantLookupResult?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken);
}

/// <summary>Minimal Catalog variant identity required by consumers.</summary>
public sealed record CatalogVariantLookupResult(Guid VariantId, Guid ProductId);
