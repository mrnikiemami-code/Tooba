# Exact Parity Matrix — TB-P10-T022-R6

| Operational | Template counterpart | Parity | Missing? | Notes |
|---|---|---|---|---|
| store N/A / StoreTemplate root | store_templates | Yes | No | Template ownership root |
| products | template_products (+TemplateId) | Yes | No | |
| localized_texts (Product/Brand/Tag/Attr*) | template_localized_texts | Yes | No | OwnerKind extended |
| product_media_references | template_product_media_references | Yes | No | |
| product_categories | template_product_categories | Yes | No | |
| variants | template_variants | Yes | No | |
| product_variant_axes | template_product_variant_axes | Yes | No | R6 |
| product_attribute_values | template_product_attribute_values | Yes | No | R6 |
| variant_attribute_values | template_variant_attribute_values | Yes | No | R6 |
| product_tag_assignments | template_product_tag_assignments | Yes | No | R6 |
| product_history_entries | template_product_history_entries | Yes | No | R6 |
| tags | template_tags (+TemplateId) | Yes | No | R6 template-scoped |
| attribute_definitions | template_attribute_definitions (+TemplateId) | Yes | No | R6 template-scoped |
| attribute_options | template_attribute_options | Yes | No | R6 |
| categories | template_categories (+TemplateId) | Yes | No | |
| category_translations | template_category_translations | Yes | No | |
| category_slug_histories | template_category_slug_histories | Yes | No | R6 |
| category_tag_assignments | template_category_tag_assignments | Yes | No | R6 |
| category_attribute_bindings | template_category_attribute_bindings | Yes | No | R6 |
| category_facet_configurations | template_category_facet_configurations | Yes | No | R6 |
| mega_menu_items | template_mega_menu_items | Yes | No | R6 |
| mega_menu_item_translations | template_mega_menu_item_translations | Yes | No | R6 |
| brands | template_brands (+TemplateId) | Yes | No | |
| store_landing_pages/sections (banner JSON) | template_store_landing_* | Yes | No | Banner family via sections |

## Exclusions (justified)

| Item | Reason |
|---|---|
| Domain events / outbox | Runtime infrastructure, not cloneable Catalog structure |
| units_of_measure | Shared quantity foundation; TemplateProduct references same CanonicalUnits.Pcs |
| store_* settings / menus / appearance | Outside Product/Category/Brand Catalog families |
| CatalogLocalizedText OwnerKind=Category duplicate | Operational also uses category_translations as SoT; Template uses dedicated translations table identically |

No deferred Product/Category/Brand structural mirrors remain.
