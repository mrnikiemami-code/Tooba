using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;

namespace Tooba.Inventory.Infrastructure.Directories;

/// <summary>
/// Cross-module lookup helpers for <see cref="InventoryDirectory"/>: Offer and Catalog reads through
/// their Contracts-only ports, each decorated with <see cref="IModuleCallTracer"/> so the Inventory
/// boundary keeps canonical trace continuity. Extracted into a cohesive partial of the same class.
/// </summary>
public sealed partial class InventoryDirectory
{
    private async Task<OfferReference?> FindOfferAsync(Guid offerId, CancellationToken cancellationToken)
    {
        using var trace = _tracer.Begin("Inventory", "Offer", "LookupOffer");
        try
        {
            var offer = await _offers.FindOfferAsync(offerId, cancellationToken).ConfigureAwait(false);
            trace.SetOk();
            return offer;
        }
        catch (Exception ex)
        {
            trace.SetError(ex);
            throw;
        }
    }

    private async Task<CatalogVariantLookupResult?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken)
    {
        using var trace = _tracer.Begin("Inventory", "Catalog", "LookupVariant");
        try
        {
            var variant = await _catalog.FindVariantAsync(variantId, cancellationToken).ConfigureAwait(false);
            trace.SetOk();
            return variant;
        }
        catch (Exception ex)
        {
            trace.SetError(ex);
            throw;
        }
    }
}
