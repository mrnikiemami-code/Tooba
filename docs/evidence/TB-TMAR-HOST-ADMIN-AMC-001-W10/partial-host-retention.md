# Partial Host retention — W10

## File

`Admin/CatalogAttributeEndpoints.cs` **RETAINED_PARTIAL_VARIANT_CATEGORY_CHANGE_ONLY**

Host/Admin count: **53 → 53**

## Removed from Host (W10)

- Four product-attribute route mappings
- Four product-attribute endpoint methods
- `SetProductAttributesRequest` / `ProductAttributeValueRequest` (Admin-only bulk transport)

## Retained in Host

- `/variant-axes`
- `/variants/editor|preview|apply|readiness`
- `/category-change-preview`
- `/primary-category`
- `CatalogActorHttpBinding` on products group
- Variant/category-change routes + helpers (`MapCategoryChangeInvalid`, `ToError`, variant DTOs)
- ~~`MapAttributeInvalid` (dead helper; W8/W9 guards)~~ → **removed in W10-R1**
- ~~`SetProductAttributeRequest` — Seller consumer~~ → **relocated to Seller in W10-R1**

See `docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W10-R1/` for authoritative repair record.
- Offer enrichment for retained variant editor/preview/apply
- Program `MapCatalogAttributeEndpoints()`

## Not started

W11 / next Host folder / Offer enrichment migration / StoreAppearance.
