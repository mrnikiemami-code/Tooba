namespace Tooba.Catalog.Application.Development;

/// <summary>
/// Catalog-owned Development enricher contract invoked by the schema development seed.
/// </summary>
public interface ICatalogAttributeSchemaSellableEnricher
{
    /// <summary>Ensures the schema demo product is Published and sellable (minimal Offer/Pricing/Inventory/Tax).</summary>
    Task EnsurePublishedAndSellableAsync(CancellationToken cancellationToken = default);
}
