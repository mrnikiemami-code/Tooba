using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Seller;
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

    public async Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
        IReadOnlyList<Guid> variantIds,
        CancellationToken cancellationToken)
    {
        using var trace = tracer.Begin("Offer", "Catalog", "LookupPrimaryCategoryIds");
        try
        {
            var result = await inner.GetPrimaryCategoryIdsByVariantIdsAsync(variantIds, cancellationToken)
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

    public async Task<IReadOnlyDictionary<Guid, string>> GetVariantTitlesAsync(
        IReadOnlyList<Guid> variantIds,
        CancellationToken cancellationToken)
    {
        using var trace = tracer.Begin("Offer", "Catalog", "LookupVariantTitles");
        try
        {
            var result = await inner.GetVariantTitlesAsync(variantIds, cancellationToken).ConfigureAwait(false);
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

    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IReadOnlyList<Guid> partyIds,
        CancellationToken cancellationToken)
    {
        using var trace = tracer.Begin("Offer", "Party", "LookupDisplayNames");
        try
        {
            var result = await inner.GetDisplayNamesAsync(partyIds, cancellationToken).ConfigureAwait(false);
            trace.SetOk();
            return result;
        }
        catch (Exception ex)
        {
            trace.SetError(ex);
            throw;
        }
    }

    public async Task<IReadOnlyList<Guid>> SearchIdsByDisplayNameAsync(
        string term,
        int take,
        CancellationToken cancellationToken)
    {
        using var trace = tracer.Begin("Offer", "Party", "SearchIdsByDisplayName");
        try
        {
            var result = await inner.SearchIdsByDisplayNameAsync(term, take, cancellationToken).ConfigureAwait(false);
            trace.SetOk();
            return result;
        }
        catch (Exception ex)
        {
            trace.SetError(ex);
            throw;
        }
    }

    public async Task<IReadOnlyList<Guid>> FilterIdsByDisplayNameAsync(
        string? op,
        string? value,
        IReadOnlyList<string>? values,
        int take,
        CancellationToken cancellationToken)
    {
        using var trace = tracer.Begin("Offer", "Party", "FilterIdsByDisplayName");
        try
        {
            var result = await inner.FilterIdsByDisplayNameAsync(op, value, values, take, cancellationToken)
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

    public async Task<Result> SetInventoryAsync(SetSellerOfferInventory request, CancellationToken cancellationToken)
    {
        using var trace = tracer.Begin("Offer", "Inventory", "SetInventory");
        try
        {
            var result = await inner.SetInventoryAsync(request, cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
            {
                trace.SetBusinessFailure(result.FirstError.Code);
            }
            else
            {
                trace.SetOk();
            }

            return result;
        }
        catch (Exception ex)
        {
            trace.SetError(ex);
            throw;
        }
    }
}
