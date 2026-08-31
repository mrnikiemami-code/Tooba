# Query contracts (TB-P07-T040)

- **Products** — existing server `POST /v1/admin/products/query` unchanged (canonical)
- **Migrated legacy admin lists** — retain existing list loaders + client-side `executeGridQuery` via LegacyAppDataGrid (no new Host GridQuery endpoints required for this gate)
- **Gift cards** — server list fetch preserved (`loadAdminGiftCards`); grid handles sort/filter/page on loaded page batch

Future server-side GridQuery endpoints may replace client-side mode per module without changing AppDataGrid surface.
