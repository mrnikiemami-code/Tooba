# Validation (skill §25 — focused, no broad solution-wide runs)

Baseline for every command below: `main` @ `4ca4aafc` (W1 Migrate), `HEAD == origin/main`, known/safe
working tree.

## 1. Structure-specific durable guard

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj \
  --filter "FullyQualifiedName~SettlementModuleAmsc001W2StructureGuardTests"

Passed!  - Failed: 0, Passed: 9, Skipped: 0, Total: 9, Duration: 56 ms - Tooba.Host.Tests.dll (net8.0)
```

| Test | Locks |
|---|---|
| `Application_is_capability_first_shallow_without_technical_axis_roots` | `Application/{Composition,Payouts,Validation}` exactly; 8 retired technical-axis roots absent; `Payouts/{Commands,Models,Ports,Queries}` exactly |
| `No_single_file_use_case_leaf_folders_remain_in_the_request_axes` | 0 subfolders under `Payouts/Commands` and `Payouts/Queries`; `Validation/` flat with exactly its 2 files |
| `Path_namespace_alignment_is_exact_for_every_production_source` | 66 production files, 0 mismatches, locked migration exemptions |
| `No_namespace_alias_workaround_or_global_using_file_remains` | 0 `GlobalUsings*.cs`, 0 `TypeForwardedTo` |
| `Root_allowlists_match_disk_and_forbidden_entries_are_absent` | manifest ↔ disk equality + forbidden files/folders absent |
| `Solution_explorer_grouping_is_canonical` | `/Modules/Settlement/` block with all six projects |
| `No_stale_or_duplicate_physical_copy_of_moved_files_remains` | 21 retired Application paths + 2 retired Infrastructure paths absent |
| `Duplicate_ownership_of_the_same_responsibility_does_not_exist` | 9 shared types + 10 CQRS requests each with exactly one physical home |
| `Module_is_certified_and_absent_from_uncertified_lists` | `structureCertified == true`, `lockVersion == ARCH-COMPLETE-002`, absent from `uncertifiedHttpOwningModules` / `preCertModules` |

## 2. Module test suite (affected by the W1 moves W2 verifies)

```text
dotnet test src/backend/Modules/Settlement/Tooba.Settlement.Tests/Tooba.Settlement.Tests.csproj

Passed!  - Failed: 0, Passed: 30, Skipped: 0, Total: 30, Duration: 501 ms - Tooba.Settlement.Tests.dll (net8.0)
```

Covers `Architecture/SettlementArchitectureGuardTests`,
`Architecture/SettlementValidatorCoverageGuardTests`, `Behavior/SettlementErrorAndGridTests`,
`Endpoints/SettlementEndpointOwnershipTests`, `Validation/SettlementValidatorTests`. Behavior is
unchanged by the structural wave.

## 3. W1 migrate guard (no regression)

```text
--filter "FullyQualifiedName~SettlementModuleAmsc001"
```

`SettlementModuleAmsc001W1MigrateGuardTests` (12) + `SettlementModuleAmsc001W2StructureGuardTests` (9)
→ all pass.

## 4. Repository-global structure gate

```text
--filter "FullyQualifiedName~TmarCompleteReferenceStructureGateTests"
```

Result: `3 passed / 1 failed`. The single failure is
`Certified_modules_satisfy_root_allowlists_and_namespace_alignment`, caused by the pre-existing
`Tooba.Catalog.Contracts.Cart` namespace deviation that the test's own Inventory note documents as out
of scope for a module-local certification. Reproduced identically at the wave-start baseline `4ca4aafc`
(and at `bac4dbe3`) — **not introduced by this wave**.

## 5. Pre-existing (NOT introduced by W2) failures — baseline comparison

```text
--filter "FullyQualifiedName~Settlement"
```

`1 failed / 31 passed / 2 skipped`, the single failure being
`PaidProjectionFinancialTests.Settlement_and_initiate_exclude_store_shipping_from_seller_ids` — its
`Read(...)` helper expects Order/Inventory source paths that do not exist at `HEAD`. Identical failure
count and name at the wave-start baseline `4ca4aafc` and at `bac4dbe3` (recorded in the W1 evidence
§12). Unrelated to Settlement structure.

Also pre-existing and reproduced at the baseline (recorded in `TB-TMAR-RETURNS-AMSC-001-W2/validation.md`
and W1 §12 for the same repository state):

- `TmarSourceSizeAndInfraAppTests` (3 of 6) — stale repository-wide source-size baseline (13 entries
  point at files that no longer exist, one of them Settlement's pre-split `SettlementDomain.cs`) plus a
  stale Infrastructure→foreign-Application edge baseline.
- `TmarDurableGuardTests` recovery pins (2) — stale `structureLock.certifiedModules` count and a stale
  recovery checkpoint substring in `docs/architecture/tmar-current-state.json`. This wave's SoT edit
  touches the Settlement certification block only; the repository-global recovery pins are outside a
  module-local structure wave's authorized scope and are left for the certify wave's SoT block.

W2 introduced **zero** new failures.

## 6. Build

```text
dotnet build src/backend/Tooba.slnx

Build succeeded.
    0 Error(s)
```

The only diagnostics are pre-existing warnings in unrelated modules
(`ReturnsErrorAndGridTests` CS8625, `PaymentModuleAmsc001W2StructureGuardTests` /
`PaymentArchitectureGuardTests` CS8602). No new warning or error originates from
`src/backend/Modules/Settlement`.

## 7. Path ↔ namespace script

```text
node .git/pn-check-settlement.js

production .cs checked: 66
locked exemptions (Migrations/Designer/Snapshot): 3
mismatches: 0
per project: {"Tooba.Settlement.Contracts":8,"Tooba.Settlement.Domain":14,
              "Tooba.Settlement.Application":21,"Tooba.Settlement.Infrastructure":18,
              "Tooba.Settlement.Endpoints":5}
```

## 8. Manifest / SoT parse check

```text
node -e "JSON.parse(fs.readFileSync('docs/architecture/tmar-module-structure-manifests.json'))"  → OK
node -e "JSON.parse(fs.readFileSync('docs/architecture/tmar-current-state.json'))"                → OK

modules: 29 entries (Settlement present exactly once, with certificationNote)
preCert: []            uncertified: ["Support","Wallet"]
Settlement projects recorded: Application, Endpoints, Infrastructure (3, unchanged)
```

## 9. Verdict

| Gate | Result |
|---|---|
| Module Applicability Gate | `HTTP_OWNING` |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` |
| Path-Namespace-State | `EXACT` |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |
| File-Cohesion-State | `COHESIVE` (`OVERSIZED_ONLY` watch, non-blocking) |
| Host final closure | `PRESERVED` |
| Focused structure guards | PASS (9/9) |
| Module + W1 guards | PASS (30/30, 12/12) |
| Solution build | 0 errors |
| New failures vs baseline | 0 |

**Structure-State = `READY_FOR_CERTIFY`** → hand off to `tooba-architecture-certify` (W3).
