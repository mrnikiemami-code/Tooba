# Migration

Additive migration: `20260910120000_AddConsolidatedPackages`

- `fulfillment.consolidated_packages`
- `fulfillment.consolidated_package_members`
- Unique `package_number`
- Filtered unique index on `shipment_id WHERE released_at IS NULL`

Replay-safe `CREATE TABLE IF NOT EXISTS` / `CREATE … INDEX IF NOT EXISTS`.
