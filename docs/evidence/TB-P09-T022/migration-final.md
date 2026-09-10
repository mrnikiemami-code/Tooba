# Migration final — TB-P09-T022

Additive migration: `20260910120000_AddConsolidatedPackages`

- `fulfillment.consolidated_packages`
- `fulfillment.consolidated_package_members`
- Unique `package_number` (`ix_consolidated_packages_package_number`)
- Filtered unique index **`ix_consolidated_package_members_shipment_active`** on `shipment_id WHERE released_at IS NULL` — present on live `tooba_alpha`
- Replay-safe `IF NOT EXISTS` patterns
- No destructive reset; fresh DB and existing `tooba_alpha` both apply to HEAD
