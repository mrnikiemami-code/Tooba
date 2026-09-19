# TB-P10-T022-R16 — Anti-Pattern Scan

| Anti-pattern | Status |
|--------------|--------|
| Builder UI / Amazing source option | ABSENT |
| Storefront Product Showcase integration | ABSENT |
| Campaign Admin UI | ABSENT |
| Offer.IsAmazing | ABSENT |
| New promotion enum identity | ABSENT (Code string master) |
| Checkout PromotionDefinition reuse | ABSENT |
| Fake frontend data | ABSENT (DB seed) |
| Hardcoded forever-expiring timestamps | ABSENT (relative UtcNow) |
| PromoAmount scalar | ABSENT |
| Fabricated discount % | ABSENT |
| Duplicate inventory truth | ABSENT (gateway reuse) |
| Per-member N+1 price/inventory | ABSENT (batch) |
| Full SellerOffer scan | ABSENT |
| Unbounded member load | ABSENT (max 48) |
| Polling / per-second jobs | ABSENT |
| P11 work | ABSENT |
| User-work overwrite | ABSENT |
