using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts.Errors;
using Tooba.Cart.Domain.Aggregates;
using Tooba.Cart.Domain.Entities;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Pricing.Contracts;

namespace Tooba.Cart.Infrastructure.Directories;

/// <summary>
/// Cart-owned offer/pricing/inventory quote-and-validate collaboration.
/// Owns the only place that turns a foreign quote contract into an accepted Cart line.
/// No foreign DbContext is touched and no distributed transaction exists.
/// </summary>
internal sealed class CartQuoteValidator(
    IOfferLookupGateway offers,
    IPriceLookupGateway prices,
    IQuantityNormalizer normalizer,
    ICatalogCartQuantityPolicyGateway? catalog = null,
    ICampaignCartPriceAuthority? campaignPrices = null)
{
    /// <summary>True when a merchandising campaign price authority is wired for this process.</summary>
    public bool HasCampaignAuthority => campaignPrices is not null;

    /// <summary>
    /// Resolves the offer, applies the effective quantity policy, and quotes the line currency.
    /// Every rejection is a typed stable Cart code; unknown failures stay unexpected.
    /// </summary>
    public async Task<(OfferReference Offer, PriceQuote Quote, decimal Quantity, Guid? EffectiveCampaignId)>
        ValidateOfferAndQuoteAsync(
            ShoppingCart cart,
            Guid offerId,
            decimal quantity,
            string selectedCurrency,
            DateTimeOffset now,
            Guid? merchandisingCampaignId,
            CancellationToken cancellationToken)
    {
        var offer = await offers.FindOfferAsync(offerId, cancellationToken)
            ?? throw new SemanticException(new SemanticError(CartErrorCodes.OfferUnavailable));
        if (offer.Status != OfferStatus.Active)
        {
            throw new SemanticException(new SemanticError(CartErrorCodes.OfferUnavailable));
        }

        if (offer.Channel != cart.Channel)
        {
            throw new SemanticException(new SemanticError(CartErrorCodes.OfferUnavailable));
        }

        if (catalog is not null)
        {
            var policy = await catalog.GetEffectiveQuantityPolicyForVariantAsync(offer.CatalogVariantId, cancellationToken)
                ?? throw new SemanticException(new SemanticError(CartErrorCodes.QuantityInvalid));
            quantity = normalizer.Normalize(quantity, policy);
        }

        CartLine.EnsureQuantity(quantity);
        if (offer.MinimumOrderQuantity is { } min && quantity < min)
        {
            throw new SemanticException(new SemanticError(CartErrorCodes.QuantityInvalid));
        }

        if (offer.MaximumOrderQuantity is { } max && quantity > max)
        {
            throw new SemanticException(new SemanticError(CartErrorCodes.QuantityInvalid));
        }

        Guid? effectiveCampaignId = null;
        PriceQuote? campaignQuote = null;
        if (merchandisingCampaignId is Guid campaignId
            && campaignId != Guid.Empty
            && campaignPrices is not null)
        {
            campaignQuote = await campaignPrices.TryResolveEligibleCampaignPriceAsync(
                campaignId,
                offerId,
                cart.Market,
                cart.Channel,
                selectedCurrency,
                now,
                cancellationToken);
            if (campaignQuote is not null)
            {
                effectiveCampaignId = campaignId;
            }
        }

        if (campaignQuote is not null)
        {
            return (offer, campaignQuote, quantity, effectiveCampaignId);
        }

        var quote = await prices.ResolvePriceAsync(
            new PriceResolutionQuery(offerId, cart.Market, cart.Channel, selectedCurrency, now, null, null, quantity),
            cancellationToken)
            ?? throw new SemanticException(new SemanticError(CartErrorCodes.PricingQuoteMissing));
        return (offer, quote, quantity, null);
    }
}
