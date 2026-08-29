# TB-P07-T027 evidence

## Scope
Category ↔ Product Assignment at Scale — Primary + Additional categories, AppDataGrid picker with bulk add/remove, Product Workspace Additional chips, PLP inclusion of Additional, schema isolation to Primary.

## Model
- `CatalogProductCategory.Role`: `Primary | Additional`
- Migration `20260830010000_AddProductCategoryAssignmentRole` (backfill; multi-row → oldest Primary, rest Additional)
- Filtered unique index: exactly one Primary per product when present
- Host: `POST/DELETE /v1/admin/products/{id}/categories/additional`
- Workspace view exposes `categoryAssignments` + `primaryCategoryId`

## Frontend
- Category Products: AppDataGrid assign dialog (`همه محصولات` / `انتخاب‌شده‌ها`), rowSelection + bulk add/remove Additional
- Primary badge vs Additional; Primary remove blocked (change-primary flow)
- Product Workspace: Additional chips add/remove
- List rows include `primaryCategoryId` for role detection
- Errors via centralized `mapAdminErrorMessage` (`catalog.category.assignment.*`)

## Schema isolation
- Effective attribute schema / variant-axis eligibility / definition allow-list use **Primary only**
- Additional is discovery/PLP only

## PLP
- Category PLP membership joins `product_categories` for **any** role in subtree; card identity CategoryId remains Primary

## Live verification (2026-08-30)
- Migration present in `catalog.__ef_migrations_history`
- ADD additional → 200 (Primary+Additional)
- Second Additional → 200
- Duplicate Additional → 400 + `catalog.category.assignment.duplicate`
- DELETE Primary via additional endpoint → 400 + `catalog.category.assignment.cannot_remove_primary`
- DELETE Additional → 200
- FE `/fa/admin/products`, `/en/admin/products`, `/fa/admin/catalog/categories` → 200
- Host health 200; Shopeiva `:3001` 200

## Validation
- Backend Host.Tests: 342 passed, 0 failed, 0 skipped
- Frontend: typecheck, lint (no new warnings on T027 files), test:admin, test:category-tree, test:product-workspace, production build — OK

## USER_VISUAL_ACCEPTED
NO
