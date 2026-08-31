# Live verification (TB-P07-T038)

Host `:5088` proof via `live-proof.mjs`:

- health OK
- `POST /v1/admin/products/query` returns `primaryCategoryName`, `additionalCategoryNames`, `additionalCategoryCount`
- no ` > ` path separators in primary/additional/summary on sampled page (50 rows)
- primary leaf samples present; additional arrays present

Chip `+N` / max-3 UI covered by frontend contract tests (`t038-category-grid-ux.test.ts`, `additional-category-chips-cell.tsx`). Current seeded page sample did not include a row with >3 additional memberships; overflow behavior is payload-driven with no network.

Tree selection: route-synced `selectedKeys`, ancestor expand, scrollIntoView — contract tests green.
