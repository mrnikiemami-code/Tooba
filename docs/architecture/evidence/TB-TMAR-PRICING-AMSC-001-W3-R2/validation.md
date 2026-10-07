# TB-TMAR-PRICING-AMSC-001-W3-R2 — validation

- Module: `Pricing`
- Starting HEAD: `2e664bb336f45b8304818b7e5754f9b0fc364f20` (branch `main`, `HEAD == origin/main`)
- Baseline worktree: `../SarvNewVer-w3r2-base` pinned to `2e664bb3` (isolated `git worktree`, removed after use)

## 1. Diff scope proof

`git diff --name-status` (unrelated pre-existing untracked evidence artifacts excluded):

```text
M  docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
M  docs/architecture/tmar-current-state.json
M  docs/architecture/tmar-module-structure-manifests.json
M  src/backend/Host/Tooba.Host.Tests/Architecture/PricingModuleAmsc001W1MigrateGuardTests.cs
M  src/backend/Host/Tooba.Host.Tests/Architecture/PricingModuleAmsc001W2StructureGuardTests.cs
R  src/backend/Host/Tooba.Host.Tests/Architecture/PricingModuleAmsc001W3CertGuardTests.cs
-> src/backend/Host/Tooba.Host.Tests/Architecture/PricingModuleAmsc001W3R2RepairGuardTests.cs
M  src/backend/Host/Tooba.Host.Tests/Architecture/TmarCompleteReferenceStructureGateTests.cs
M  src/backend/Host/Tooba.Host.Tests/HostModuleEndpointOwnershipTests.cs
M  src/backend/Host/Tooba.Host/Program.cs
M  src/backend/Host/Tooba.Host/Tooba.Host.csproj
D  src/backend/Modules/Pricing/Tooba.Pricing.Endpoints/PricingEndpointModule.cs
D  src/backend/Modules/Pricing/Tooba.Pricing.Endpoints/Tooba.Pricing.Endpoints.csproj
M  src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/DependencyInjection/PricingModule.cs
M  src/backend/Modules/Pricing/Tooba.Pricing.Tests/Architecture/PricingArchitectureGuardTests.cs
D  src/backend/Modules/Pricing/Tooba.Pricing.Tests/Endpoints/PricingEndpointModuleTests.cs
M  src/backend/Modules/Pricing/Tooba.Pricing.Tests/Tooba.Pricing.Tests.csproj
M  src/backend/Tooba.slnx
```

Every entry is Pricing, Host-Pricing-composition, Pricing guards, `Tooba.slnx` or Pricing SoT/manifest.
No unrelated module file was touched. No file outside the task's `ALLOWED FILES` was modified.

## 2. Machine-checked structure audit

`node docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R2/audit.cjs`

```json
{
  "module_cs_files": 38,
  "endpoint_project_dir_exists": false,
  "endpoint_test_dir_exists": false,
  "production_Tooba_Pricing_Endpoints": 0,
  "production_MapPricingModule": 0,
  "production_AddPricingEndpointPresentation": 0,
  "production_v1_pricing_literal": 0,
  "production_MapGet": 0,
  "production_MapPost": 0,
  "production_MapPut": 0,
  "production_MapDelete": 0,
  "production_MapPatch": 0,
  "production_MapGroup": 0,
  "production_ISender": 0,
  "production_IEndpointRouteBuilder": 0,
  "host_Tooba_Pricing_Endpoints": 0,
  "host_MapPricingModule": 0,
  "host_AddPricingEndpointPresentation": 0,
  "host_v1_pricing_literal": 0,
  "host_csproj_Pricing_Endpoints_ref": 0,
  "host_csproj_Pricing_Infrastructure_ref": 1,
  "registration_catalog_contributor": 1,
  "registration_resource_set": 1,
  "contracts_catalog_contributor_classes": 1,
  "slnx_pricing_project_entries": 5,
  "slnx_pricing_has_endpoints": false,
  "stable_codes": 11,
  "descriptor_factories": 11,
  "descriptor_ctor_sites": 1,
  "en_resource_keys": 11,
  "fa_resource_keys": 11,
  "en_fa_key_sets_identical": true,
  "knowncodes_guard": true,
  "isknown_guard": true,
  "migration_files": [
    "20260823085546_InitialPricing.Designer.cs",
    "20260823085546_InitialPricing.cs",
    "PricingDbContextModelSnapshot.cs"
  ]
}
```

## 3. Assertion matrix (task PASS criteria)

| Assertion | Result |
| --- | --- |
| `Tooba.Pricing.Endpoints` production references / files | `ZERO` (0 hits, directory absent) |
| `AddPricingEndpointPresentation` production references | `ZERO` |
| `MapPricingModule` production references | `ZERO` |
| `"/v1/pricing"` production route mapping | `ZERO` |
| Pricing project directories | exactly 5 (`Contracts`, `Domain`, `Application`, `Infrastructure`, `Tests`) |
| `/Modules/Pricing/` `.slnx` entries | exactly **5** |
| `PricingErrorCatalogContributor` class declarations | exactly **1** |
| `PricingErrorResourceSet` class declarations | exactly **1** |
| DI registration of `IErrorCatalogContributor` through `PricingModule` | exactly **1** |
| DI registration of `IErrorResourceSet` through `PricingModule` | exactly **1** |
| Stable codes / descriptors / EN keys / FA keys | **11 / 11 / 11 / 11** (unchanged) |
| Pricing migration file identities | unchanged (single `20260823085546_InitialPricing` + designer + snapshot) |
| Pricing HTTP routes / groups | **0** |
| Host Pricing Endpoints reference | **0** |
| Host Pricing Infrastructure reference | preserved (1) |
| `path↔namespace` for 4 production projects | `EXACT` |
| `foreignAppInfraDomainCoupling` | `ZERO` |
| Manifest `modules[]` certified count | **25** (Pricing removed; was 26) |
| Manifest `preCertModules` | `ProductWorkspace`, `Pricing` (`structureCertified: false`, `READY_FOR_CERTIFY`) |
| `structureLock.certifiedModules` | **24** members; `Pricing` absent |
| Global Host checkpoint | preserved (`TB-TMAR-HOST-ROOT-FINAL-CERT-001` / `HOST_ROOT_FINAL_CERTIFIED`) |
| `automaticNextImplementationTask` | `NONE` |

## 4. Focused test runs

### 4.1 `Tooba.Pricing.Tests`

```text
Passed!  - Failed: 0, Passed: 13, Skipped: 0, Total: 13, Duration: 965 ms
```

### 4.2 Host Pricing / ownership / structure-gate guards

Filter: `FullyQualifiedName~Pricing | ~ErrorCatalogUniqueCode | ~HostModuleEndpointOwnership | ~TmarCompleteReferenceStructureGate`

```text
Failed!  - Failed: 2, Passed: 43, Skipped: 1, Total: 46
```

Both failures are **baseline** failures, identical on the `2e664bb3` worktree run
(`Failed: 2, Passed: 43, Skipped: 1, Total: 46`):

| Failure | Cause | Relation to W3-R2 |
| --- | --- | --- |
| `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment` | repository-global `Tooba.Catalog.Contracts/Cart` path↔namespace deviation (`Expected: "Tooba.Catalog.Contracts.Cart"`, `Actual: "Tooba.Catalog.Contracts"`) | pre-existing, explicitly out of scope for a module-local wave; unchanged by W3-R2 |
| `TaxFoundationTests.Pricing_and_catalog_do_not_own_tax_amounts_and_outcomes_stay_distinct` | missing `Modules/Tax/Tooba.Tax.Domain/TaxDomain.cs` | pre-existing, unrelated to Pricing |

All 8 Pricing guard suites pass: `PricingModuleAmsc001W1MigrateGuardTests`,
`PricingModuleAmsc001W2StructureGuardTests`, `PricingModuleAmsc001W3R2RepairGuardTests`,
`PricingArchitectureGuardTests`, `PricingErrorCatalogTests`, `ErrorCatalogUniqueCodeGuardTests`,
`HostModuleEndpointOwnershipTests`, `PricingFoundationTests` (except its baseline Postgres skip).

### 4.2b Pricing AMSC guard suites (isolated, green)

Filter: `FullyQualifiedName~PricingModuleAmsc001W3R2RepairGuardTests | ~PricingModuleAmsc001W2StructureGuardTests | ~PricingModuleAmsc001W1MigrateGuardTests | ~HostModuleEndpointOwnershipTests`

```text
Passed!  - Failed: 0, Passed: 31, Skipped: 0, Total: 31, Duration: 85 ms
```

| Suite | Tests | Result |
| --- | --- | --- |
| `PricingModuleAmsc001W1MigrateGuardTests` | 9 | passed |
| `PricingModuleAmsc001W2StructureGuardTests` | 9 | passed |
| `PricingModuleAmsc001W3R2RepairGuardTests` | 9 | passed |
| `HostModuleEndpointOwnershipTests` | 4 | passed |

### 4.3 Full `Tooba.Host.Tests` suite — new-failure proof

| Run | Result |
| --- | --- |
| Current tree (W3-R2) | `Failed: 79, Passed: 2036, Skipped: 130, Total: 2245` |
| Baseline `2e664bb3` (isolated worktree) | `Failed: 79, Passed: 2036, Skipped: 130, Total: 2245` |
| **New failures introduced by W3-R2** | **ZERO** (set-difference of failing test ids: empty both directions) |
| Failures fixed by W3-R2 | none |
| Guards weakened / baselines widened | **NONE** |

The `R100` rename of `PricingModuleAmsc001W3CertGuardTests` → `PricingModuleAmsc001W3R2RepairGuardTests`
keeps the suite count identical (rename, not deletion); its assertions were rewritten to the repaired
truth rather than relaxed.

### 4.4 Solution build

```text
dotnet build src/backend/Tooba.slnx
    36 Warning(s)
    0 Error(s)
```

No project in `Tooba.slnx` fails to load, and no dangling `Tooba.Pricing.Endpoints` reference remains.

## 5. Bounded-validation compliance

- No repository-global unrelated failing guard was required to turn green; the two remaining focused
  failures are proven identical at the baseline.
- `git diff --name-status` proves the change surface is exactly the bounded repair.
- All four required evidence documents exist in
  `docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R2/`:
  `structure-repair.md`, `project-inventory-before-after.md`, `endpoint-applicability-audit.md`,
  `validation.md`.

## 6. Verdict

`Structure-State = READY_FOR_CERTIFY`; `Guards-Weakened-State = NONE`;
`Baselines-Widened-State = NONE`; `Behavior-Preservation-State = PRESERVED`;
`Schema-Migration-State = UNCHANGED`; `Evidence-State = COMPLETE`;
`Automatic-Next-Implementation-Task-State = NONE`;
`Workflow-Stop-State = USER_REVIEW_PRICING_AMSC_001_W3_R2`.
