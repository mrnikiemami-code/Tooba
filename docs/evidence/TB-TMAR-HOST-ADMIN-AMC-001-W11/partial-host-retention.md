# Partial Host retention — W11

## Host/Admin count

53 → 53

## CatalogAttributeEndpoints.cs after W11

State: **RETAINED_CATEGORY_CHANGE_ONLY**

Contains only:

- POST `/category-change-preview`
- PUT `/primary-category`
- `CategoryChangeRequest` / `CategoryChangePreviewRequest`
- `MapCategoryChangeInvalid` / `ToError`
- `CatalogActorHttpBinding` on products group

Must not / does not contain:

- five variant route mappings or methods
- variant transport records
- Offer enrichment helper / Offer.Contracts usings
- `SetProductVariantAxesRequest`

Program still maps `MapCatalogAttributeEndpoints()` until W12.

Variant Host route count: 5 → 0.
