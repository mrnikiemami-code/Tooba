# ProductMedia capability — W13

## Capability

`Tooba.Catalog.Application/ProductMedia/` owns Admin product media editor/readiness and mutations.

## Structure

```
ProductMedia/
  Commands/   Attach, AttachPlaceholder, Reorder, SetPrimary, Patch, Detach (+ handlers)
  Queries/    GetProductMedia, GetProductMediaReadiness (+ handlers)
  Models/     write bodies + ProductMediaItemView (Primary JSON) + ProductMediaReadinessView
  Ports/      IProductMediaDirectory
  Validators/ Attach (MediaAssetId), Reorder (OrderedMediaAssetIds NotNull)
```

## Persistence

`IProductMediaDirectory` / `ProductMediaDirectory` — Result-typed, primary uniqueness, exact-set reorder, unassign-only detach, history with actor.

## HTTP

`CatalogProductMediaAdminEndpoints` under `/v1/admin/products/{productId}` — eight routes, ICatalogAdminAuthorizer + ISender + ApiResponseFactory. Workspace scope policy on writes. Actor bind on writes.

## Host residue

ProductWorkspace* files retained for non-media slices. Media route count Host 8→0.
