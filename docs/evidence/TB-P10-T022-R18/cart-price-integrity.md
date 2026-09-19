# TB-P10-T022-R18 — Cart Price Integrity

CartDirectory.ValidateOfferAndQuoteAsync calls IPriceLookupGateway.ResolvePriceAsync → Base-only.
Add-to-cart does not accept campaignId context; QuotedAmount is Base AuthoredPrice.
**Guarded gap (follow-up):** server cart/checkout cannot yet re-resolve MerchandisingCampaign qualifier at line add. Storefront display is correct; end-to-end purchase at promo requires a later task to pass campaign context into Cart/Checkout without trusting client amounts.
No fake cart promo implemented in R18.
