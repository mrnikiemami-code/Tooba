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
- `MapAttributeInvalid` (dead helper; W8/W9 guards)
- `SetProductAttributeRequest` — still consumed by Host **Seller** `SellerPanelEndpoints`
- Offer enrichment for retained variant editor/preview/apply
- Program `MapCatalogAttributeEndpoints()`

## Not started

W11 / next Host folder / Offer enrichment migration / StoreAppearance.
