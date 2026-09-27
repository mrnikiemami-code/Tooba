namespace Tooba.Catalog.Application.Variants.Ports;

/// <summary>
/// Narrow Catalog Application port for Offer count enrichment on variant surfaces.
/// Implemented in Infrastructure against Offer.Contracts only.
/// </summary>
public interface IVariantOfferLookup
{
    /// <summary>
    /// Counts non-archived offers for each catalog variant id.
    /// Missing keys are returned with count 0.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> CountOffersByCatalogVariantIdsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken);
}
