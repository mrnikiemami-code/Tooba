# Analyze — W14 Product SEO Admin slice

## Structured state (pre-migrate)

| Field | State |
|---|---|
| Foundation-State | FOUNDATION_READY (Catalog certified; add ProductSeo capability folders) |
| Ownership-State | MUST_SPLIT (SEO vs remaining ProductWorkspace) |
| File-Cohesion-State | MULTI_RESPONSIBILITY_COHESION_VIOLATION on ProductWorkspace* (SEO members only in scope) |
| Localization-State | EXCEPTION_MESSAGE_BASED on Host SEO (IOE Message Contains → PlatformHttpException codes) |
| API-Result-Pattern-State | AD_HOC (Results.Json + PlatformHttpException) |
| Stable-Error-Code-State | STRING_HEURISTIC (exact stale + Contains قبلاً استفاده / نامعتبر / Tenant|محصول) |
| CQRS-State | MISSING on SEO HTTP |
| Validator-Coverage-State | GAPS (no FluentValidation on Host SEO) |
| Contracts-Boundary-State | CLEAN target (Application models/ports) |
| Cross-Module-Coupling-State | Host → Catalog.Application ICatalogDirectory (evacuate for SEO HTTP) |
| Cross-Module-Join-State | NONE for SEO |
| Persistence-Ownership-State | CORRECT (Catalog DbContext) |
| Endpoint-Ownership-State | HOST_OWNED → MODULE_OWNED |
| Schema-Migration-State | UNCHANGED |
| Behavior-Preservation-Risk | MEDIUM (scope + workspace.* codes + slug/concurrency/history) |
| Final-Disposition | READY_TO_MIGRATE |

## Target analyzed

Three Admin product SEO routes under `/v1/admin/products/{productId}` plus Composer/Directory SEO members. ProductWorkspace non-SEO surfaces retained. Host/Admin remains 52 files.

## Workspace scope

`X-Tooba-Workspace-Scope=view` → GET SEO/readiness allowed; PUT denied `workspace.permission.denied` 403. Generalize W13 media helper to shared `CatalogWorkspaceScope`; media alias retained.

## Actor

PUT queues `EventSeoChanged` with actor — bind via `CatalogActorRequestBinding`. Reads do not bind.

## Canonical references

Offer/BuildingBlocks Result + ApiResponseFactory; W13 ProductMedia evacuate pattern.
