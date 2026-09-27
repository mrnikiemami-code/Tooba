# Disposition map — W16 Product Workspace Publish Readiness GET (member-level)

Source Host files (RETAINED_PARTIAL after W16):
- `Admin/ProductWorkspaceEndpoints.cs`
- `Admin/ProductWorkspaceComposer.cs`
- `Admin/ProductWorkspaceModels.cs`

Host/Admin production `*.cs` count: **52 → 52**.
Publish-readiness Host route count: **1 → 0**.

## Endpoint route registrations (Host)

| Member | Classification | Destination |
|---|---|---|
| `MapGet("/{productId:guid}/publish/readiness", GetPublishReadinessAsync)` | MOVE_W16 | Catalog.Endpoints ProductPublishing |
| `GetPublishReadinessAsync` method | DELETE from Host | Catalog endpoints |
| All other ProductWorkspace route maps | RETAIN | Host (later waves) |
| `ReadPermissions` / `X-Tooba-Workspace-Scope` | RETAIN for remaining Host routes; readiness GET leaves Host | GET: no edit-scope check; Admin auth via ICatalogAdminAuthorizer |
| `AdminPanelAccess.RequireAuthorizedAsync` on readiness | REPLACE | `ICatalogAdminAuthorizer` |
| `CatalogActorHttpBinding` group filter | RETAIN on Host group; readiness route leaves Host | no actor bind on readiness GET |
| `ToError` / PlatformHttpException catch on readiness | DELETE from readiness surface | `ApiResponseFactory` |

Exact route preserved: `GET /v1/admin/products/{productId:guid}/publish/readiness` with optional `locale`.

## Composer members

| Member | Classification | Destination |
|---|---|---|
| `GetPublishReadinessAsync` public | MOVE_W16 then DELETE Host public | `GetProductPublishReadinessQuery` + reader |
| `MapPublishReadiness` | RETAIN | Host Composer (aggregate GetAsync) |
| Aggregate `GetAsync` publication/readiness | RETAIN | Host Composer via `ICatalogDirectory.GetProductPublishReadinessAsync` |
| Lifecycle Publish/Unpublish/Archive/Restore | RETAIN | Host (not W16) |

## Models

| Type | Classification | Destination |
|---|---|---|
| `ProductPublishReadinessView` | RETAIN Host | Still used by `ProductPublicationView.AggregateReadiness` |
| `ProductPublishMissingRequirementView` | RETAIN Host | Aggregate missing checklist |
| `ProductPublicationView` | RETAIN | Host aggregate |

HTTP Admin response shape moves to Catalog Application Models (parity with Host view fields).

## Catalog persistence / contracts

| Member | Classification | Destination |
|---|---|---|
| `ICatalogDirectory.GetProductPublishReadinessAsync` | EXTRACT → focused reader; thin unwrap wrapper retained | `IProductPublishReadinessReader.GetAsync` |
| `CatalogDirectory.GetProductPublishReadinessAsync` | DELEGATE one-way to focused reader; unwrap IOE for legacy | thin wrapper |
| `IsProductPrimaryCategoryAssignableAsync` | MOVE logic into focused reader (or shared private); CatalogDirectory may keep private for other callers if needed | ProductPublishReadinessReader |
| Focused ProductSeo / ProductMedia / ProductVariants / ProductAttributes readiness | REUSE | Inject ports into reader; do not duplicate |
| `ProductPublishReadiness` / `ProductPublishMissingRequirement` | RETAIN Application CatalogContracts | Live callers + reader return |
| `ProductPublishRules` | PRESERVE Domain | All Message*Fa / SummarizeMissingFa |
| `ProductSeoRules.NormalizeLocale` | PRESERVE Domain | Locale normalization |

### Live thin-wrapper callers of `GetProductPublishReadinessAsync` (document)

- Host `ProductWorkspaceComposer` aggregate GetAsync
- Host.Tests `ProductPublishTests`
- CatalogDirectory `PublishProductAsync` (lifecycle gate — not W16)
- CatalogDemo seed (if still live)

## Domain / rules (PRESERVE)

| Concern | Classification |
|---|---|
| Locale blank/null → ProductSeoRules.NormalizeLocale → fa-IR | PRESERVE |
| Missing order category/identity/attributes/variants/media/seo | PRESERVE |
| Attribute missing-code suffix | PRESERVE |
| Media/SEO MessageFa fallback to ProductPublishRules | PRESERVE |
| isReady = missing.Count == 0 | PRESERVE |
| MessageReadyFa / SummarizeMissingFa | PRESERVE |

## Error / result

| Code | HTTP | W16 treatment |
|---|---|---|
| `workspace.product.missing` | 404 | Result + CatalogErrorCodes on new HTTP/read seam |
| `workspace.permission.denied` | 403 | Not applied on GET readiness (CanView always true from ReadPermissions; no edit-scope) |

No PlatformHttpException / expected InvalidOperationException / message parsing on moved readiness HTTP/read seam.
Legacy `ICatalogDirectory.GetProductPublishReadinessAsync` may still throw IOE for old callers via unwrap OUTSIDE new HTTP seam.

## Validation matrix

| Request | Classification |
|---|---|
| `GetProductPublishReadinessQuery` | NO_VALIDATOR_REQUIRED |

## Final disposition

`READY_TO_MIGRATE`
