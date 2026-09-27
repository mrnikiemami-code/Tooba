# Analyze — W13 Product Media Admin slice

## Structured state (pre-migrate)

| Field | State |
|---|---|
| Foundation-State | FOUNDATION_READY (Catalog certified; add ProductMedia capability folders) |
| Ownership-State | MUST_SPLIT (media vs remaining ProductWorkspace) |
| File-Cohesion-State | MULTI_RESPONSIBILITY_COHESION_VIOLATION on ProductWorkspace* (media members only in scope) |
| Localization-State | EXCEPTION_MESSAGE_BASED on Host media (IOE → PlatformHttpException) |
| API-Result-Pattern-State | AD_HOC (Results.Json + PlatformHttpException) |
| Stable-Error-Code-State | STRING_HEURISTIC for reorder (Persian message Contains) |
| CQRS-State | MISSING on media HTTP |
| Validator-Coverage-State | GAPS (no FluentValidation on Host media) |
| Contracts-Boundary-State | CLEAN target (Application models/ports) |
| Cross-Module-Coupling-State | Host → Catalog.Application ICatalogDirectory (to evacuate for media HTTP) |
| Cross-Module-Join-State | NONE for media |
| Persistence-Ownership-State | CORRECT (Catalog DbContext) |
| Endpoint-Ownership-State | HOST_OWNED → MODULE_OWNED |
| Schema-Migration-State | UNCHANGED |
| Behavior-Preservation-Risk | MEDIUM (workspace scope + workspace.* codes + Primary JSON + 201 without Location) |
| Final-Disposition | READY_TO_MIGRATE |

## Target analyzed

Eight Admin product media routes under `/v1/admin/products/{productId}` plus Composer/Directory media members. ProductWorkspace non-media surfaces retained.

## Workspace scope finding

`X-Tooba-Workspace-Scope=view` → Host `ReadPermissions` yields `CanEditCatalog=false` → writes call `EnsureCatalogEdit` → `workspace.permission.denied` 403. Reads do not call EnsureCatalogEdit. Live transport policy — preserve via module-owned endpoint helper; do not copy Host `ProductWorkspacePermissions` into Catalog.

## Actor finding

Media mutations queue `ProductHistory` with `_actor?.ActorUserId/DisplayName`. Bind actor on write routes. Reads do not write history.

## Primary invariant

When media exists, exactly one primary via `EnforcePrimaryUniqueness`. First attach is primary. Set-primary sets exactly one. Reorder preserves primary flags unless uniqueness repair. Detach primary repairs to first DisplayOrder.

## Canonical references

Offer/BuildingBlocks Result + ApiResponseFactory; W12 CategoryChanges capability-first CQRS; ICatalogAdminAuthorizer; CatalogActorRequestBinding.
