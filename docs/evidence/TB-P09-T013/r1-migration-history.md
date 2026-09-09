# R1 migration history

Offer assembly: `InitialOffer` → `AddOfferReturnPolicy` (T006) → `AddOfferQuantityLimits` (T013).

`tooba_alpha.offer.__ef_migrations_history` before repair:

- `20260823082919_InitialOffer`
- `20260909130100_AddOfferQuantityLimits` (T013 raw SQL + history insert)

Missing: `20260907120000_AddOfferReturnPolicy`.

Live `offer.offers` already had T006 columns matching that migration exactly:

- `return_policy_choice varchar(32) NOT NULL DEFAULT 'Default'`
- `custom_return_window_days integer NULL`

plus one T013 pair:

- `minimum_order_quantity numeric(18,6) NULL`
- `maximum_order_quantity numeric(18,6) NULL`

Replay of T006 `AddColumn` would collide. Same T006-era gap on fulfillment: `AddShipmentProviderMetadata` missing from history while `shipments` already had those columns.

No new quantity subsystem. Snapshot updated so future `ef migrations add` does not re-add Offer policy/min-max.
