# Tags foundation — TB-P07-T032

## Domain
- `CatalogTag` + Product/Category M2M assignments (no CSV).
- Migration `20260830090000_AddCatalogTagFoundation`
- Tables: `catalog.tags`, `catalog.product_tag_assignments`, `catalog.category_tag_assignments`

## API
- `/v1/admin/catalog/tags` list/create/get
- Product/Category assign + remove nested routes

## UX
- Reusable `admin-tags-panel` / `catalog-tags-card` on Product + Category General.
- Searchable multi-select, create, removable chips, duplicate prevention.
- Helper: برچسب‌ها برای گروه‌بندی، جستجو و نمایش هدفمند…
- NOT meta keywords; no `<meta name="keywords">` strategy.

## Tests
- Host `CatalogTagFoundationTests`
- FE `catalog-tag-api.test.ts`
