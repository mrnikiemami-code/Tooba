namespace Tooba.Catalog.Application.Development;

/// <summary>
/// Host-owned cross-module enricher: Offer/Pricing/Inventory/Tax/Party sellable snapshot
/// after Catalog attribute-schema Development seed.
/// </summary>
public interface ICatalogAttributeSchemaSellableEnricher
{
    /// <summary>Ensures the schema demo product is Published and sellable (minimal Offer/stock).</summary>
    Task EnsurePublishedAndSellableAsync(CancellationToken cancellationToken = default);
}
