# Publish readiness capability — W16

## Ownership

Catalog owns Admin `GET /v1/admin/products/{productId}/publish/readiness`.

## Application structure

```
ProductPublishing/
  Queries/GetProductPublishReadinessQuery.cs
  Queries/GetProductPublishReadinessHandler.cs
  Models/ProductPublishReadinessModels.cs
  Ports/IProductPublishReadinessReader.cs
  Validators/ (.gitkeep; NO_VALIDATOR_REQUIRED)
```

## Focused read seam

- `IProductPublishReadinessReader` / `ProductPublishReadinessReader`
- Composes lawful focused ports:
  - `IProductSeoDirectory.GetAsync` (identity + SEO readiness)
  - `IProductAttributeDirectory.GetReadinessAsync`
  - `IProductVariantDirectory.GetReadinessAsync`
  - `IProductMediaDirectory.GetReadinessAsync`
  - Category assignability via `CatalogCategoryTreeRules.EnsureAssignableProductCategory`
- Does not duplicate attribute/variant/media/SEO business logic
- `ProductPublishRules` remains Domain authority for messages/summaries

## Legacy wrapper

`ICatalogDirectory.GetProductPublishReadinessAsync` delegates one-way to focused reader and unwraps `workspace.product.missing` to prior IOE for aggregate / PublishProductAsync / ProductPublishTests.
