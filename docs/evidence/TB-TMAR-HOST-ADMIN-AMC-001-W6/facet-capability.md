# Facet capability — W6

## Application structure

```
Tooba.Catalog.Application/Facets/
  Commands/   UpsertCategoryFacet*, RemoveCategoryFacetOverride*, ReorderCategoryFacets*
  Queries/    GetEffectiveCategoryFacets*, ListLocalCategoryFacets*, GetStorefrontCategoryFacets*
  Models/     FacetSharedModelNote, ReorderCategoryFacetsWriteModel
  Ports/      IFacetDirectory
  Validators/ UpsertCategoryFacetCommandValidator, ReorderCategoryFacetsCommandValidator
```

- No `*Contracts.cs` bundle under Facets
- No one-folder-per-request
- Path ↔ namespace exact
- Shared DTOs (`CategoryFacetConfigurationInput/View`, `EffectiveCategoryFacet`) remain Application-root for ICatalogDirectory parity (same MegaMenu/Tags pattern)

## Persistence

- `IFacetDirectory` (Application port)
- `FacetDirectory` (Infrastructure) — Result + CatalogErrorCodes; no PlatformHttpException / IOE expected flow
- `CatalogDirectory` retains thin one-way wrappers → FacetDirectory (CatalogDemo / legacy tests)

## Endpoints

- `Endpoints/Admin/Facets/CatalogFacetAdminEndpoints.cs` — 5 routes, `ICatalogAdminAuthorizer` + `ISender` + `ApiResponseFactory`
- `Endpoints/Storefront/Facets/CatalogFacetStorefrontEndpoints.cs` — 1 route, `ISender` + `ApiResponseFactory`, no Admin auth
- Registered once via `CatalogEndpointModule`
- Host `CatalogFacetEndpoints.cs` deleted; Program map removed

## Domain

- `CatalogCategoryFacetRules` remains authoritative
- `ValidateDisplayType` returns `CatalogFacetDisplayTypeViolation` (typed; no Persian message throw)
- `CatalogCategoryFacetResolver` inheritance/filterable eligibility unchanged
