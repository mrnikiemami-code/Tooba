# 11 — Category Seed Proof

Task: `TB-P05-T016`

Matrix (`StorefrontDemoCatalogMatrix`):

| Metric | Count |
| --- | ---: |
| L1 families | 8 |
| L2 children | 24 |
| L3 leaves | 72 |
| Demo products | 72 |

Bootstrap:

- Fresh seed creates + publishes L3 under each L2
- Products attach to L3 leaf categories
- `EnsureThirdLevelCategoriesAsync` backfills L3 on existing Development databases

Tests (`StorefrontDemoCatalogSeedTests`):

- `ThirdLevelCategoryCount >= 48` matrix threshold
- Postgres integration asserts `ThirdLevelCategories >= 48` after seed
- Idempotent second run preserves counts
