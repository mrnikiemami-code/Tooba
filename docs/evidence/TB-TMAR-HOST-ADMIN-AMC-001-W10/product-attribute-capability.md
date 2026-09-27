# Product-attribute capability — W10

## Structure

```
Application/Attributes/ProductValues/
  Commands/   SetProductAttributes*, SetProductAttribute*
  Queries/    GetProductAttributeEditorState*, GetProductAttributeReadiness*
  Models/     ProductAttributeWriteModels.cs
  Ports/      IProductAttributeDirectory.cs
  Validators/ SetProductAttributesCommandValidator.cs

Infrastructure/ProductAttributeDirectory.cs
Endpoints/Admin/Attributes/ProductValues/CatalogProductAttributeAdminEndpoints.cs
Endpoints/Admin/CatalogActorRequestBinding.cs
```

## Routes (4)

- GET `/v1/admin/catalog/products/{productId}/attributes`
- PUT `/v1/admin/catalog/products/{productId}/attributes`
- GET `/v1/admin/catalog/products/{productId}/attributes/readiness`
- PUT `/v1/admin/catalog/products/{productId}/attributes/{definitionId}`

## Pipeline

HTTP → `ICatalogAdminAuthorizer` → `CatalogActorRequestBinding` → `ISender` → Handler → `IProductAttributeDirectory` → `Result` → `ApiResponseFactory`.

## Focused port

`IProductAttributeDirectory` / `ProductAttributeDirectory` owns editor/readiness/single/bulk. CatalogDirectory keeps thin Unwrap wrappers for CatalogDemo/tests.
