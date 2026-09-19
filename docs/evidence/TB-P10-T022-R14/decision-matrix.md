# Decision Matrix

| Decision | Value |
|---|---|
| Reuse existing checkout `promotion.promotions` as Amazing rail? | **NO** |
| Need PromotionType (AMAZING)? | **YES** (new closed type/registry) |
| Need Campaign table? | **YES** |
| Need CampaignOffer membership? | **YES** |
| Need campaign translations? | **YES** (user-facing badge/title) |
| Promotional price override at membership? | **YES** (optional `PromoAmount`; list from AuthoredPrice) |
| Quantity allocation? | **LATER** |
| Early access? | **LATER** |
| Teasing/future state? | **Architect now / implement LATER** (window + visibility) |
| Scheduled job required? | **NO** for correctness if queries use windows; optional sweeper later |
| Cache? | **YES** — short TTL + invalidation |
| Builder internal source | `PromotionCampaign` |
| Builder user-facing label | پیشنهاد شگفت‌انگیز |
| Builder CampaignId nullable | **YES** — null = current active AMAZING |
| Seed data strategy | Idempotent Dev seed on real Offers (next phase) |
| First implementation task | Phase 1 schema/domain foundation (Merchandising Campaign + membership + type + translations) |
