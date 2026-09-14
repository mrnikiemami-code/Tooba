# TB-P10-T005-R1 — Migration runtime

Applied through Host startup `Database.MigrateAsync` (`ProductWorkspaceDevelopmentBootstrap.MigrateSchemaOnlyAsync`). No ad-hoc CREATE TABLE outside the EF migration.

Host log (startup):

- `Applying migration '20260914010000_AddStoreAppearanceSettings'`
- `CREATE TABLE IF NOT EXISTS catalog.store_appearance_settings …`
- `INSERT INTO catalog.__ef_migrations_history … 20260914010000_AddStoreAppearanceSettings`

Live `tooba_alpha` after recycle:

| Check | Result |
| --- | --- |
| `catalog.__ef_migrations_history` | `20260914010000_AddStoreAppearanceSettings` |
| `to_regclass('catalog.store_appearance_settings')` | `catalog.store_appearance_settings` |
| row count | 0 (missing row → projector default) |
| `GET /v1/storefront/appearance` | 200 `paletteKey=tooba-blue` `storeScope=tenant:store-alpha` |

Idempotent: later startup logs `No migrations were applied. The database is already up to date.`
