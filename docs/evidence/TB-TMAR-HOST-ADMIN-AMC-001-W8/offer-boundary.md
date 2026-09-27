# Offer boundary — W8

## Definition seven routes

**ZERO Offer dependency.**

Preview/set variant-axis capability impact uses only Catalog persistence:

- CategoryAttributeBindings
- ProductCategories (primary)
- ProductVariantAxes
- Localized category names

No `Offer.Contracts` / Application / Infrastructure / Domain on:

- AttributeDefinitionDirectory
- Attribute Definition CQRS
- CatalogAttributeDefinitionAdminEndpoints

## Retained Host file

`CatalogAttributeEndpoints.cs` still references `IOfferLookupGateway` for **retained** product variant editor/preview/apply enrichment (`EnrichVariantEditorWithOfferCountsAsync`) — later waves. Not part of W8 Definition surface.
