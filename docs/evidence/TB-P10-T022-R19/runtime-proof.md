# TB-P10-T022-R19 — Runtime Proof

Host Development @ 127.0.0.1:5088. Probe: docs/evidence/TB-P10-T022-R19/probe.mjs → probe-report.json EXIT 0.

Proven:
- A host ready
- B published PromotionCampaign page cards carry merchandisingCampaignId + promo overlay
- C guest cart create
- D ATC with campaignId → server unitAmount = campaign AuthoredPrice; line.merchandisingCampaignId set
- E cart reload preserves promo quote
- F quantity change preserves campaign context + promo unit
- P arbitrary CampaignId → Base price; no MerchandisingCampaignId retained
- Q client amount not in StorefrontAddCartLineRequest

Focused CampaignCartPriceIntegrityTests (Docker): normal/base, campaign promo, wrong campaign, expiry reload reprice PASS.
Checkout ResolveCheckoutLineQuoteAsync + PRICE_CHANGED path covered in code; Order snapshots PriceId/unit from accepted quote.
