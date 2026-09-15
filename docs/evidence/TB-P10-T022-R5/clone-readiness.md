# Clone-Readiness (structural only) — TB-P10-T022-R5

Cloning is **not** implemented.

## Direct 1:1 mapping ready

| Template | Operational |
|---|---|
| TemplateProduct | CatalogProduct (minus TemplateId) |
| TemplateLocalizedText | CatalogLocalizedText |
| TemplateProductMediaReference | CatalogProductMediaReference |
| TemplateProductCategory | CatalogProductCategory |
| TemplateVariant | CatalogVariant |
| TemplateCategory | CatalogCategory |
| TemplateCategoryTranslation | CatalogCategoryTranslation |
| TemplateBrand | CatalogBrand |
| TemplateStoreLandingPage (+sections) | StoreLandingPage (+sections) |

## Non-1:1 / deferred

| Gap | Why |
|---|---|
| Attribute / facet / tag / history / mega-menu tables | Shared catalog infrastructure; Fashion seed products are attribute-free. Full attributed clone needs Template mirrors of those child tables later. |
| Offer / Pricing / Inventory | Not Catalog Product fields; clone of commercial readiness is outside Template Catalog. |
| Banner entity | Does not exist operationally; clone path is Landing section JSON → Landing section JSON. |
| MediaAsset binary | MediaAssetId is opaque; clone must copy/link Media module assets separately. |
| UnitOfMeasureId | References shared `units_of_measure` (CanonicalUnits.Pcs) — copy Guid as-is. |

No invented Template-only commercial fields were added.
