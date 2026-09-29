# Certification — TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001

Certification standard: `.cursor/skills/tooba-architecture-certify/SKILL.md` against ARCH-COMPLETE-002
and the current TMAR SoT.

## Verdict

`READY_FOR_CERTIFICATION` — Wave 1 objective met. `certificationState = PASS` for this bounded task.
The target Host file is **not** deleted (Wave 2 owns that) and remains explicitly classified debt.

## 1. Physical tree

```
src/backend/Modules/Catalog/Tooba.Catalog.Application/Development/
  IWorkspaceDemoSeed.cs                    (new)

src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/
  WorkspaceDemoSeed.cs                     (new)
  WorkspaceDemoProductSeed.cs              (new)
  WorkspaceDemoMarketplaceSeed.cs          (new)
  CatalogAttributeSchemaDevelopmentSeed.cs (existing)
  CatalogAttributeSchemaSellableEnricher.cs(existing)
  LandingPageDevelopmentSeed.cs            (existing)
  StoreMenuDevelopmentSeed.cs              (existing)
  IndustryBatch*TemplateCatalogSeed.cs     (existing)
```

```
src/backend/Host/Tooba.Host/Development/
  ProductWorkspaceDevelopmentBootstrap.cs  (MIGRATION_AND_SEED_ORDERING_ONLY)
  DevelopmentTenantCommerceContext.cs      (ALLOWED_DEVELOPMENT_COMPOSITION)
  MarketplaceDevelopmentBootstrap.cs       (ALLOWED_DEVELOPMENT_COMPOSITION)
  MarketplaceAdminDevBootstrap.cs          (ALLOWED_DEVELOPMENT_RUNTIME_SEAM)
  MarketplaceSellerDevBootstrap.cs         (ALLOWED_DEVELOPMENT_RUNTIME_SEAM)
```

Host/Development production file count: **5 → 5**.

## 2. Path ↔ namespace

All four new files verified exact (path suffix equals namespace), no alias, no `TypeForwardedTo`, no
duplicate physical copy, no stale root copy. All resolve through normal SDK globbing inside their
existing project folders.

## 3. Host authority classification

| Host reference | Class |
| --- | --- |
| `ProductWorkspaceDevelopmentBootstrap.cs` → `IWorkspaceDemoSeed` (Catalog Application) | ALLOWED_CONTRACT_CONSUMPTION |
| `ProductWorkspaceDevelopmentBootstrap.cs` → `I*SchemaMigrator` (Offer/Pricing/Inventory/Tax/Promotion Contracts) | ALLOWED_COMPOSITION_ROOT |
| `ProductWorkspaceDevelopmentBootstrap.cs` → module Development seeds and `DbContext.Database.MigrateAsync` | STRUCTURAL_DEBT_ONLY (Wave 2) |
| `ProductWorkspaceDevelopmentBootstrap.cs` → Catalog/Party business persistence and foreign Application/Domain/MediatR | **ZERO** |
| `Program.cs` → `ProductWorkspaceDevelopmentBootstrap` | ALLOWED_COMPOSITION_ROOT |

`ILLEGAL_BUSINESS_AUTHORITY = 0` for the Wave 1 seed slice.
`ILLEGAL_PERSISTENCE_AUTHORITY` remains for the migration list only — explicitly deferred to Wave 2.

## 4. Cross-module boundary audit

`WorkspaceDemoProductSeed`, `WorkspaceDemoMarketplaceSeed`, `WorkspaceDemoSeed` scanned: no foreign
`.Application`, `.Infrastructure`, `.Domain`, no foreign `DbContext`/`DbSet`, no `IServiceProvider`,
no `MediatR`. Foreign interaction is exclusively Party/Offer/Pricing/Inventory/Tax `.Contracts`.
Guarded by `HostDevelopmentEnricherClosureGuardTests`.

## 5. Persistence ownership

- Catalog workspace seeds touch only `CatalogDbContext` (own module schema).
- Party display-name mutation now happens inside `Party.Infrastructure`.
- No cross-module join, no foreign `DbSet`, no transaction spanning module databases.
- `ARCH-DATA-001` intact; no migration regenerated.

## 6. Schema / route / frontend

`schemaChangeState = NONE`, `routeChangeState = NONE`, `frontendState = UNCHANGED`.

## 7. Durable guards added/updated

| Guard | Change |
| --- | --- |
| `HostDevelopmentAmcGuardTests.ClassifiedAllowlist` | `ProductWorkspaceDevelopmentBootstrap.cs` reclassified to migration-only Wave 2 debt (folder still exactly 5 files) |
| `HostDevelopmentEnricherClosureGuardTests.ProductWorkspace_bootstrap_remains_migration_only_debt` | replaces the old "remains open debt and untouched" assertion |
| `HostDevelopmentEnricherClosureGuardTests.Workspace_demo_seed_is_catalog_owned_and_contracts_only` | new: Catalog ownership, Contracts-only, pinned seed values |
| `HostDevelopmentEnricherClosureGuardTests.ProductWorkspace_host_file_has_zero_business_seed_authority` | new: zero foreign business/Application/Domain/MediatR/seed-constant authority in the Host file |

No existing guard was weakened.

## 8. Closed-folder regression

No file was added to any closed Host folder. `Host/Development` count unchanged at 5. No new Host
folder. No unrelated certified module (Offer/Pricing) was modified beyond the parity-required absence
of change. `sinkFolderRegressionState = NONE`.

## 9. Residual debt (non-blocking, explicitly owned by Wave 2)

- `ProductWorkspaceDevelopmentBootstrap.cs` still types 28 `DbContext`s for schema migration.
- `Host/Wishlist/WishlistDevelopmentSeed.cs` and `Host/Settings/SettingsFoundationDevelopmentSeed.cs`
  remain out of scope per the accepted closure plan.

## 10. Known pre-existing failures

Unrelated to this task and reproduced at clean HEAD: `TmarSourceSizeAndInfraAppTests` (3, caused by the
sibling `.tmp-baseline` worktree being scanned), `SupportFoundationTests` (2), `ReviewsFoundationTests` (1).
