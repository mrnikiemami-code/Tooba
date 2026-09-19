# TB-P10-T022-R19 — Cart/Checkout Pricing Discovery

## Path
ProductCard (PromotionCampaign overlay sets MerchandisingCampaignId)
→ FE addOfferToCart(offerId, qty, merchandisingCampaignId?)
→ POST /v1/storefront/cart/{id}/lines body { offerId, quantity, merchandisingCampaignId? }
→ StorefrontCartComposer.AddLineAsync → ICartDirectory.AddOrIncreaseLineAsync
→ CartDirectory.ValidateOfferAndQuoteAsync:
  - optional ICampaignCartPriceAuthority.TryResolveEligibleCampaignPriceAsync
  - else IPriceLookupGateway.ResolvePriceAsync (Base)
→ CartLine persists MerchandisingCampaignId + QuotedAmount/PriceId from server quote
→ GetCartAsync / quantity change → RevalidateCampaignQuotesAsync
→ CheckoutDirectory commit → ResolveCheckoutLineQuoteAsync → PRICE_CHANGED if drift
→ OrderLine snapshots UnitPriceSnapshot + PriceId (immutable)

## DTOs
- StorefrontAddCartLineRequest(OfferId, Quantity, MerchandisingCampaignId?)
- CartLine.MerchandisingCampaignId (nullable Guid)
- CartLineSnapshot.MerchandisingCampaignId
- No client unit price / discount fields accepted

## Stale handling
Ineligible campaign → Base quote; MerchandisingCampaignId cleared when effectiveCampaign becomes null.
Checkout throws PRICE_CHANGED when QuotedAmount/PriceId differs from re-resolved quote.
