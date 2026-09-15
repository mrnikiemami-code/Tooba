# Schema Parity Audit — TB-P10-T022-R5

## Product family (`catalog`)

| Operational | Template mirror |
|---|---|
| `products` | `template_products` (+ `template_id`) |
| `localized_texts` (OwnerKind=Product) | `template_localized_texts` |
| `product_media_references` | `template_product_media_references` |
| `product_categories` (+ role) | `template_product_categories` |
| `variants` | `template_variants` (structural; Fashion seed attribute-free) |

Not mirrored in this task (shared infrastructure / audit-only; documented in clone-readiness):

- attribute_definitions / options / product_attribute_values / variant_attribute_values / product_variant_axes
- product_tag_assignments / product_history_entries

## Category family

| Operational | Template mirror |
|---|---|
| `categories` (+ ParentCategoryId, media ids) | `template_categories` (+ `template_id`) |
| `category_translations` | `template_category_translations` |

Deferred: slug history, category tags, attribute bindings, facets, mega menu.

## Brand family

| Operational | Template mirror |
|---|---|
| `brands` | `template_brands` (+ `template_id`) |
| `localized_texts` (OwnerKind=Brand) | `template_localized_texts` |

## Banner family

**No operational Banner entity exists.** Banners live in `store_landing_page_sections.configuration_json` (`BannerShowcase` / `PromoBanner`).

Mirror:

- `template_store_landing_pages` (+ `template_id`)
- `template_store_landing_page_sections` (BannerShowcase JSON with local `imageUrl`)

## StoreTemplate root

`catalog.store_templates` — `template_id`, unique `key`, `name`, `is_active`, timestamps.

Fashion key: `fashion`, name: `پوشاک`.
