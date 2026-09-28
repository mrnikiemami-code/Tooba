# Migrate + Certify — TB-TMAR-HOST-DEVELOPMENT-AMC-002

## Migration performed
Consolidated 7 duplicated Development seed wrappers into **one** Host platform seam and reconnected the composition root to the Catalog-owned seeds.

### Files deleted (7)
| Old path | Responsibility rehomed to |
| --- | --- |
| Development/CatalogAttributeSchemaDevelopmentSeedHost.cs | DevelopmentTenantCommerceContext.cs seam + Catalog seed |
| Development/FashionTemplateCatalogSeedHost.cs | same seam + Tooba.Catalog.Infrastructure.Development seed |
| Development/IndustryBatchATemplateCatalogSeedHost.cs | same seam + Catalog seed |
| Development/IndustryBatchBTemplateCatalogSeedHost.cs | same seam + Catalog seed |
| Development/IndustryBatchCTemplateCatalogSeedHost.cs | same seam + Catalog seed |
| Development/LandingPageDevelopmentSeedHost.cs | same seam + Catalog seed |
| Development/StoreMenuDevelopmentSeedHost.cs | same seam + Catalog seed |

### Files created (1)
| New path | Class |
| --- | --- |
| Development/DevelopmentTenantCommerceContext.cs | ALLOWED_DEVELOPMENT_COMPOSITION — single `store-alpha` tenant resolve + `CommerceContext` assign + module seed invocation |

### Files modified
| Path | Change |
| --- | --- |
| Host/Tooba.Host/Program.cs | 7 `*SeedHost.ApplyAsync` calls → `DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync(app.Services, trace, static (p, ct) => Seed.ApplyAsync(p, ct))`; added `using Tooba.Catalog.Infrastructure.Development;` |

Trace labels preserved exactly: `catalog-attribute-schema-seed`, `landing-dev-seed`, `menu-dev-seed`, `fashion-template-catalog-seed`, `industry-batch-a/b/c-template-catalog-seed`.

### Guarantees used (NOT invented ports)
- Catalog seed `ApplyAsync(IServiceProvider, CancellationToken)` reads `ICommerceContextAssigner` / `CatalogDbContext` from the same scope; the seam assigns `CommerceContext` **before** invoking, so `scope.ServiceProvider` == `provider` (behavior identical).
- Only scope creation (`CreateAsyncScope`) differs from the old composition root by nesting one extra scope, which is intended (the old `ProductWorkspaceDevelopmentBootstrap` already calls these seeds inside its own scope identically).

## Final tree (6 production files)
| File | Classification |
| --- | --- |
| MarketplaceDevelopmentBootstrap.cs | ALLOWED_DEVELOPMENT_COMPOSITION (retained lock) |
| MarketplaceAdminDevBootstrap.cs | ALLOWED_DEVELOPMENT_RUNTIME_SEAM (retained lock) |
| MarketplaceSellerDevBootstrap.cs | ALLOWED_DEVELOPMENT_RUNTIME_SEAM (retained lock) |
| DevelopmentTenantCommerceContext.cs | ALLOWED_DEVELOPMENT_COMPOSITION |
| CatalogAttributeSchemaSellableEnricher.cs | STRUCTURAL_DEBT_ONLY (bounded blocker) |
| ProductWorkspaceDevelopmentBootstrap.cs | STRUCTURAL_DEBT_ONLY (separate bounded task) |

## Certify — structured state
| Field | Value |
| --- | --- |
| Ownership-State | CORRECT (for migrated slice) |
| File-Cohesion-State | COHESIVE for creatures above; envicher/PWBL are documented debt |
| Cross-Module-Coupling-State | LEGAL_CONTRACTS_ONLY for migrated slice (Program→Catalog.Infrastructure.Development is allowed composition, same as ProductWorkspace) |
| Persistence-Ownership-State | FOREIGN_ACCESS only inside the two documented debt files |
| Endpoint-Ownership-State | MODULE_OWNED (ZERO Host routes in folder) |
| Logging-State | CANONICAL (existing `app.Logger.LogError` blocks untouched) |
| Sensitive-Logging-State | NONE |
| OpenTelemetry-State | CANONICAL (unchanged) |
| Correlation-Trace-State | CANONICAL (CommerceContext TraceId labels preserved) |
| Schema-Migration-State | UNCHANGED |
| Behavior-Preservation-Risk | LOW (dev-only, `IsDevelopment()` gated) |
| Final-Disposition | 7 files READY_FOR_CERTIFICATION; 2 debt files documented |

## Guard changes
- `HostDevelopmentAmcGuardTests`: stale 3-file equality → classified allowlist dict (equality still enforced; any unclassified file fails) + permanent assertion that the 7 wrappers stay absent.
- `HostAdminAmcW34TemplateSeedsGuardTests`: updated to assert wrappers absent, seeds Catalog-owned, single seam invoked.
- `HostAdminAmcLandingPageSeedGuardTests`: updated to assert wrapper absent + Catalog seed invoked via seam.

## Focused validation
- `dotnet build src/backend/Host/Tooba.Host` → **succeeded, 0 errors**
- `dotnet test Tooba.Host.Tests` filter (`HostDevelopmentAmcGuardTests`, `HostAdminAmcW34TemplateSeedsGuardTests`, `HostAdminAmcLandingPageSeedGuardTests`, `HostAdminCanon002GuardTests`, `HostCartResidualGuardTests`, `HostAdminCanon009GuardTests`, `HostAdminCanonicalCertificationGuardTests`, `TmarDurableGuardTests`) → **Passed 63 / Failed 0**

**Canonical validation count = 63/63** (reconciled by `TB-TMAR-HOST-DEVELOPMENT-AMC-002-R1`). An earlier draft recorded **57/57**, which was the first focused run *before* the stale `HostDevelopmentAmcGuardTests` allowlist was repaired and before the `TmarDurableGuardTests` fleet was added to the filter. 63/63 is the deterministically re-proven count over the same focused filter on current main.

## Destination integrity check (Analyze §3d / Migrate §25c / Certify §13b)
| Destination | Classification | Change |
| --- | --- | --- |
| Host/Development/ (active folder) | OPEN_FOR_CURRENT_TASK | -7 wrappers, +1 `DevelopmentTenantCommerceContext.cs` |
| Host/Admin, Host/Content, Host/Composition, Host/Wallet, Host/Storefront, all other Host folders | NOT A DESTINATION | untouched |
| Modules/Catalog/** | LOCKED_BY_ACCEPTED_DISPOSITION | untouched (0 files changed) |
| Modules/** (all others) | LOCKED_BY_ACCEPTED_DISPOSITION | untouched |

- No file was moved **into** any other Host folder or resurrected folder.
- No new Host folder was invented.
- The only protected-set change is the **active** Development folder's own allowlist, which the task explicitly reopens; its stale 3-file set (RED at clean HEAD) was replaced by a classified 6-file set with a durable guard, recorded in SoT `hostDevelopmentAmc002`.
- Result: `SINK_FOLDER_REGRESSION` = **NONE**.

## Residual debt / blockers
1. `CatalogAttributeSchemaSellableEnricher` (cross-module) — needs dev-seed ports in Offer/Party/Pricing/Inventory/Tax before Catalog can own it. Requires Architect decision + touching reference module Offer.
2. `ProductWorkspaceDevelopmentBootstrap` — cross-module dev-seed orchestrator; separate bounded task.
3. Repository-wide pre-existing size/inventory baseline redness (unrelated; not repaired here).
