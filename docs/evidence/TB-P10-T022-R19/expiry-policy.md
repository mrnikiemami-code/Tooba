# TB-P10-T022-R19 — Expiry Policy

When a CartLine carries MerchandisingCampaignId and later revalidation finds the campaign ineligible
(expired window, archived, wrong store, membership removed, missing campaign AuthoredPrice):

1. Server resolves Base AuthoredPrice via ResolvePriceAsync.
2. Line.QuotedAmount/PriceId update to Base.
3. MerchandisingCampaignId is cleared (effectiveCampaign = null).
4. Never silently preserve expired campaign amount through checkout.
5. Checkout re-resolves again; PRICE_CHANGED if cart quote still stale.

Purchasable products remain addable at Base price.
