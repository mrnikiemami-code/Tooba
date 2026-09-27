# Analyze — W15 Product Workspace History read Admin slice

## Structured state (pre-migrate)

| Field | State |
|---|---|
| Foundation-State | FOUNDATION_READY (Catalog certified; add ProductHistory capability folders) |
| Ownership-State | MUST_SPLIT (History GET vs remaining ProductWorkspace) |
| File-Cohesion-State | MULTI_RESPONSIBILITY_COHESION_VIOLATION on ProductWorkspace* (history route members only in scope) |
| Localization-State | CANONICAL Domain labels via ProductHistoryRules; Host maps missing product via PlatformHttpException |
| API-Result-Pattern-State | AD_HOC (Results.Json + PlatformHttpException catch) |
| Stable-Error-Code-State | workspace.product.missing already CatalogErrorCodes; Host still throws PlatformHttpException |
| CQRS-State | MISSING on history HTTP |
| Validator-Coverage-State | NO_VALIDATOR_REQUIRED expected (Guid route + optional section/skip/take normalized in seam) |
| Contracts-Boundary-State | CLEAN target (Application models/ports; no *Contracts.cs bundle) |
| Cross-Module-Coupling-State | Host → Catalog.Application ICatalogDirectory for history page |
| Cross-Module-Join-State | NONE (Catalog ProductHistoryEntries only) |
| Persistence-Ownership-State | CORRECT (Catalog DbContext) |
| Endpoint-Ownership-State | HOST_OWNED → MODULE_OWNED |
| Schema-Migration-State | UNCHANGED |
| Behavior-Preservation-Risk | LOW–MEDIUM (paging/filter/order/actor fallback/view scope) |
| Final-Disposition | READY_TO_MIGRATE |

## Target analyzed

Single Admin route `GET /v1/admin/products/{productId}/history` with optional `section`, `skip`, `take`. Composer `GetHistoryPageAsync` + page view models. Aggregate `BuildHistoryShellListsAsync` / `ProductHistoryItem` Activity/Audit retained in Host.

## Workspace scope

`X-Tooba-Workspace-Scope=view` → ReadPermissions.CanView=true → history GET allowed. No edit-scope gate. Do not call `CatalogWorkspaceScope.AllowsCatalogEdit` on this GET.

## Actor

Read does not bind actor. Actor display on rows uses stored ActorDisplayName with ProductHistoryRules.ActorSystemFa fallback.

## Canonical references

W13 ProductMedia / W14 ProductSeo evacuate; BuildingBlocks Result + ApiResponseFactory; ICatalogAdminAuthorizer.
