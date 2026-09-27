# Analyze — TB-TMAR-HOST-ADMIN-AMC-001-W5

## Target

`src/backend/Host/Tooba.Host/Admin/CatalogMegaMenuEndpoints.cs` — 4 Admin + 1 Storefront MegaMenu routes.

## Ownership verdict

True owner = **Catalog**. Host owns HTTP + AdminPanelAccess + PlatformHttpException/IOE→Problem mapping + direct `ICatalogDirectory`. Persistence/use-case authority already lives in `CatalogDirectory` MegaMenu members and Domain `CatalogMegaMenuTreeRules` / `CatalogMegaMenuComposer`.

## Foundation-State

Catalog = `FOUNDATION_READY` (W1–W4 capability-first CQRS pattern established; Tags is the W4 pattern reference).

## Routes (preserve exactly)

| Method | Path |
|---|---|
| GET | `/v1/admin/catalog/categories/{categoryId:guid}/mega-menu` |
| GET | `/v1/admin/catalog/categories/{categoryId:guid}/mega-menu/placement-options` |
| PUT | `/v1/admin/catalog/categories/{categoryId:guid}/mega-menu` |
| DELETE | `/v1/admin/catalog/categories/{categoryId:guid}/mega-menu` |
| GET | `/v1/storefront/mega-menu` |

## Current violations

- Host HTTP ownership of Catalog MegaMenu Admin + Storefront
- Direct `ICatalogDirectory` from Host endpoints
- `AdminPanelAccess` Host auth instead of `ICatalogAdminAuthorizer`
- Expected failures via `InvalidOperationException` + `Results.Problem(ex.Message)` (message-as-code)
- `PlatformHttpException` catch/map in Host endpoints
- Raw `Results.Json` / `Results.NoContent` instead of `ApiResponseFactory`
- MegaMenu not structured as Commands/Queries/Models/Ports/Validators
- Broad directory used for focused MegaMenu capability

## Behavior to preserve

- Locale query default `fa-IR` + `CatalogCategorySlugNormalizer.NormalizeLocale`
- Unbound category → preview/default configuration view (not an error)
- Display title fallback: override → translation name → ResolveCategoryDisplayName → "—"
- Parent path via `BuildMenuPathAsync`; presentation level via `ComputePresentationLevel`
- Placement options exclude self category; filter at `MaxPresentationDepth`; order by SortOrder
- Upsert create/update binding + translation override semantics
- Remove absent binding = idempotent success
- Remove with children = rejected
- Category missing on get/upsert = expected failure (normalize to Result + stable code)
- Storefront composition: visible + Category type + published + visible category + slug + title
- Auth on four Admin routes; Storefront unauthenticated
- Success shapes: JSON views / NoContent mutate / storefront list

## Canonical target

- Endpoints: `Catalog.Endpoints/Admin/MegaMenu/` + `Catalog.Endpoints/Storefront/MegaMenu/`
- Application: `MegaMenu/{Commands,Queries,Models,Ports,Validators}`
- Persistence: focused `IMegaMenuDirectory` + `MegaMenuDirectory`
- Result + `ApiResponseFactory` + `CatalogErrorCodes` + resx
- Auth: `ICatalogAdminAuthorizer` (Admin only)
- Legacy `ICatalogDirectory` MegaMenu methods remain for CatalogDemo/tests; thin one-way wrappers into MegaMenuDirectory
- Domain `ValidatePlacement` returns typed violation (no IOE message-as-code); Directory maps to one placement error code

## Structured state (pre-migrate)

| Field | State |
|---|---|
| Ownership-State | MUST_SPLIT (Host HTTP vs Catalog capability) |
| CQRS-State | MISSING |
| API-Result-Pattern-State | AD_HOC / RAW_RESULTS |
| Localization-State | EXCEPTION_MESSAGE_BASED |
| Cross-Module-Coupling-State | NONE (within Catalog) |
| Endpoint-Ownership-State | HOST_OWNED |
| Final-Disposition | READY_TO_MIGRATE |

## Out of scope

Facets/Categories/Attributes/StoreAppearance*/ProductWorkspace*/Merchandising*/W6/schema/frontend.
