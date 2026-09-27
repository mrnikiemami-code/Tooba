# Partial Host retention — W8

## File state

`Admin/CatalogAttributeEndpoints.cs` = **RETAINED_PARTIAL_FOR_LATER_WAVES**

Host/Admin recursive production `*.cs`: **53 → 53** (file not deleted).

## Removed from Host

- MapGroup `/v1/admin/catalog/attribute-definitions` and 7 route mappings
- List/Get/Create/Update/Preview/Set/AddOption endpoint methods
- CreateAttributeDefinitionRequest / UpdateAttributeDefinitionRequest / SetVariantAxisCapabilityRequest / AddAttributeOptionRequest

Definition Host route count: **7 → 0**

## Retained in Host

- category `attribute-schema` routes (5)
- product attributes / readiness / variant-axes / variants editor-preview-apply-readiness (9)
- category-change-preview / primary-category (2)
- MapAttributeInvalid / MapCategoryChangeInvalid / ToError / EnrichVariantEditorWithOfferCountsAsync
- Program `MapCatalogAttributeEndpoints()`

## Ownership after W8

| Surface | Owner |
|---|---|
| 7 Definition Admin routes | Catalog.Endpoints exactly once |
| Remaining Attribute groups | Host until later waves |
