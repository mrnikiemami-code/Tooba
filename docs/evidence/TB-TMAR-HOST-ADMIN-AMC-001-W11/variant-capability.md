# Variant capability — W11

Catalog owns Variant Axes + Variant Matrix Admin as capability `Application/Variants/`.

## Structure

```
Application/Variants/{Commands,Queries,Models,Ports,Validators}
Infrastructure/ProductVariantDirectory.cs
Infrastructure/Adapters/VariantOfferLookupAdapter.cs
Endpoints/Admin/Variants/CatalogProductVariantAdminEndpoints.cs
```

## Routes (module-owned, exactly once)

| Method | Route |
|---|---|
| PUT | `/v1/admin/catalog/products/{productId}/variant-axes` |
| GET | `/v1/admin/catalog/products/{productId}/variants/editor` |
| POST | `/v1/admin/catalog/products/{productId}/variants/preview` |
| PUT | `/v1/admin/catalog/products/{productId}/variants/apply` |
| GET | `/v1/admin/catalog/products/{productId}/variants/readiness` |

## Ports

- `IProductVariantDirectory` / `ProductVariantDirectory` — focused persistence; MaxVariantCombinations=200; transaction on apply; EventVariantsChanged
- `IVariantOfferLookup` / `VariantOfferLookupAdapter` — Offer.Contracts only

## Legacy

`ICatalogDirectory` variant methods remain as thin Unwrap wrappers for CatalogDemo / Host Seller / tests.
