# Final Category Admin — TB-P07-T031

Runtime: Host `http://127.0.0.1:5088`, FE `http://127.0.0.1:3000`, Shopeiva `:3001`.

## Live HTTP
- `/fa/admin/catalog/categories` → 200
- `/fa/admin/catalog/attributes` → 200
- Category workspace page for live L3 id → 200

## Tree
- `GET /v1/admin/catalog/categories/tree?locale=fa-IR` → 200 (multi-root tree)
- Search/expand/collapse remain on approved Category Admin UI (no regression from accepted P07 baseline)
- Technical Schema route stays deferred (`ADMIN_DEFERRED_NAV_HREFS`), not a live nav leaf

## General / Media
- Workspace returns translations + media asset ids without exposing raw GUID as primary UX in FE panels
- Image / icon / banner use real Media Library assignment flows (accepted Category Admin baseline; no `به‌زودی` in category panels)

## Translations
- fa-IR / en supported via locale-based translations (no NameFa/NameEn scalars)

## Attributes
- Attribute library: `GET /v1/admin/catalog/attribute-definitions` → 11 definitions live
- Category effective schema sample (`01a043f3-…`): **4** bindings (`live-schema-sample.json`)
- UI retains «افزودن ویژگی موجود» / «ایجاد ویژگی جدید» with inherited vs category-specific behavior chips

## Filters
- Local facets found on category `01a030d1-3fc4-7000-a617-966cf6571799` → **2** (`live-facets-local.json`)
- Effective facets endpoint live; panel persists display/search/collapse/order (accepted Filters baseline)

## Products picker
- Category Products panel uses canonical `AppDataGrid` (All / Selected, server paging/search, bulk add/remove, Primary vs Additional badges) — covered by unit gates + live category page 200

## MegaMenu
- `GET .../mega-menu?locale=fa-IR` → 200 on sampled L3 categories
- Panel remains functional (no stub CTA)

## Incomplete / technical UX
- No live Category nav item labeled `به‌زودی` for scoped Catalog Admin leaves
- Category-schema remains deferred only
