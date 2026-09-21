using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Domain.Aggregates;
using Tooba.BuildingBlocks.Results;
using Tooba.Party.Contracts;
using Tooba.Pricing.Contracts;

namespace Tooba.Offer.Application.ReadModels;

/// <summary>Composes Offer-owned seller read models through stable owner contracts.</summary>
public sealed class OfferReadModelComposer(
    ICatalogOfferReadGateway catalog,
    IPriceLookupGateway pricing,
    ISellerOfferInventoryGateway inventory,
    IPartyLookup parties,
    IReturnPolicyResolver returnPolicies,
    IClock clock)
{
    private const string DefaultMarket = "IR";
    private const string DefaultCurrency = "IRR";

    /// <summary>Builds enriched list items for the supplied Offer aggregates.</summary>
    public async Task<IReadOnlyList<SellerOfferListItem>> ListAsync(
        IReadOnlyList<SellerOffer> offers,
        CancellationToken cancellationToken)
    {
        if (offers.Count == 0) return [];
        var offerIds = offers.Select(x => x.OfferId).ToArray();
        var catalogRows = await catalog.GetOfferPresentationsAsync(
            offers.Select(x => x.CatalogVariantId).Distinct().ToArray(), cancellationToken);
        var prices = await pricing.ResolvePricesBatchAsync(
            offerIds, DefaultMarket, SalesChannel.Marketplace, DefaultCurrency, clock.UtcNow, cancellationToken);
        var stocks = await inventory.GetAvailabilityAsync(offerIds, cancellationToken);
        return offers.Select(offer =>
        {
            catalogRows.TryGetValue(offer.CatalogVariantId, out var product);
            prices.TryGetValue(offer.OfferId, out var price);
            stocks.TryGetValue(offer.OfferId, out var stock);
            return new SellerOfferListItem(
                offer.OfferId, offer.CatalogVariantId, product?.ProductId,
                product?.ProductTitle ?? string.Empty, offer.SellerSku, offer.Status.ToString(),
                price?.Amount, price?.Currency ?? DefaultCurrency, stock?.Available ?? 0, offer.UpdatedAt);
        }).ToArray();
    }

    /// <summary>Builds the enriched detail model for one Offer aggregate.</summary>
    public async Task<SellerOfferDetailPage> DetailAsync(SellerOffer offer, CancellationToken cancellationToken)
    {
        var catalogRows = await catalog.GetOfferPresentationsAsync([offer.CatalogVariantId], cancellationToken);
        catalogRows.TryGetValue(offer.CatalogVariantId, out var product);
        var price = await pricing.ResolvePriceAsync(new PriceResolutionQuery(
            offer.OfferId, DefaultMarket, (SalesChannel)(int)offer.Channel, DefaultCurrency,
            clock.UtcNow, null, null, null), cancellationToken);
        var stocks = await inventory.GetAvailabilityAsync([offer.OfferId], cancellationToken);
        stocks.TryGetValue(offer.OfferId, out var stock);
        var seller = await parties.FindByIdAsync(offer.SellerPartyId, cancellationToken);
        var options = returnPolicies.Options;
        return new SellerOfferDetailPage(
            offer.OfferId, offer.SellerPartyId, seller?.DisplayName ?? string.Empty,
            offer.CatalogVariantId, product?.ProductId, product?.ProductTitle ?? string.Empty,
            product?.BrandName, offer.SellerSku, offer.Status.ToString(), offer.Channel.ToString(),
            price?.Amount, price?.Currency ?? DefaultCurrency, stock?.OnHand ?? 0, stock?.Reserved ?? 0,
            stock?.Available ?? 0, true, offer.ReturnPolicyChoice, offer.CustomReturnWindowDays,
            options.DefaultReturnWindowDays, options.SellerCanOverrideReturnPolicy,
            options.MinReturnWindowDays, options.MaxReturnWindowDays, options.AllowNonReturnableOffers,
            offer.MinimumOrderQuantity, offer.MaximumOrderQuantity,
            product?.UnitCode, product?.UnitName, product?.UnitShortName);
    }

    /// <summary>Creates the canonical seller-scoped not-found failure.</summary>
    public static Result<T> NotFound<T>() =>
        Result<T>.Failure(new SemanticError(OfferErrorCodes.NotFound));

    /// <summary>Creates the canonical seller-scoped not-found failure (void Result).</summary>
    public static Result NotFoundResult() =>
        Result.Failure(new SemanticError(OfferErrorCodes.NotFound));
}
