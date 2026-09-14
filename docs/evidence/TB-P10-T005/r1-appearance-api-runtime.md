# TB-P10-T005-R1 — Appearance API runtime

URL: `http://127.0.0.1:5088/v1/storefront/appearance`

| Request | Status | Shape |
| --- | --- | --- |
| default / `Host: alpha.localhost` | 200 | `storeScope=tenant:store-alpha`, `paletteKey=tooba-blue`, `paletteKeyWasKnown=true`, `themeMode=Light`, tokens `37 99 235` / `29 78 216` / `255 255 255` / `37 99 235`, `updatedAt=1970-01-01` (no row) |
| `Host: disabled.localhost` | 404 | `platform.resolution.failed` — no alpha appearance leak |
| missing row | 200 | default curated palette (known) |
| unknown key | Host tests 4/4 + FE palette-registry — resolve to `tooba-blue` |

Cache: in-memory 5 minutes, key `store-appearance:{scope}`. Isolation: `StoreAppearanceFoundationTests` 4/4; disabled host does not return store-alpha body.

No raw 500. Store/domain scoped via commerce context.
