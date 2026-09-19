# TB-P10-T022-R16 — Runtime Data Proof

Proven via `MerchandisingCampaignRuntimeTests` (Testcontainers, fixed clock `2026-09-19T12:00:00Z`):

- AMAZING type seeded
- Active high-priority wins over overlapping lower priority
- Future excluded from active; future resolver returns teasing campaign
- Expired / Draft / Archived not runtime-active
- OOS + Suspended members filtered
- Member SortOrder stable after filter
- FA / EN / fallback locale
- Canonical AuthoredPrice amount/currency
- Inventory Available from StockPosition batch
- No PromoAmount on read model
- Seed upsert idempotent (same campaign Id count = 1)
- Cross-store storeId returns null/empty
- StartAt tiebreaker when Priority equal
