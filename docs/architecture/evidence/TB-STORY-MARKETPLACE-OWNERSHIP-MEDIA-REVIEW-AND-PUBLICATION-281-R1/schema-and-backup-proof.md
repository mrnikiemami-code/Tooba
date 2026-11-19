# Schema and backup proof (R1)

## Immutable parent migrations

- Media `20261118233932_Tb281SellerAssetOwnerScope` — not edited/reapplied
- Story `20261118233937_Tb281CreatedByAndOccurrences` — not edited/reapplied

## Additive R1 Media migration

- Name: `20261119000844_Tb281R1OwnerOccurrenceIntentHash`
- Changes: drop occurrence PK on `occurrence_key`; add required `intent_hash` (varchar 64); composite PK `(tenant_id, seller_party_id, occurrence_key)`
- Plan pending before apply: only this Media migration
- Apply: `Tooba.MigrationRunner apply --tenant store-alpha` once → Media pending `[]`

## Backups

See `RECEIPTS.md` for R1 pre-apply dump hash/TOC and parent dump NOW verification.
