# Analyze — TB-TMAR-HOST-ADMIN-AMC-001-W12

## Structured state (pre-migrate)

| Field | Value |
|---|---|
| Foundation-State | FOUNDATION_READY (Catalog certified; extend `CategoryChanges/` beside Variants/Attributes) |
| Ownership-State | correct destination = Catalog; Host file MUST_DELETE after move |
| File-Cohesion-State | COHESIVE remaining Host file (only category-change) — evacuate then delete |
| Oversized/God-File-State | CatalogAttributeEndpoints.cs — 2 routes + helpers (~104 LOC) |
| Localization-State | EXCEPTION_MESSAGE_BASED — MapCategoryChangeInvalid compares Persian IOE message |
| API-Result-Pattern-State | RAW_RESULTS / AD_HOC (`Results.Json`, PlatformHttpException ToError) |
| Stable-Error-Code-State | STRING_HEURISTIC — assignment-level via message equality else `catalog.category_change.invalid` |
| Logging-State | CANONICAL |
| Sensitive-Logging-State | NONE |
| OpenTelemetry-State | CANONICAL |
| Correlation-Trace-State | CANONICAL |
| CQRS-State | MISSING on two category-change routes |
| Validator-Coverage-State | GAPS (none on Host HTTP) |
| Contracts-Boundary-State | CLEAN once Endpoints→Application only |
| Cross-Module-Coupling-State | NONE illegal after move (Catalog→Host ZERO) |
| Cross-Module-Join-State | NONE |
| Persistence-Ownership-State | CatalogDirectory owns logic; extract focused `ICategoryChangeDirectory` |
| Endpoint-Ownership-State | HOST_OWNED → MODULE_OWNED |
| Host-Residue-State | After W12: CatalogAttributeEndpoints ABSENT; Admin count 52 |
| Schema-Migration-State | UNCHANGED |
| Behavior-Preservation-Risk | LOW–MEDIUM (typed codes + ProblemDetails replace `{title,errorCode}` envelope; success JSON shape preserved) |
| Canonical-Reference-Used | W10/W11 CatalogActorRequestBinding; ApiResponseFactory; CatalogErrorCodes; Variants port pattern |
| Final-Disposition | READY_TO_MIGRATE |

## Target routes

1. POST `/v1/admin/catalog/products/{productId:guid}/category-change-preview`
2. PUT `/v1/admin/catalog/products/{productId:guid}/primary-category`

## Responsibility map

- DOMAIN_RULE: Level-3 assignability; schema preview orphans/invalid axes; lifecycle unpublish; history event names
- APPLICATION_USE_CASE: preview report; replace primary with transaction + history
- PERSISTENCE: Products, Categories, ProductCategories, ProductAttributeValues, ProductVariantAxes, Variants, AttributeDefinitions/Options, LocalizedTexts, ProductHistory
- HTTP_ENDPOINT: two Admin routes → Catalog.Endpoints CategoryChanges
- AUTHORIZATION_ADAPTER: ICatalogAdminAuthorizer
- ACTOR_BINDING: CatalogActorRequestBinding for EventCategoryChanged / EventUnpublished

## Ownership

Catalog owns category-change impact preview, primary-category replacement/migration, write/read models, focused persistence seam, stable errors, both HTTP routes. Host owns ZERO Catalog Attribute/category-change HTTP after W12.

## Illegal dependencies (current Host surface)

- Endpoints → ICatalogDirectory
- PlatformHttpException catch + Results.Json ToError
- InvalidOperationException catch + `ex.Message == ProductAssignableLevelRequiredMessageFa`
- Host CatalogActorHttpBinding group filter

## Exact target paths / namespaces

```
Tooba.Catalog.Application/CategoryChanges/Commands/...
Tooba.Catalog.Application/CategoryChanges/Queries/...
Tooba.Catalog.Application/CategoryChanges/Models/...
Tooba.Catalog.Application/CategoryChanges/Ports/ICategoryChangeDirectory.cs
Tooba.Catalog.Application/CategoryChanges/Validators/...
Tooba.Catalog.Endpoints/Admin/CategoryChanges/CatalogProductCategoryChangeAdminEndpoints.cs
Tooba.Catalog.Infrastructure/CategoryChangeDirectory.cs
```

## Migration order

1. Persist disposition map (this folder) — DONE before code
2. Add CatalogErrorCodes + catalog contributor + resx
3. Add ICategoryChangeDirectory + CategoryChangeDirectory (Result, typed assignability)
4. Thin-wrap CatalogDirectory methods
5. CQRS commands/queries/handlers/validators
6. Catalog.Endpoints map + CatalogEndpointModule
7. Remove Host Program mapping; DELETE CatalogAttributeEndpoints.cs
8. Update prior-wave guards; add HostAdminAmcW12GuardTests
9. SoT hostAdminAmcW12; focused builds/tests
10. Commit + push

## Verification plan

Focused builds Catalog.* + Host + Host.Tests; W7–W12 guards; prove Host file absent; both routes Catalog-owned once; no message classification; assignment-level code preserved; transaction/history/variant safety preserved.

## Certification blockers

None for W12 bounded slice if PASS criteria met. Residual: ProductWorkspace / Seller / StoreAppearance still Host-owned (out of scope).
