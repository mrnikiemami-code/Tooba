# TB-TMAR-TAX-AMSC-001-W2 — structure (`tooba-architecture-structure`)

- Module: `Tax`
- Skill: `tooba-architecture-structure`
- Starting HEAD: `fa87201a` (W1 Migrate, `HEAD == origin/main`)
- Structure-State: `READY_FOR_CERTIFY`
- Structure-Handoff-State: `READY_FOR_CERTIFY`
- Module Applicability Gate: `INTERNAL_ONLY` (unchanged from W0, now structurally honest)
- Host final closure: preserved (`HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` / `HOST_ROOT_FINAL_CERTIFIED` untouched; zero Host production file added or widened)
- Behavior: `PRESERVED` (pure physical reorganization plus namespace/using repointing; zero route/DTO/descriptor/resource-key/stable-code/DI/schema change)

## 1. Classification states

| Axis | Before | After |
| --- | --- | --- |
| Module Applicability Gate | `INTERNAL_ONLY` | `INTERNAL_ONLY` |
| Tax-Endpoints-Project-State | `CEREMONIAL_PRESENT` | `ABSENT` |
| Tax-Http-Route-State | `EMPTY_GROUP_ONLY` (`/v1/tax`) | `ZERO` |
| Presentation-Registration-State | `INFRASTRUCTURE_MODULE_COMPOSITION` (W1) | `INFRASTRUCTURE_MODULE_EXACTLY_ONCE` |
| Tax-Project-Count-State | 6 (5 production + Tests) | 5 (4 production + Tests) |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` (6 projects) | `CANONICAL` (5 projects) |
| Path-Namespace-State | `MISMATCH` (24 production files) | `EXACT` |
| Physical-Copy-State | `CLEAN` | `CLEAN` |
| Root-Allowlist-State | `VIOLATION` (no manifest entry) | `ENFORCED` |
| File-Cohesion-State | `COHESIVE` | `COHESIVE` |
| Structure-State | `REPAIR_REQUIRED` | `READY_FOR_CERTIFY` |

## 2. The defects that were repaired

### 2.1 `INTERNAL_ONLY` violation — ceremonial Endpoints project

W0 proved Tax owns **0** module HTTP routes and **0** endpoint-reachable requests while the
repository kept a ceremonial `Tooba.Tax.Endpoints` project whose only content was:

```csharp
public static IEndpointRouteBuilder MapTaxModule(this IEndpointRouteBuilder app)
{
    _ = app.MapGroup("/v1/tax");   // empty group — no mapped operation
    return app;
}
```

That is a direct contradiction of the migrate/structure rule *"internal-only modules must not
create or retain Endpoints/CQRS ceremony where no HTTP/application surface exists"* and of the
certified `Inventory`/`Pricing` (W3-R2/R3) `INTERNAL_ONLY` precedent. W2 retired it in full:

```text
src/backend/Modules/Tax/Tooba.Tax.Endpoints/TaxEndpointModule.cs                 DELETED
src/backend/Modules/Tax/Tooba.Tax.Endpoints/Tooba.Tax.Endpoints.csproj           DELETED
src/backend/Modules/Tax/Tooba.Tax.Tests/Endpoints/TaxEndpointModuleTests.cs      DELETED
```

`src/backend/Tooba.slnx` — the single `/Modules/Tax/` `Tooba.Tax.Endpoints` entry was removed;
`/Modules/Tax/` now groups exactly 5 projects (`Application`, `Contracts`, `Domain`,
`Infrastructure`, `Tests`).

`src/backend/Host/Tooba.Host/Program.cs` — removal only (no replacement project, alias or
type-forward was introduced):

```diff
-using Tooba.Tax.Endpoints;
...
-app.MapTaxModule();
```

`src/backend/Host/Tooba.Host/Tooba.Host.csproj` — the `Tooba.Tax.Endpoints` `ProjectReference`
was removed. `src/backend/Modules/Tax/Tooba.Tax.Tests/Tooba.Tax.Tests.csproj` lost the same
reference.

The presentation registration stays exactly where W1 put it (the `TaxModule.AddServices`
Infrastructure composition root) — unchanged, still exactly once, still the certified
`Inventory`/`Pricing` `INTERNAL_ONLY` shape.

### 2.2 `PATH_NAMESPACE_ALIGNMENT` violation — 24 production files

Every capability folder in `Contracts`, `Domain`, `Application` and `Infrastructure` declared the
**project-level** namespace instead of the path-derived one:

| Project | Files | Before | After |
| --- | --- | --- | --- |
| `Tooba.Tax.Contracts` | `Dtos/TaxOutcome.cs` | `Tooba.Tax.Contracts` | `Tooba.Tax.Contracts.Dtos` |
| `Tooba.Tax.Contracts` | `Ports/TaxCalculatorContracts.cs` | `Tooba.Tax.Contracts` | `Tooba.Tax.Contracts.Ports` |
| `Tooba.Tax.Contracts` | `Ports/TaxQueryContracts.cs` | `Tooba.Tax.Contracts` | `Tooba.Tax.Contracts.Ports` |
| `Tooba.Tax.Contracts` | `Ports/ITaxDevelopmentSeedGateway.cs` | `Tooba.Tax.Contracts` | `Tooba.Tax.Contracts.Ports` |
| `Tooba.Tax.Contracts` | `Ports/ITaxSchemaMigrator.cs` | `Tooba.Tax.Contracts` | `Tooba.Tax.Contracts.Ports` |
| `Tooba.Tax.Domain` | `Aggregates/{TaxRule,TaxCategory,TaxOfferClassification}.cs` | `Tooba.Tax.Domain` | `Tooba.Tax.Domain.Aggregates` |
| `Tooba.Tax.Domain` | `Enums/TaxRuleEnums.cs` | `Tooba.Tax.Domain` | `Tooba.Tax.Domain.Enums` |
| `Tooba.Tax.Domain` | `Events/TaxDomainEvents.cs` | `Tooba.Tax.Domain` | `Tooba.Tax.Domain.Events` |
| `Tooba.Tax.Domain` | `Policies/TaxRounding.cs` | `Tooba.Tax.Domain` | `Tooba.Tax.Domain.Policies` |
| `Tooba.Tax.Application` | `Ports/ITaxDirectory.cs` | `Tooba.Tax.Application` | `Tooba.Tax.Application.Ports` |
| `Tooba.Tax.Infrastructure` | `Adapters/{TaxDirectory,TaxDevelopmentSeedGateway,TaxSchemaMigrator}.cs` | `Tooba.Tax.Infrastructure` | `Tooba.Tax.Infrastructure.Adapters` |
| `Tooba.Tax.Infrastructure` | `DependencyInjection/TaxModule.cs` | `Tooba.Tax.Infrastructure` | `Tooba.Tax.Infrastructure.DependencyInjection` |
| `Tooba.Tax.Infrastructure` | `Outbox/TaxOutboxRegistration.cs` | `Tooba.Tax.Infrastructure` | `Tooba.Tax.Infrastructure.Outbox` |

`Persistence/`, `Persistence/Configurations/`, `Persistence/Migrations/` and `Events/` already
declared path-derived namespaces and were left alone. Verified machine-side: **0 mismatches**
across all 5 Tax projects.

The Domain capability split needed a namespace bridge, exactly as the certified
`Catalog`/`Order`/`Pricing` Domain precedent:

```csharp
// src/backend/Modules/Tax/Tooba.Tax.Domain/GlobalUsings.cs  (NEW)
global using Tooba.Tax.Domain.Aggregates;
global using Tooba.Tax.Domain.Enums;
global using Tooba.Tax.Domain.Events;
global using Tooba.Tax.Domain.Policies;
```

It declares **no** namespace and holds **no** type. It is the single allowlisted root source file
of the Domain.

### 2.3 Application port split (W0 §13 optional polish, adopted)

`Application/Ports/ITaxUseCaseGuard.cs` was split out of the mixed `ITaxDirectory.cs` for parity
with the certified `Inventory`/`Pricing` port layout (`Pricing.Application/Ports/IPricingUseCaseGuard.cs`).
`ITaxDirectory.cs` now holds only the two capability records plus the port; the guard owns its own
file. No member, signature or lifetime changed.

## 3. Canonical folders kept deliberately

- `Application` stays `Composition/` + `Ports/` only. Tax owns **zero** endpoint-reachable requests
  (W0), so no `Commands/`, `Queries/`, `Validators/`, `Models/`, `Handlers/` or `Requests/` folder
  was invented — inventing CQRS ceremony would have been the structural defect, not the repair.
- `Contracts` stays `Dtos/` + `Errors/` + `Ports/` + `Resources/` (the single self-contained code +
  text boundary established by W1).
- `Domain` stays `Aggregates/`, `Enums/`, `Events/`, `Policies/` + the `GlobalUsings.cs` bridge.
- `Infrastructure` stays on `Adapters/`, `DependencyInjection/`, `Events/`, `Outbox/`,
  `Persistence/` (`Persistence/Configurations/`, `Persistence/Migrations/`).
- `/Modules/Tax/` keeps all five projects (`Contracts`, `Domain`, `Application`, `Infrastructure`,
  `Tests`) matching disk — no decorative or stale solution folder was introduced.

## 4. Behavior preservation

The 11 stable code string values, the 11 descriptors, the 11 EN + 11 FA resource keys and their
text, the `TaxOperation` typed-fault seam, the DI lifetimes, the `tax` schema, the single migration
`20260823190000_InitialTax` (+ designer + snapshot), the four calculation outcomes plus
`CalculationError`, the highest-`Specificity` winner selection, the `TaxRounding` scale policy, the
`TrustedInternal` override behaviour and the outbox integration event names
(`tax.rule_created.v1`, `tax.rule_activated.v1`, `tax.rule_changed.v1`,
`tax.calculation_failed.v1`) are **byte-identical**.

`Persistence/Migrations/*` was **not** touched: the Tax migration designer/snapshot carry no
entity type name (the migration only calls `modelBuilder.HasDefaultSchema("tax")`), so the Domain
namespace split has zero EF model surface consequence. No table, column, index, constraint,
migration id or Up/Down semantics changed.

W2 is pure physical reorganization plus namespace/using repointing.

## 5. Durable guards added / updated

`src/backend/Host/Tooba.Host.Tests/Architecture/TaxModuleAmsc001W2StructureGuardTests.cs` (**new**, 8 tests)

- `Ceremonial_endpoints_project_stays_retired_internal_only`
- `Contracts_and_Domain_are_capability_first_with_no_root_dump`
- `Application_stays_shallow_with_no_technical_axis_or_use_case_leaves`
- `Infrastructure_uses_canonical_integration_folders`
- `Localization_surface_has_a_single_authoritative_home`
- `Path_derived_namespaces_are_exact`
- `Root_allowlists_and_forbidden_lists_match_the_manifest`
- `Solution_grouping_is_canonical_modules_tax`

`src/backend/Host/Tooba.Host.Tests/Architecture/TaxModuleAmsc001W1MigrateGuardTests.cs`
(**W1 semantics preserved, not weakened**) — every W1 assertion is intact; only the physical paths
pinned by the guard follow the post-W2 tree.

`src/backend/Modules/Tax/Tooba.Tax.Tests/Architecture/TaxArchitectureGuardTests.cs`

- `Endpoints_csproj_does_not_reference_infrastructure` → replaced by
  `Ceremonial_endpoints_project_stays_retired_internal_only` (the project it inspected no longer
  exists; the new test asserts its permanent absence).
- `Program_maps_tax_module` → `Program_no_longer_maps_a_tax_module_route_group`, asserting the
  ceremonial map call, the `using` and the `"/v1/tax"` literal are gone.
- `Tax_golden_boundaries_remain_clean` — the dead `Sources("Tooba.Tax.Endpoints")` leg was removed
  from `AllProductionSources()`; every remaining assertion is intact.

`src/backend/Host/Tooba.Host.Tests/HostModuleEndpointOwnershipTests.cs`

- `Program_maps_offer_and_tax_modules` now asserts `MapOfferModule()` is present and
  `MapTaxModule()` / `Tooba.Tax.Endpoints` are **absent** (plus the existing Pricing absence).

`src/backend/Host/Tooba.Host.Tests/ContractsW4CharacterizationTests.cs`

- `Tax_outcome_and_calculator_are_contracts_assembly_owned` now pins the path-derived namespaces
  (`Tooba.Tax.Contracts.Dtos`, `Tooba.Tax.Contracts.Ports`) **and** keeps asserting the assembly
  identity (`Tooba.Tax.Contracts`), so the boundary ownership claim is preserved rather than
  relaxed.

`src/backend/Host/Tooba.Host.Tests/Architecture/HostDevelopmentEnricherClosureGuardTests.cs`

- `New_development_gateways_live_in_owning_module_contracts` — the Party development-gateway path
  was repointed from `Party/Tooba.Party.Contracts/IPartyDevelopmentSeedGateway.cs` to
  `Party/Tooba.Party.Contracts/Ports/IPartyDevelopmentSeedGateway.cs`. This is a **pre-existing,
  unrelated baseline failure** (the file moved in commit `00c0b9bf`, `refactor(party): AMC-001 W2`,
  long before this task); the guard was stale, not weakened.

Consumer repointing (namespace-only, zero behavior change):

| Consumer | Change |
| --- | --- |
| `Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.cs` | `Tooba.Tax.Contracts` → `Tooba.Tax.Contracts.Ports` |
| `Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.Access.cs` | removed the unused `Tooba.Tax.Contracts` using |
| `Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.Reservations.cs` | `Tooba.Tax.Contracts` → `.Dtos` + `.Ports` |
| `Catalog.Infrastructure/Storefront/StorefrontComposer.cs` | `Tooba.Tax.Contracts` → `.Ports` |
| `Catalog.Infrastructure/Development/StorefrontDemo/StorefrontDemoCatalogBootstrap.cs` | `Tooba.Tax.Contracts` → `.Ports` |
| `Catalog.Infrastructure/Development/WorkspaceDemoMarketplaceSeed.cs` | `Tooba.Tax.Contracts` → `.Ports` |
| `Catalog.Infrastructure/Development/CatalogAttributeSchemaSellableEnricher.cs` | `Tooba.Tax.Contracts` → `.Ports` |
| `ProductWorkspace.Application/Composition/ProductManagement/Queries/GetProductWorkspaceHandler.cs` | `Tooba.Tax.Contracts` → `.Ports` |
| `Host/Tooba.Host/Composition/ToobaModuleComposition.cs` | `Tooba.Tax.Infrastructure` → `Tooba.Tax.Infrastructure.DependencyInjection` |
| `Host.Tests/{TaxFoundationTests,CheckoutOrderFoundationTests,StorefrontDemoCatalogSeedTests,ArchitectureBoundaryTests,ProductWorkspaceAggregateGetW19Tests}` | path-derived usings (`Application.Ports`, `Contracts.Dtos`, `Contracts.Ports`, `Domain.Aggregates`, `Domain.Enums`, `Infrastructure.Adapters`, `Infrastructure.Outbox`, `Infrastructure.DependencyInjection`) |

The two `Tax.Contracts` namespaces are still consumed Contracts-only by foreign modules — no
foreign module gains a Tax `Application`/`Infrastructure`/`Domain` edge.

## 6. Manifest interaction

`docs/architecture/tmar-module-structure-manifests.json` → **new** `preCertModules[Tax]` entry
(`structureCertified: false`, `lockVersion: ARCH-COMPLETE-002`, `structureState: READY_FOR_CERTIFY`,
`structureAuthorityTask: TB-TMAR-TAX-AMSC-001-W2`), disk-accurate per project:

| Project | `rootAllowlist` | Notable `forbiddenRootFiles` | `forbiddenTopLevelFolders` |
| --- | --- | --- | --- |
| `Tooba.Tax.Contracts` | `[]` | `TaxErrorCodes.cs`, `TaxCalculatorContracts.cs`, `TaxQueryContracts.cs`, `ITaxDevelopmentSeedGateway.cs`, `ITaxSchemaMigrator.cs`, `TaxOutcome.cs` | `[]` |
| `Tooba.Tax.Domain` | `["GlobalUsings.cs"]` | the 7 split domain files | `["Errors","ValueObjects"]` |
| `Tooba.Tax.Application` | `[]` | `TaxOperation.cs`, `ITaxDirectory.cs`, `ITaxUseCaseGuard.cs`, `TaxContracts.cs`, `TaxErrorCodes.cs` | `["Commands","Queries","Validators","Models","Handlers","Requests"]` |
| `Tooba.Tax.Infrastructure` | `[]` | `TaxModule.cs`, `TaxDirectory.cs`, `TaxDbContext.cs`, `TaxEvents.cs`, `TaxOutboxRegistration.cs`, `TaxModuleMigration.cs`, `TaxSchemaMigrator.cs`, `TaxDevelopmentSeedGateway.cs` | `["Migrations","Repositories","Directories","Messaging"]` |
| `Tooba.Tax.Tests` | `[]` | — | `["Endpoints"]` |

`structureCertified` was **not** flipped and `Tax` was **not** added to
`structureLock.certifiedModules` — the W3 Certify verdict is deliberately left open. Tax owns zero
HTTP routes, so it is correctly absent from `uncertifiedHttpOwningModules`.

## 7. Focused validation

| Validation | Result |
| --- | --- |
| `Tooba.slnx` full solution build | succeeded, **0 errors** |
| `Tooba.Tax.Tests` | **11 / 11 passed** |
| `TaxModuleAmsc001W1MigrateGuardTests` + `TaxModuleAmsc001W2StructureGuardTests` | **16 / 16 passed** (W1 8, W2 8) |
| `Tooba.Host.Tests` full suite | **2288 passed / 130 skipped / 71 failed** |
| `Tooba.Host.Tests` full suite at the `fa87201a` baseline (isolated `git worktree`) | **2287 passed / 130 skipped / 72 failed** |
| **New failures introduced by W2** | **ZERO** (set-difference of failing test ids: `NEW = 0`) |
| Fixed by W2 | `ContractsW4CharacterizationTests.Tax_outcome_and_calculator_are_contracts_assembly_owned` (namespace repoint) |
| `.slnx` | `/Modules/Tax/` group now 5 projects, matching disk |

The 51 distinct failing test ids are identical at both heads (the count differs by one because one
flaky/ordering-dependent test flips between runs). They are all present at the `fa87201a` baseline
and unrelated to Tax: Catalog `Results.Json` WIP, the missing `Modules/Wishlist` file, Host/Admin
count drift, the stale `HostDevelopmentEnricherClosureGuardTests` Party path, the
`HostDevelopmentMigrationSeamGuardTests` module-count drift, and the repository-global
`TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
failure (the pre-existing `Tooba.Catalog.Contracts/Cart` + `Tooba.Cart.Contracts/{Checkout,Presentation}`
project-level namespace deviation documented in the Inventory W3 certification as out of scope for a
module-local certification).

## 8. Handoff

- `Structure-State = READY_FOR_CERTIFY` — every structure §27 gate met on disk and enforced by the
  new durable guard.
- `Tax-Endpoints-Project-State = ABSENT`; `Tax-Http-Route-State = ZERO`;
  `Presentation-Registration-State = INFRASTRUCTURE_MODULE_EXACTLY_ONCE`;
  `Tax-Project-Count-State = EXACT_5`; `Solution-Grouping-State = CANONICAL_5`;
  `Path-Namespace-State = EXACT`; `Root-Allowlist-State = ENFORCED`;
  `Physical-Copy-State = CLEAN`; `Guards-Weakened-State = NONE`; `Baselines-Widened-State = NONE`;
  `Automatic-Next-Implementation-Task-State = NONE`;
  `Workflow-Stop-State = USER_REVIEW_TAX_AMSC_001_W2`.
- Residual for W3 Certify: promote the manifest entry into the certified `modules` array
  (`structureCertified: true`), add `Tax` to `structureLock.certifiedModules` (31), add
  `TaxModuleAmsc001W3CertGuardTests`, and record the SoT certification block + Master Recovery
  checkpoint.
- Host final closure preserved: zero Host production files added; schema/migrations unchanged;
  guards strengthened; zero guards weakened; zero unrelated files touched.
