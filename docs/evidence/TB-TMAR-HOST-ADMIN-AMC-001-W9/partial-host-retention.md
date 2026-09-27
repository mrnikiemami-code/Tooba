# Partial Host retention — W9

`CatalogAttributeEndpoints.cs` remains after W9.

## Removed from Host

- Five attribute-schema route mappings
- Five schema endpoint methods
- Schema-only transport records: BindCategoryAttributeRequest, UpdateCategoryAttributeBindingRequest, ReorderCategoryBindingsRequest

## Retained in Host (later waves)

- Product attributes / readiness / per-definition value set
- Variant-axis assignment
- Variants editor / preview / apply / readiness
- Category-change-preview / primary-category
- MapAttributeInvalid / MapCategoryChangeInvalid / ToError
- Offer.Contracts enrichment for variant editor only
- Program `MapCatalogAttributeEndpoints()` while retained routes remain

## Metrics

| Metric | Value |
|---|---|
| Host/Admin `*.cs` | 53 → 53 |
| Schema Host routes | 5 → 0 |
| Attribute host file state | RETAINED_PARTIAL_PRODUCT_VARIANT_ONLY |
| W10 started | false |
