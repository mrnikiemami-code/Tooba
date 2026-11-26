using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Errors;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Contracts.ReturnPolicy;
using Tooba.Offer.Domain.Aggregates;
using Tooba.BuildingBlocks.Results;
using Tooba.Party.Contracts.Ports;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Contracts.Ports;
using Tooba.Pricing.Contracts.Seller;
using Tooba.StoreContext.Contracts.Current;

namespace Tooba.Offer.Application.Offers.ReadModels;

/// <summary>Composes Offer-owned seller read models through stable owner contracts.</summary>
public sealed class OfferReadModelComposer(
    ICatalogOfferReadGateway catalog,
    IPriceLookupGateway pricing,
    ISellerOfferInventoryGateway inventory,
    IPartyLookup parties,
    IReturnPolicyResolver returnPolicies,
    IOfferFulfillmentAssignmentLookup fulfillmentAssignments,
    IOfferPriceHistoryReader priceHistory,
    IClock clock,
    ICurrentStoreCommerceContext? storeCommerce = null)
{
    private string? Market => storeCommerce?.Current?.Market;
    private string? Currency => storeCommerce?.Current?.DefaultCurrency;

    /// <summary>Builds enriched list items for the supplied Offer aggregates.</summary>
    public async Task<IReadOnlyList<SellerOfferListItem>> ListAsync(
        IReadOnlyList<SellerOffer> offers,
        CancellationToken cancellationToken,
        string? locale = null)
    {
        if (offers.Count == 0) return [];
        var offerIds = offers.Select(x => x.OfferId).ToArray();
        var catalogRows = await catalog.GetOfferPresentationsAsync(
            offers.Select(x => x.CatalogVariantId).Distinct().ToArray(), cancellationToken, locale);
        var prices = new Dictionary<Guid, PriceQuote>();
        if (!string.IsNullOrWhiteSpace(Market) && !string.IsNullOrWhiteSpace(Currency))
        {
            foreach (var channel in offers.GroupBy(x => x.Channel))
            {
                var batch = await pricing.ResolvePricesBatchAsync(channel.Select(x => x.OfferId).ToArray(),
                    Market!, (SalesChannel)(int)channel.Key, Currency!, clock.UtcNow, cancellationToken);
                foreach (var pair in batch) prices[pair.Key] = pair.Value;
            }
        }
        var stocks = await inventory.GetAvailabilityAsync(offerIds, cancellationToken);
        var sellerNames = await parties.GetDisplayNamesAsync(
            offers.Select(x => x.SellerPartyId).Distinct().ToArray(), cancellationToken);
        return offers.Select(offer =>
        {
            catalogRows.TryGetValue(offer.CatalogVariantId, out var product);
            prices.TryGetValue(offer.OfferId, out var price);
            stocks.TryGetValue(offer.OfferId, out var stock);
            return new SellerOfferListItem(
                offer.OfferId, offer.CatalogVariantId, product?.ProductId,
                product?.ProductTitle ?? string.Empty, offer.SellerSku, offer.Status.ToString(), offer.Channel.ToString(),
                price?.Amount, price?.Currency ?? Currency ?? string.Empty, stock?.Available ?? 0, offer.UpdatedAt,
                sellerNames.GetValueOrDefault(offer.SellerPartyId),
                product?.VariantCode,
                product?.PrimaryMediaAssetId,
                product?.BrandName,
                product?.VariantLabel);
        }).ToArray();
    }

    /// <summary>Builds the enriched detail model for one Offer aggregate.</summary>
    public async Task<SellerOfferDetailPage> DetailAsync(SellerOffer offer, CancellationToken cancellationToken, string? locale = null, bool adminSurface = false)
    {
        var catalogRows = await catalog.GetOfferPresentationsAsync([offer.CatalogVariantId], cancellationToken, locale);
        catalogRows.TryGetValue(offer.CatalogVariantId, out var product);
        var price = !string.IsNullOrWhiteSpace(Market) && !string.IsNullOrWhiteSpace(Currency)
            ? await pricing.ResolvePriceAsync(new PriceResolutionQuery(offer.OfferId, Market!,
                (SalesChannel)(int)offer.Channel, Currency!, clock.UtcNow, null, null, null), cancellationToken)
            : null;
        var stocks = await inventory.GetAvailabilityAsync([offer.OfferId], cancellationToken);
        stocks.TryGetValue(offer.OfferId, out var stock);
        var seller = await parties.FindByIdAsync(offer.SellerPartyId, cancellationToken);
        var options = returnPolicies.Options;
        var assignments = await fulfillmentAssignments.GetAssignmentsAsync([offer.OfferId], cancellationToken);
        assignments.TryGetValue(offer.OfferId, out var fulfillment);
        var priceRevision = await priceHistory.GetRevisionAsync(offer.OfferId, cancellationToken);
        return new SellerOfferDetailPage(
            offer.OfferId, offer.SellerPartyId, seller?.DisplayName ?? string.Empty,
            offer.CatalogVariantId, product?.ProductId, product?.ProductTitle ?? string.Empty,
            product?.BrandName, offer.SellerSku, offer.Status.ToString(), offer.Channel.ToString(),
            price?.Amount, price?.Currency ?? Currency ?? string.Empty, stock?.OnHand ?? 0, stock?.Reserved ?? 0,
            stock?.Available ?? 0, true, offer.ReturnPolicyChoice, offer.CustomReturnWindowDays,
            options.DefaultReturnWindowDays, options.SellerCanOverrideReturnPolicy,
            options.MinReturnWindowDays, options.MaxReturnWindowDays, options.AllowNonReturnableOffers,
            offer.MinimumOrderQuantity, offer.MaximumOrderQuantity,
            product?.UnitCode, product?.UnitName, product?.UnitShortName,
            stock?.Locations?.Select(location => new SellerOfferInventoryLocation(
                location.StockItemId,
                location.LocationId,
                location.LocationCode,
                location.LocationName,
                location.OnHand,
                location.Reserved,
                location.Available,
                location.Revision)).ToList(),
            product?.VariantCode,
            product?.PrimaryMediaAssetId,
            fulfillment?.Responsibility,
            fulfillment?.ShippingTypeId,
            offer.UpdatedAt,
            product?.VariantLabel,
            product?.UnitQuantityDecimalPlaces ?? 0,
            product?.UnitQuantityStep,
            product?.UnitRoundingMode,
            priceRevision?.Revision ?? "absent",
            priceRevision?.PriceId,
            stock?.OfferedQuantity,
            stock?.AllocationHeld,
            stock?.AllocationRevision,
            offer.UpdatedAt.ToString("O"),
            new OfferRevisions(offer.UpdatedAt.ToString("O"), priceRevision?.Revision ?? "absent", stock?.AllocationRevision ?? "absent"),
            new OfferCapabilities(
                adminSurface || fulfillment?.Responsibility == OfferFulfillmentResponsibility.Seller,
                fulfillment?.Responsibility == OfferFulfillmentResponsibility.Platform,
                adminSurface,
                adminSurface || fulfillment?.Responsibility == OfferFulfillmentResponsibility.Seller));
    }

    /// <summary>Creates the canonical seller-scoped not-found failure.</summary>
    public static Result<T> NotFound<T>() =>
        Result<T>.Failure(new SemanticError(OfferErrorCodes.NotFound));

    /// <summary>Creates the canonical seller-scoped not-found failure (void Result).</summary>
    public static Result NotFoundResult() =>
        Result.Failure(new SemanticError(OfferErrorCodes.NotFound));
}

