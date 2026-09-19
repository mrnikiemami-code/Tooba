# TB-P10-T022-R15 — DB Indexes

Schema: `promotion`

## merchandising_promotion_types

- UNIQUE `code`
- INDEX `(is_active, sort_order)`

## merchandising_promotion_type_translations

- PK `(type_id, locale)` → unique type+locale

## merchandising_campaigns

- INDEX `store_id`
- INDEX `promotion_type_id`
- INDEX `(store_id, promotion_type_id, lifecycle_status)` — resolve active by store+type
- INDEX `(lifecycle_status, start_at, end_at)` — window filter
- INDEX `(store_id, priority)` — priority among store campaigns

## merchandising_campaign_translations

- PK `(campaign_id, locale)` → unique campaign+locale

## merchandising_campaign_offers

- UNIQUE `(campaign_id, seller_offer_id)`
- INDEX `seller_offer_id` — reverse lookup
- INDEX `(campaign_id, sort_order)` — ordered members

## Not created

- Campaign price child indexes (price deferred)
- Cross-schema FK to offer/pricing/inventory
