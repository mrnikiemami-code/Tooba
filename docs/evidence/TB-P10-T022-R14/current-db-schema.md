# Current DB Schema

Sources: EF `*DbContextModelSnapshot` + Initial migrations. No live schema mutation this task.

## `offer.offers`

`offer_id` uuid PK; `catalog_variant_id`; `seller_party_id`; `channel` varchar(32); `seller_sku` varchar(64)?; `status` varchar(32); `created_at`; `updated_at`; `return_policy_choice` varchar(32) default `Default`; `custom_return_window_days` int?; `minimum_order_quantity` numeric(18,6)?; `maximum_order_quantity` numeric(18,6)?.

Opaque Guid refs to Catalog/Party (no cross-schema FK).

## `pricing.prices`

`price_id`; `offer_id`; `market`; `channel`; `currency`; `amount` numeric(19,4); `valid_from`; `valid_to`?; `status`; `qualifier_kind`; `qualifier_key`?; timestamps.

Index `(offer_id, market, channel, currency, qualifier_kind, valid_from)`. No rrp/compare_at column.

## `inventory.stock_positions` / `reservations` / `locations`

Stock keyed by `offer_id` + `location_id`. Available quantity is domain-derived (`OnHand - Reserved`), not a stored column.

## `promotion.promotions`

Migration `20260823210000_InitialPromotion` creates schema `promotion` table `promotions` matching `PromotionDefinition` 1:1 plus `promotion.outbox_messages`. Indexes: `coupon_code`; `(status, effective_from)`.

## Catalog (selected)

`products`, `variants`, `brands`, `categories`, `tags`, `localized_fields`, `store_landing_pages` (unique locale+slug), `store_landing_page_sections` (`configuration_json`).

## Code ↔ physical mismatch

None material for this audit. Gap is **absence of merchandising campaign / membership tables**, not entity–migration drift.
