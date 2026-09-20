# Top oversized-file decomposition queue — TB-TMAR-BOUNDARY-V1-R1

Prioritized for later waves. NO splitting in this repair.

| Order | Path | LOC | Layer | Risk | Why hard | Coverage now | Missing characterization | Likely boundary |
|---:|---|---:|---|---|---|---|---|---|
| 1 | `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogDirectory.cs` | 5124 | Catalog/Infra | Critical | Storefront+admin read/write hub; EF coupling; many call sites | Used in Host integration tests (cart/checkout/campaign) as concrete Directory; thin slice coverage | Per-capability Directory method suites (category/product/landing/settings) | Split by use-case into focused Directories + Contracts |
| 2 | `src/backend/Modules/Catalog/Tooba.Catalog.Domain/CatalogDomain.cs` | 2305 | Catalog/Domain | Critical | Aggregate of many domain types/invariants in one compilation unit | Indirect via Directory/Host tests | Per-aggregate domain characterization before extract | Types by aggregate (Category/Product/Brand/…) |
| 3 | `src/backend/Host/Tooba.Host/Admin/AdminOrderOperationsComposer.cs` | 2468 | Host | High | Corrective actions + fulfillment gates; Host write debt | Partial: AdminOrderCorrectiveActionsTests, OrderSupply*, source-read asserts | Full corrective-action matrix characterization | CQRS commands per operation (HOST wave) |
| 4 | `src/backend/Host/Tooba.Host/Storefront/StorefrontEndpoints.cs` | 1242 | Host | High | Public HTTP surface; checkout/payment/cart | Multiple source-contract tests (checkout identity, abuse, payment guards) | Endpoint-group golden responses | Route groups → composers already partially started |
| 5 | `src/backend/Host/Tooba.Host/Storefront/StorefrontComposer.cs` | 1579 | Host | High | Read/write composition across modules | Limited focused composer tests | Per-flow characterization (cart/checkout/catalog read) | CQRS + Contracts for remaining Host writes |
| 6 | `src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs` | 1910 | Host | High | Admin product authoring orchestration | Sparse | Workspace command characterization | Product admin CQRS handlers |
| 7 | `src/backend/Modules/Catalog/Tooba.Catalog.Application/CatalogContracts.cs` | 1303 | Catalog/App | High | Contracts live in Application (TMAR debt) | Type used widely; few isolated contract tests | Contract surface snapshot tests before move | Extract `Tooba.Catalog.Contracts` (CONTRACTS wave) |
| 8 | `src/frontend/app/admin/category-admin-screen.tsx` | 2215 | Frontend | Medium | Monolithic admin UI | FE critical-storefront focused elsewhere; screen-level sparse | Screen interaction characterization before split | Feature panels / hooks |
| 9 | `src/frontend/app/admin/product-workspace-screen.tsx` | 1570 | Frontend | Medium | Coupled to ProductWorkspaceComposer API | Sparse | Workspace flow characterization | Section components |
| 10 | `src/frontend/app/admin/admin-api.ts` | 1326 | Frontend | Medium | Broad admin client surface | Indirect e2e/integration | Per-resource client modules + contract tests | Split by admin resource |

Recommended order follows table (Catalog god files and Host write composers before FE cosmetics). Contracts extraction (item 7) is sequenced by CONTRACTS-W1 independently of physical splits.
