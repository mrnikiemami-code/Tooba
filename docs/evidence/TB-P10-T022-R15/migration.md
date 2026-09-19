# TB-P10-T022-R15 — Migration

## Migration

- Name: `20260919134400_MerchandisingCampaignFoundation`
- After: `20260823210000_InitialPromotion`
- Schema: `promotion`
- Reversible: YES (`Down` drops new tables only)

## Tables added

1. `merchandising_promotion_types`
2. `merchandising_promotion_type_translations`
3. `merchandising_campaigns`
4. `merchandising_campaign_translations`
5. `merchandising_campaign_offers`

## Preserved / untouched

- `promotion.promotions` (checkout `PromotionDefinition`)
- `promotion.outbox_messages`
- Offer / Pricing / Inventory schemas and tables
- No `IsAmazing` column anywhere
- No repurposing of checkout promotion rows

## Seed (not in migration SQL)

Idempotent application seed via `IMerchandisingCampaignDirectory.EnsureAmazingTypeSeededAsync`:

- Code `AMAZING`
- FA `fa-IR`: پیشنهاد شگفت‌انگیز
- EN `en-US`: Amazing Offers

Called from `ProductWorkspaceDevelopmentBootstrap` immediately after `PromotionDbContext` migrate.

## Validation

Focused tests call `Database.MigrateAsync()` on Testcontainers Postgres and assert physical table + unique code index presence.

Local Dev DB (`tooba_alpha`): migration `20260919134400_MerchandisingCampaignFoundation` applied; `\dt promotion.merchandising*` lists five tables.
