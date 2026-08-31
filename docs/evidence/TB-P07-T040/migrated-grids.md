# Migrated grids (TB-P07-T040)

All migrated surfaces use canonical `AppDataGrid` via `LegacyAppDataGrid` + `buildLegacyGridBridge` (client-side `executeGridQuery` preserved).

- Orders, Fulfillments, Returns, Sellers, Customers, Settlement (GridPage)
- Reviews, Promotions, Payout queue, Content articles
- Stories admin list
- Catalog attribute definitions + category effective schema
- Gift cards list

Saved view keys added under `saved-view-store.ts` (`grid.admin.*`).
