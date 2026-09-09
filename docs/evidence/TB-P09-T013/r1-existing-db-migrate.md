# R1 existing DB migrate

Proven schema match, then replay-safe `ADD COLUMN IF NOT EXISTS` on:

- `offer.AddOfferReturnPolicy`
- `fulfillment.AddShipmentProviderMetadata` (sibling T006 gap blocking HEAD apply)

`MigrationRunner apply --tenant store-alpha` on live `tooba_alpha`: pending 0, no wipe.

After apply, history includes the omitted rows. Columns unchanged (IF NOT EXISTS no-op). User data kept.

Focused gap test: InitialOffer only + manual T006 columns + missing history → `Migrate()` to HEAD without collision.
