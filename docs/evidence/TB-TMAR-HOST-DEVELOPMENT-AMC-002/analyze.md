# Analyze — TB-TMAR-HOST-DEVELOPMENT-AMC-002

## Active Host folder
`src/backend/Host/Tooba.Host/Development/`

## Before tree (12 production files)
| # | File | LOC | Responsibility |
| --- | --- | --- | --- |
| 1 | MarketplaceDevelopmentBootstrap.cs | 67 | HOST_COMPOSITION_ROOT + 10 foreign `DbContext` migrate |
| 2 | MarketplaceAdminDevBootstrap.cs | 36 | HOST_RUNTIME_SEAM (authorization tuple) |
| 3 | MarketplaceSellerDevBootstrap.cs | 43 | HOST_RUNTIME_SEAM (tuple + seller snapshot) |
| 4 | CatalogAttributeSchemaDevelopmentSeedHost.cs | 36 | DEVELOPMENT_SEED wrapper |
| 5 | CatalogAttributeSchemaSellableEnricher.cs | 154 | CROSS_MODULE_DEVELOPMENT_ORCHESTRATION |
| 6 | FashionTemplateCatalogSeedHost.cs | 36 | DEVELOPMENT_SEED wrapper |
| 7 | IndustryBatchATemplateCatalogSeedHost.cs | 36 | DEVELOPMENT_SEED wrapper |
| 8 | IndustryBatchBTemplateCatalogSeedHost.cs | 36 | DEVELOPMENT_SEED wrapper |
| 9 | IndustryBatchCTemplateCatalogSeedHost.cs | 36 | DEVELOPMENT_SEED wrapper |
| 10 | LandingPageDevelopmentSeedHost.cs | 36 | DEVELOPMENT_SEED wrapper |
| 11 | StoreMenuDevelopmentSeedHost.cs | 36 | DEVELOPMENT_SEED wrapper |
| 12 | ProductWorkspaceDevelopmentBootstrap.cs | 428 | CROSS_MODULE_DEVELOPMENT_ORCHESTRATION (god file) |

## Key findings
1. **Prior evidence was stale.** `TB-TMAR-HOST-DEVELOPMENT-AMC-001` certified only 3 files; the folder grew to 12 via W25/W32/W34.
2. **Folder guard was RED at clean HEAD `4376ee82`.** `HostDevelopmentAmcGuardTests.Development_folder_matches_exact_retained_allowlist` allowed only 3 files.
3. **The 7 seed wrappers + enricher were post-evacuation residue:** Catalog already owns every seed implementation; the Host wrappers only re-resolved tenant `store-alpha`, assigned `CommerceContext` (duplicated 8×) and called `Catalog*.ApplyAsync`.
4. **`ControlPlaneRegistry` is `internal` to `Tooba.Host`** and no public tenant-catalog port exists in `BuildingBlocks`. Therefore the wrapper's tenant/commerce-assignment body is genuinely a **Host platform seam**, not a Catalog responsibility. Correct disposition = consolidate to **one** thin seam, not "move to Catalog".
5. **`CatalogAttributeSchemaSellableEnricher` cannot move to Catalog** without adding dev-seed ports to Offer/Party/Pricing/Inventory/Tax Contracts (`Catalog.Infrastructure` has no Application refs for them) — an Offer-reference-module change. Bounded blocker.

## Illegal dependencies found
- 8 files → `Tooba.Catalog.Infrastructure.Development` (wrapper duplication)
- `CatalogAttributeSchemaSellableEnricher` → Catalog/Party `DbContext` + foreign `Offer/Party/Pricing/Inventory/Tax` Application ports
- `ProductWorkspaceDevelopmentBootstrap` → ~35 foreign `Infrastructure.Persistence` + `DbContext`/`DbSet` read/write + cross-module `SaveChanges`
- `MarketplaceDevelopmentBootstrap` → 10 foreign `DbContext` (migrate-only)

## Disposition (user-approved scope)
- `READY_TO_MIGRATE` for the 7 seed wrappers (evacuate → single Host seam).
- `FOUNDATION_REQUIRES_SEPARATE_BOUNDED_TASK` for `ProductWorkspaceDevelopmentBootstrap` (user-selected).
- `NEEDS_ARCHITECT_DECISION` for `CatalogAttributeSchemaSellableEnricher` (bounded blocker).
- Retain the 3 Marketplace bootstraps (prior canonical retention lock).
