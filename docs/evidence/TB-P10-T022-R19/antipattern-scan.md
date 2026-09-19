# TB-P10-T022-R19 — Anti-Pattern Scan

Rejected:
- trusting client unit price / discount %
- CampaignId as automatic discount without Store/active/member/price validation
- duplicated pricing engine
- stale promo through checkout without revalidation
- campaign title/timer on CartLine
- Builder / Storefront redesign / P11
- cart localStorage as price truth

Implemented:
- MerchandisingCampaignId context only
- CampaignCartPriceAuthority server validation
- Base fallback + PRICE_CHANGED at checkout
