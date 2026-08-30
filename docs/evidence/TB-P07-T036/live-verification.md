# Live verification (TB-P07-T036)

## Runtime
- Host `:5088` rebuilt and health=200 after Primary migration + Brand PLP changes.
- Admin FE `:3000` — `/fa/admin/products` → 200 (after fixing duplicate `newCategoryId` in preview parser).
- Shopeiva `:3001` → 200.

## Coverage notes
- Category Products display-only + wizard wiring verified via source tests and live compile.
- Host focused suite (PrimaryCategoryMigration + StorefrontComposition + CatalogAttributeSchema + ProductCategoryAssignment): **29 passed, 0 failed, 0 skipped**.
- Full interactive wizard/preview against demo data deferred to operator visual pass (`USER_VISUAL_ACCEPTED=NO`).

## Seed restore
No destructive Primary migration applied against shared demo seed in this verification pass (preview/migrate covered by Host tests with isolated fixtures).
