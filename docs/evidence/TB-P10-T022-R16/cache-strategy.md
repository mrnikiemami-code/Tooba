# TB-P10-T022-R16 — Cache Strategy

## Decision

**Deferred** — no complex cache in R16.

## Planned key shape (next phase)

`StoreId + PromotionTypeCode + Locale + campaign window/version`

## Invalidation triggers (when implemented)

- Campaign publish/update/archive
- Membership reorder/add/remove
- Translation update
- Stock/price changes (TTL or event-driven)

Extension point: wrap `IMerchandisingCampaignQuery` in Host decorator when Product Showcase wires Amazing source.
