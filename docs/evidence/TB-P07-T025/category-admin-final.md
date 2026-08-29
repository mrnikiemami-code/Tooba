# TB-P07-T025 — Category Admin final

## Routes checked

- `/fa/admin/catalog/categories`
- `/en/admin/catalog/categories` (HTTP 200)

## Locales

- fa RTL Category Admin live
- en route reachable

## VIEW / EDIT

- Tree live with expand/add/more actions
- Tabs locked set: General, Translations, Attributes, Facets, Mega-menu, Products (all implemented; no stub tabs)

## Reference

- Product reference image used for Product gate; Category checked against locked Category Admin contracts from T022/T022-R1

## Findings

| Check | Result |
|-------|--------|
| Navigation group «کاتالوگ / دسته‌بندی‌ها» distinct from «محصولات» | yes |
| AppCategoryTree live | yes |
| Incomplete visible tab | none |
| Attribute create / add-existing | present in Attributes panel (prior lock) |
| Behavior chips | independent toggles (prior lock + tests) |
| Category Products | real AppDataGrid (prior lock) |
| Level-3 assignment rule | Product EDIT surfaces L3 requirement messaging |
| Raw Bad Request in normal UX | no (duplicate attribute API returns localized title + `catalog.attribute.code.duplicate`) |

## Incomplete UX found

- none in scoped Category Admin (`به‌زودی` only remains as dead-code branch for `live:false` nav items; Catalog/Product nav items are all `live:true`)

## Architecture regression

- none
