using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Catalog.Contracts;
using Tooba.Inventory.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Party.Contracts;
using Tooba.Pricing.Contracts;

namespace Tooba.Offer.Infrastructure.Adapters;

/// <summary>Traced Offer→Catalog variant lookup boundary.</summary>
internal sealed class TracedCatalogVariantLookup(ICatalogVariantLookup inner, IModuleCallTracer tracer) : ICatalogVariantLookup
{
    public async Task<CatalogVariantLookupResult?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken)
    {
        using var trace = tracer.Begin("Offer", "Catalog", "LookupVariant");
        try
        {
            var result = await inner.FindVariantAsync(variantId, cancellationToken).ConfigureAwait(false);
            trace.SetOk();
            return result;
        }
        catch (Exception ex)
        {
            trace.SetError(ex);
            throw;
        }
    }
}

/// <summary>Traced Offer→Catalog presentation read boundary.</summary>
internal sealed class TracedCatalogOfferReadGateway(ICatalogOfferReadGateway inner, IModuleCallTracer tracer) : ICatalogOfferReadGateway
{
    public async Task<IReadOnlyDictionary<Guid, CatalogOfferPresentation>> GetOfferPresentationsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken)
    {
        using var trace = tracer.Begin("Offer", "Catalog", "LookupPresentations");
        try
        {
            var result = await inner.GetOfferPresentationsAsync(catalogVariantIds, cancellationToken).ConfigureAwait(false);
            trace.SetOk();
            return result;
        }
        catch (Exception ex)
        {
            trace.SetError(ex);
            throw;
        }
    }
}

/// <summary>Traced Offer→Party lookup boundary.</summary>
internal sealed class TracedPartyLookup(IPartyLookup inner, IModuleCallTracer tracer) : IPartyLookup
{
    public async Task<PartyLookupResult?> FindByIdAsync(Guid partyId, CancellationToken cancellationToken)
    {
        using var trace = tracer.Begin("Offer", "Party", "LookupParty");
        try
        {
            var result = await inner.FindByIdAsync(partyId, cancellationToken).ConfigureAwait(false);
            trace.SetOk();
            return result;
        }
        catch (Exception ex)
        {
            trace.SetError(ex);
            throw;
        }
    }
}

/// <summary>Traced Offer→Pricing lookup boundary.</summary>
internal sealed class TracedPriceLookupGateway(IPriceLookupGateway inner, IModuleCallTracer tracer) : IPriceLookupGateway
{
    public async Task<PriceQuote?> ResolvePriceAsync(PriceResolutionQuery query, CancellationToken cancellationToken)
    {
        using var trace = tracer.Begin("Offer", "Pricing", "LookupPrice");
        try
        {
            var result = await inner.ResolvePriceAsync(query, cancellationToken).ConfigureAwait(false);
            trace.SetOk();
            return result;
        }
        catch (Exception ex)
        {
            trace.SetError(ex);
            throw;
        }
    }

    public async Task<IReadOnlyDictionary<Guid, PriceQuote>> ResolvePricesBatchAsync(
        IReadOnlyCollection<Guid> offerIds,
        string market,
        SalesChannel channel,
        string currency,
        DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        using var trace = tracer.Begin("Offer", "Pricing", "LookupPricesBatch");
        try
        {
            var result = await inner.ResolvePricesBatchAsync(offerIds, market, channel, currency, at, cancellationToken)
                .ConfigureAwait(false);
            trace.SetOk();
            return result;
        }
        catch (Exception ex)
        {
            trace.SetError(ex);
            throw;
        }
    }

    public async Task<IReadOnlyDictionary<Guid, PriceQuote>> ResolveCampaignPricesBatchAsync(
        IReadOnlyCollection<Guid> offerIds,
        Guid campaignId,
        string market,
        SalesChannel channel,
        string currency,
        DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        using var trace = tracer.Begin("Offer", "Pricing", "LookupCampaignPricesBatch");
        try
        {
            var result = await inner.ResolveCampaignPricesBatchAsync(
                    offerIds, campaignId, market, channel, currency, at, cancellationToken)
                .ConfigureAwait(false);
            trace.SetOk();
            return result;
        }
        catch (Exception ex)
        {
            trace.SetError(ex);
            throw;
        }
    }
}

/// <summary>Traced Offer→Inventory availability boundary.</summary>
internal sealed class TracedSellerOfferInventoryGateway(ISellerOfferInventoryGateway inner, IModuleCallTracer tracer)
    : ISellerOfferInventoryGateway
{
    public async Task<IReadOnlyDictionary<Guid, OfferInventorySummary>> GetAvailabilityAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken)
    {
        using var trace = tracer.Begin("Offer", "Inventory", "LookupAvailability");
        try
        {
            var result = await inner.GetAvailabilityAsync(offerIds, cancellationToken).ConfigureAwait(false);
            trace.SetOk();
            return result;
        }
        catch (Exception ex)
        {
            trace.SetError(ex);
            throw;
        }
    }

    public async Task SetInventoryAsync(SetSellerOfferInventory request, CancellationToken cancellationToken)
    {
        using var trace = tracer.Begin("Offer", "Inventory", "SetInventory");
        try
        {
            await inner.SetInventoryAsync(request, cancellationToken).ConfigureAwait(false);
            trace.SetOk();
        }
        catch (Exception ex)
        {
            trace.SetError(ex);
            throw;
        }
    }
}
