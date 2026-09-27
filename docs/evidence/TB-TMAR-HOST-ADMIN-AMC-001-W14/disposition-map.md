# Disposition map — W14 Product Workspace SEO Admin (member-level)

Source Host files (RETAINED_PARTIAL after W14):
- `Admin/ProductWorkspaceEndpoints.cs`
- `Admin/ProductWorkspaceComposer.cs`
- `Admin/ProductWorkspaceModels.cs`

Host/Admin production `*.cs` count: **52 → 52**.
SEO Host route count: **3 → 0**.

## Endpoint route registrations (Host)

| Member | Classification | Destination |
|---|---|---|
| `MapGet("/{productId}/seo", GetSeoAsync)` | MOVE_W14 | Catalog.Endpoints ProductSeo |
| `MapPut("/{productId}/seo", PutSeoAsync)` | MOVE_W14 | Catalog.Endpoints ProductSeo |
| `MapGet("/{productId}/seo/readiness", GetSeoReadinessAsync)` | MOVE_W14 | Catalog.Endpoints ProductSeo |
| All other ProductWorkspace route maps | RETAIN | Host (later waves) |
| `ReadPermissions` / `X-Tooba-Workspace-Scope` | MOVE_POLICY (SEO put only) | Catalog.Endpoints `CatalogWorkspaceScope` (generalize W13 media helper) |
| `AdminPanelAccess.RequireAuthorizedAsync` on 3 routes | REPLACE | `ICatalogAdminAuthorizer` |
| `CatalogActorHttpBinding` group filter | RETAIN on Host group; SEO routes leave Host | W14 PUT: `CatalogActorRequestBinding` |
| `ToError` / PlatformHttpException catch | DELETE from SEO surface | `ApiResponseFactory` |
| `GetSeoAsync` / `PutSeoAsync` / `GetSeoReadinessAsync` endpoint methods | DELETE from Host | Catalog endpoints |

Exact route base preserved: `/v1/admin/products/{productId:guid}` (NOT `/v1/admin/catalog/...`).

## Composer members

| Member | Classification | Destination |
|---|---|---|
| `GetSeoAsync` | MOVE_W14 then DELETE Host public | `GetProductSeoQuery` |
| `UpdateSeoAsync` | MOVE_W14 then DELETE Host public | `UpdateProductSeoCommand` |
| `GetSeoReadinessAsync` | MOVE_W14 then DELETE Host public | `GetProductSeoReadinessQuery` |
| `MapSeoDetail` | DELETE (SEO-route-only) | Application mapping in handlers |
| PlatformHttpException / message Contains classification on SEO | DELETE | Result + CatalogErrorCodes |
| Aggregate `ProductSeoView` mapping in `GetAsync` | RETAIN | Host Composer (workspace aggregate) |
| `EnsureCatalogEdit` / `CanView` for non-SEO | RETAIN | Host Composer |

## Models

| Type | Classification | Destination |
|---|---|---|
| `AdminProductSeoUpdateRequest` | MOVE_W14 then DELETE Host | Application `UpdateProductSeoWriteModel` |
| `ProductSeoDetailView` | MOVE shape to Catalog; DELETE Host | Application `ProductSeoDetailView` (route response) |
| `ProductSeoReadinessView` | MOVE shape to Catalog; DELETE Host | Application `ProductSeoReadinessView` (route + nested) |
| `ProductSeoView` | RETAIN Host | Still used by `ProductWorkspaceView.Seo` |

## ICatalogDirectory / CatalogDirectory SEO methods

| Method | Classification | Destination |
|---|---|---|
| `GetProductSeoAsync` | EXTRACT → focused port; thin UnwrapSeo wrapper retained | `IProductSeoDirectory.GetAsync` |
| `UpdateProductSeoAsync` | EXTRACT → focused port; thin UnwrapSeo wrapper retained | `IProductSeoDirectory.UpdateAsync` |
| `GetProductSeoReadinessAsync` | EXTRACT → focused port; thin UnwrapSeo wrapper retained | `IProductSeoDirectory.GetReadinessAsync` |
| `BuildProductSeoDetailAsync` | MOVE into focused directory | private |
| `ResolveProductNameForSeoAsync` | MOVE into focused directory; publish readiness uses SEO readiness/detail via wrappers | private |
| `ResolveLocalizedFieldForSeoAsync` | MOVE into focused directory | private |
| `UpsertProductLocalizedFieldForSeoAsync` | MOVE into focused directory | private |
| Application DTOs `ProductSeoUpdateInput` / `ProductSeoDetail` / `ProductSeoReadiness` | RETAIN in CatalogContracts.cs | Shared legacy models; wrappers return them |

Live thin-wrapper callers: `ProductSeoTests`, `ProductPublishPrep`, `GetProductPublishReadinessAsync`.

## Domain / rules (PRESERVE)

| Concern | Classification |
|---|---|
| `ProductSeoRules.NormalizeLocale` (default fa-IR) | PRESERVE Domain |
| `ProductSeoRules.Evaluate` readiness | PRESERVE |
| `ProductSeoRules.BuildPublicPath` | PRESERVE |
| `CatalogCategorySlugNormalizer.NormalizeSlug` / `SlugifyFromName` | PRESERVE |
| fa-IR `SeoTitleSeam` compatibility | PRESERVE |
| ExpectedUpdatedAt optimistic concurrency | PRESERVE |
| Global tenant slug uniqueness | PRESERVE |
| `ProductHistoryRules.EventSeoChanged` | PRESERVE with actor |
| Single SaveChanges atomicity | PRESERVE |
| Schema / migrations | UNCHANGED |

## Error / result (typed — preserve workspace.* codes)

| Code | HTTP | Source today |
|---|---|---|
| `workspace.permission.denied` | 403 | Host CanView/CanEditCatalog |
| `workspace.product.missing` | 404 | IOE product missing / Contains Tenant\|محصول |
| `workspace.catalog.stale` | 409 | exact message `workspace.catalog.stale` |
| `workspace.product.slug.duplicate` | 409 | Contains `قبلاً استفاده` |
| `workspace.product.slug.invalid` | 400 | Contains `نامعتبر` |
| `workspace.product.seo.rejected` | 400 | generic catch |

W14: register in CatalogErrorCodes + contributor + resx; ProductSeoDirectory returns SemanticError — no message classification on migrated surface.

## Workspace scope

Generalize W13 `CatalogWorkspaceMediaScope` → shared `CatalogWorkspaceScope` (same view-deny semantics). Media helper delegates so W13 guards remain valid. SEO PUT uses `CatalogWorkspaceScope.AllowsCatalogEdit`.

## Validation matrix

| Request | Classification |
|---|---|
| `GetProductSeoQuery` | NO_VALIDATOR_REQUIRED (route Guid + optional locale) |
| `GetProductSeoReadinessQuery` | NO_VALIDATOR_REQUIRED |
| `UpdateProductSeoCommand` | VALIDATOR_REQUIRED — Locale NotNull transport; do NOT duplicate slug/uniqueness/concurrency/readiness |

## Final disposition

`READY_TO_MIGRATE`
