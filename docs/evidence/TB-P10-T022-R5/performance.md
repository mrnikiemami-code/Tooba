# Performance — TB-P10-T022-R5

## Indexes

- `store_templates.key` unique
- `template_categories(template_id)` + `(template_id, parent_category_id, sort_order)`
- `template_brands(template_id)`
- `template_products(template_id)`
- `template_store_landing_pages(template_id)` + unique `(template_id, locale, slug)`
- Child uniqueness mirrors operational (product-category, media, translations, localized texts)

## Operational width

- No columns added to `products` / `categories` / `brands`

## Preview load

`FashionTemplatePreviewQuery` loads Fashion template once, then categories/products/brands/sections with joins scoped by `TemplateId` — no N+1 per card; no union with operational tables.
