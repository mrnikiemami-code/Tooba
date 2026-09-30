# TB-TMAR-HOST-GRID-AMC-001-R5-R1 — Migrate (repair)

## Scope

Remove Reviews → Catalog.Application leakage for admin grid title enrichment and remaining Reviews Catalog lookup usage. Reconcile recovery SoT to Grid R5-R1 closed HOST_ZERO.

## Disposition executed

| Item | Before | After |
| --- | --- | --- |
| AdminReviewGridQueryEngine title enrich | ICatalogLookupGateway (Catalog.Application) | ICatalogAdminProductTitleIdLookup.GetProductTitlesByIdsAsync (Contracts) |
| ReviewDirectory / ReviewsDevelopmentSeed | ICatalogLookupGateway + CatalogPublicationStatus (Application/Domain) | ICatalogReviewProductLookup (Contracts; status string) |
| Catalog.Contracts | title ID resolve only | + GetProductTitlesByIdsAsync; + ICatalogReviewProductLookup |
| Catalog.Infrastructure | title ID lookup | + CatalogReviewProductLookup adapter; title batch titles |
| Reviews.Infrastructure.csproj | Catalog.Application ref | Catalog.Contracts only |
| Host/Grid | ABSENT | ABSENT (unchanged) |
| Story / Party grid engines | Contracts-only foreign | CLEAN (audit) |

## Behavior parity

- Pending-only Reviews admin grid search/filter/paging/sort/page-title enrichment preserved.
- Review submit / published pages / home featured / development seed resolve products via Contracts seam with Published status string.

## Guards

- Updated: HostGridAmcR3GuardTests (no Application / GetProductTitlesByIdsAsync)
- New: HostGridAmcR5R1GuardTests
- Focused: HostGridAmcR3/R5, Reviews foundation/grid, TmarDurableGuardTests

## SoT

- lastAcceptedTask = TB-TMAR-HOST-GRID-AMC-001-R5-R1
- activeModule = Grid
- state = GRID_CLOSED_HOST_ZERO_USER_REVIEW_REQUIRED
- workflowStop = USER_REVIEW_HOST_GRID_AMC_001_R5_R1_CLOSED_HOST_ZERO
- automaticNext = NONE
- Storefront R4 preserved historical lineage

## Explicit non-goals

- Full Reviews/Catalog ARCH-COMPLETE-002
- Schema / frontend change
- Moving anything back to Host
