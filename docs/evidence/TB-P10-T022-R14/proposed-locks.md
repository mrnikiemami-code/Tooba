# PROPOSED-LOCKS (Architect approval required — not written to lock registry)

1. **PROPOSED-LOCK-PROMO-001** — No `Offer.IsAmazing` / boolean explosion for campaign kinds.
2. **PROPOSED-LOCK-PROMO-002** — Do not duplicate Pricing or Inventory tables for Amazing; reuse `AuthoredPrice` + `StockPosition`.
3. **PROPOSED-LOCK-PROMO-003** — Merchandising campaigns are separate from checkout `PromotionDefinition`.
4. **PROPOSED-LOCK-PROMO-004** — Product source `PromotionCampaign` inherits Page locale; no section-owned language.
5. **PROPOSED-LOCK-PROMO-005** — Countdown/timer is derived from campaign `EndAt`; never persisted as ticking state.
6. **PROPOSED-LOCK-PROMO-006** — Internal source key remains generic (`PromotionCampaign`); user label may be پیشنهاد شگفت‌انگیز.
7. **PROPOSED-LOCK-PROMO-007** — Discount percent derived when list + promo selling available.

Canonical LOCK-SF-001…390 unchanged this task.
