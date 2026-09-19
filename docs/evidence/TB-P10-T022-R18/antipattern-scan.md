# TB-P10-T022-R18 — AntiPattern Scan

- No CampaignOffer.PromoAmount scalar
- No second pricing engine
- No client-authoritative promo
- No stored discount percent
- No fake compare-at (promo>=base → null)
- Future/expired applyCampaignPrices=false
- Bulk ResolveCampaignPricesBatchAsync
- No Amazing-specific ProductCard / Builder redesign
- Capability map is index only (not mandatory per-task read)
