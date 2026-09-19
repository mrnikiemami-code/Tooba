# TB-P10-T022-R18 — Runtime Proof

Host Development @ 127.0.0.1:5088. Probe: docs/evidence/TB-P10-T022-R18/probe.mjs → probe-report.json

Results:
- Preview PromotionCampaign AMAZING: productCount=7, promoCount=3, baseOnlyCount=4 PASS
- Published storefront public page: promoCount=3, baseOnlyCount≥1 PASS
- Discount math (promo < offer) PASS
- Seed prefers Published Catalog products so ProductCards receive campaign AuthoredPrice overlays
- Cart/checkout campaign context: guarded gap (cart-price-integrity.md); L/M/N not claimed
- No client price correction; prices from Host JSON
