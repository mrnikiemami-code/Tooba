# 02 — Category Hierarchy Decision

Task: `TB-P05-T016`

## Existing Catalog capability

`CatalogCategory.ParentCategoryId` already supports arbitrary tree depth. No schema migration required.

## Decision

Extend **Development demo seed only**:

- `StorefrontDemoCatalogMatrix.ThirdLevelCategoryCount` = 72 (3 leaves × 24 L2)
- Fresh seed creates L3 under each L2 using product display names as leaf category names
- Products assign to L3 leaf categories (not L2)
- `EnsureThirdLevelCategoriesAsync` backfills L3 on already-seeded Development databases idempotently

## Non-goals

- No separate MegaMenu table
- No frontend-only JSON hierarchy
- No cross-module SQL
- No API/DTO shape change (`StorefrontCategoryItem` remains flat 3-field navigation payload)
