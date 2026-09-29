# Validation — TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001

## Focused build

| Project | Result |
|---------|--------|
| `BuildingBlocks/Tooba.Persistence/Tooba.Persistence.csproj` | Build succeeded, 0 errors |
| `Host/Tooba.Host/Tooba.Host.csproj` | Build succeeded, 0 errors |
| `Modules/Catalog/Tooba.Catalog.Infrastructure` | Build succeeded, 0 errors |
| `Modules/Promotion/Tooba.Promotion.Infrastructure` | Build succeeded, 0 errors |
| `Modules/Support/Tooba.Support.Infrastructure` | Build succeeded, 0 errors |
| All 28 module Infrastructure projects | compiled as transitive dependencies of `Tooba.Host` with 0 errors |

`Tooba.Host` does **not** reference `Tooba.MigrationRunner`. `Tooba.MigrationRunner` was not
touched.

## Focused tests

Command:

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~Architecture"
```

| Metric | Value |
|--------|-------|
| Total | 437 |
| Passed | 433 |
| Failed | 4 (all pre-existing, unrelated) |
| Duration | 2 s |

New guard class: `HostDevelopmentMigrationSeamGuardTests` → **5 of 5 passed**.

## Pre-existing unrelated failures (proven baseline, not caused by this task)

Same 4 tests fail on a stash-clean `main` tree:

1. `HostCachingAmcGuardTests.Cache_telemetry_uses_canonical_meter_and_bounded_dimensions`
2. `AddressBookValidatorCoverageGuardTests.AddressBook_is_certified_with_exactly_one_manifest_entry`
3. `ErrorCatalogUniqueCodeGuardTests.Composed_production_contributors_register_each_machine_code_exactly_once`
4. `ErrorCatalogUniqueCodeGuardTests.Composed_catalog_and_safe_error_mapper_resolve_shared_codes`

Method used: `git stash push --include-untracked` → run the four filters → `git stash pop`.
They failed identically with the changes absent.

## Guards updated (existing, not weakened)

| Guard file | Change |
|------------|--------|
| `HostDevelopmentAmcGuardTests` | allowlist now classifies `DevelopmentSchemaMigrator.cs` (ALLOWED_DEVELOPMENT_COMPOSITION); `ProductWorkspaceDevelopmentBootstrap.cs` entry removed; still exact-file-set equality with 5 files |
| `HostDevelopmentEnricherClosureGuardTests` | the two ProductWorkspace-bootstrap tests now assert the bootstrap is absent and the neutral migrator has zero business-seed authority / zero foreign DbContext |
| `HostAdminAmcW32PwShellFinalGuardTests` | asserts `DevelopmentSchemaMigrator.cs` exists, `ProductWorkspaceDevelopmentBootstrap` absent from `Program.cs` |
| `HostAdminAmcW34TemplateSeedsGuardTests` | Catalog seed call sites now asserted in the Catalog Infrastructure seam + `Program` seam call |
| `HostAdminAmcLandingPageSeedGuardTests` | landing seed assertion moved to the Catalog Infrastructure seam |
| `HostCustomerProfileEvacuationGuardTests` | module-owned CustomerProfile seed call sites now asserted against `DevelopmentSchemaMigrator.cs` (still exactly 2) |

## Guards added

`HostDevelopmentMigrationSeamGuardTests`:

1. neutral seam is in `Tooba.Persistence` with stable `Module`/`Order` and no module context names;
2. all 28 module composition roots register `AddModuleSchemaMigrator`;
3. the Wave 2 migration slice (`DevelopmentSchemaMigrator.cs` + `DevelopmentTenantCommerceContext.cs`)
   has zero foreign DbContext / special migrator / `Infrastructure.Persistence` references, and
   `Host/Development` still holds exactly 5 files with the bootstrap absent;
4. `Program.cs` has no `ProductWorkspaceDevelopmentBootstrap` reference;
5. SoT block + evidence folder exist.

## Task-requested guard coverage

| Requirement | Covered by |
|-------------|------------|
| `ProductWorkspaceDevelopmentBootstrap.cs` absent | `HostDevelopmentMigrationSeamGuardTests`, `HostDevelopmentAmcGuardTests`, `HostDevelopmentEnricherClosureGuardTests` |
| `DevelopmentSchemaMigrator.cs` has no foreign module references | `HostDevelopmentMigrationSeamGuardTests`, `HostDevelopmentEnricherClosureGuardTests` |
| Host/Development exact classified file set = 5 | `HostDevelopmentAmcGuardTests`, `HostDevelopmentMigrationSeamGuardTests` |
| ProductWorkspace business seed remains Catalog-owned | `HostDevelopmentEnricherClosureGuardTests` (`Workspace_demo_seed_is_catalog_owned_and_contracts_only`) |
| Each required Development schema module represented exactly once | `All_twenty_eight_modules_register_their_own_schema_migrator` (28 of 28) |
| Migration order explicit and deterministic | `ModuleSchemaMigrationOrder` constants + `Migration_order_parity` evidence |
| Duplicate module migration registration fails / guard-detected | `MigrateOrderedAsync` fail-fast on duplicate `Order`; `registrations == 28` exact-count guard |
| No foreign DbContext names in Host/Development | `ProductWorkspace_migration_slice_has_no_foreign_DbContext_or_persistence_namespace` |
| No sink-folder regression | `Host_Admin_count_15...` guards unchanged and passing |
| Program preserves both Development modes | `Program_invokes_marketplace_bootstrap_only_in_Development`; `DevelopmentSchemaMigrator.ApplyAsync` + `MigrateSchemaOnlyAsync` both called |
| Wave 1 Contracts-only seed boundary intact | `Catalog_infrastructure_references_foreign_contracts_not_foreign_internals` + Wave 1 guards |
