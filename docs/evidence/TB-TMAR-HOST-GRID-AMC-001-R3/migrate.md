# TB-TMAR-HOST-GRID-AMC-001-R3 — Migrate

## Scope

Replace Reviews Host CatalogDbContext reach-through with Catalog Contracts product-title ID lookup; move Reviews policy + engine + `AdminReviewItem` into Reviews module.

## Disposition executed

| Item | Before | After |
| --- | --- | --- |
| Product title ID resolve | Host engine → `CatalogDbContext` | `ICatalogAdminProductTitleIdLookup` (Catalog.Contracts) + Catalog.Infrastructure adapter |
| `AdminListGridPolicies.Reviews` | Host | `Reviews.Infrastructure/Grid/ReviewsAdminGridPolicies` |
| `AdminReviewGridQueryEngine` | Host/Grid | `Reviews.Infrastructure/Grid` |
| `AdminReviewItem` | Host/Reviews/ReviewEndpoints | `Reviews.Application` |
| Grid port | none | `IAdminReviewGridPort` + `AdminReviewGridAdapter` |
| `ReviewPanelComposer` | Host Grid + CatalogDbContext | `IAdminReviewGridPort` only |
| Program DI Host Review engine | registered | **Removed** |

## Ownership notes

- FOUNDATION_PARTIAL Reviews: Application gained BuildingBlocks + row model + port; Infrastructure gained Grid/Adapters + Catalog.Contracts ref.
- Page title enrich still uses existing `ICatalogLookupGateway.GetProductTitlesAsync` (pre-existing Catalog.Application dependency via ReviewDirectory).
- No CatalogDbContext on Host Reviews path or Reviews grid engine.

## Behavior parity

- Field whitelist / default sort `created` desc / search across reviewer/body/title/product title IDs preserved.
- Title filter operators (contains/equals/blank/…) preserved via Catalog Contracts.

## Guards

- New: `HostGridAmcR3GuardTests`

## Explicit non-goals

- Full Reviews ARCH-COMPLETE-002 / dissolving ReviewPanelComposer HTTP ownership
- Moving `ICatalogLookupGateway` titles enrich to Contracts (separate from ID resolve)
- Schema change
- Commit / push / Bridge POST
