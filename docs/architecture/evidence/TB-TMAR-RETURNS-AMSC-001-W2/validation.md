# Validation (section 25 — focused, no broad solution-wide runs)

Baseline for every command below: `main` @ `0a573864` (W1 Migrate), `HEAD == origin/main`, clean tree.

## 1. Structure-specific durable guard (new in W2)

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj \
  --filter "FullyQualifiedName~ReturnsModuleAmsc001W2StructureGuardTests"

Passed!  - Failed: 0, Passed: 7, Skipped: 0, Total: 7, Duration: 21 ms - Tooba.Host.Tests.dll (net8.0)
```

| Test | Locks |
|---|---|
| `Application_is_capability_first_shallow_without_technical_axis_roots` | `Application/{Composition,ReturnRequests,Validation}` exactly; 8 retired technical-axis roots absent; `ReturnRequests/{Commands,Models,Ports,Queries}` exactly |
| `No_single_file_use_case_leaf_folders_remain_in_the_request_axes` | 0 subfolders under `ReturnRequests/Commands` and `ReturnRequests/Queries` |
| `Path_namespace_alignment_is_exact_for_every_production_source` | 71 production files, 0 mismatches, locked migration exemptions |
| `Root_allowlists_match_disk_and_forbidden_entries_are_absent` | manifest ↔ disk equality + forbidden files/folders absent |
| `Manifest_records_returns_as_pre_cert_not_certified` | `uncertifiedHttpOwningModules` without Returns; `structureCertified == false`; 6 projects; `modules[]` still has no Returns |
| `Solution_explorer_grouping_is_canonical` | `/Modules/Returns/` block with all six projects |
| `No_stale_or_duplicate_physical_copy_of_moved_files_remains` | 13 retired paths absent |

## 2. Module test suite (affected by the W1 moves W2 verifies)

```text
dotnet test src/backend/Modules/Returns/Tooba.Returns.Tests/Tooba.Returns.Tests.csproj

Passed!  - Failed: 0, Passed: 16, Skipped: 0, Total: 16, Duration: 916 ms - Tooba.Returns.Tests.dll (net8.0)
```

Covers `Architecture/ReturnsArchitectureGuardTests`, `Behavior/ReturnsCharacterizationTests`,
`Behavior/ReturnsErrorAndGridTests`, `Behavior/ReturnsSemanticPresentationTests`,
`Endpoints/ReturnsEndpointOwnershipTests`. Behavior is unchanged by the structural wave.

## 3. W1 migrate guard (no regression)

```text
--filter "FullyQualifiedName~ReturnsModuleAmsc001"
```

`ReturnsModuleAmsc001W1MigrateGuardTests` (12 tests) + `ReturnsModuleAmsc001W2StructureGuardTests`
(7 tests) → all pass.

## 4. Error-catalog guards

`ErrorCatalog*` guards → pass (no localization/error-code surface was touched by W2).

## 5. Pre-existing (NOT introduced by W2) failures — baseline comparison

```text
--filter "FullyQualifiedName~TmarDurableGuardTests"
W2 working tree:  Failed: 2, Passed: 4, Total: 6
HEAD (stashed):   Failed: 2, Passed: 4, Total: 6
```

Identical by exact test name in both runs — **pre-existing at `0a573864`, unrelated to this wave**:

| Test | Failure | Nature |
|---|---|---|
| `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | `structureLock.certifiedModules` in `docs/architecture/tmar-current-state.json` lists 16 modules while the manifest already records 28 certified modules | stale SoT snapshot |
| `TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative` | expected checkpoint sub-string absent from the SoT | stale SoT checkpoint |

Both belong to the `docs/architecture/tmar-current-state.json` recovery block, which W2 is **not**
authorized to rewrite (section 23: structure prepares for Certify; it does not replace Certify). They
are recorded here as the honest baseline and are the responsibility of the W3 `tooba-architecture-certify`
wave's SoT block.

W2 introduced **zero** new failures: 2 → 2, identical names.

## 6. Build

`Tooba.Host.Tests` (which references the full backend graph including all six Returns projects) builds
clean for the test runs above; the only diagnostics are pre-existing `xUnit2013`/`CS8625` warnings in
unrelated files. No new warning or error originates from `src/backend/Modules/Returns`.

## 7. Path ↔ namespace script

```text
node .git/pn-check-returns.js
production .cs checked: 71
locked exemptions (Migrations/Designer/Snapshot): 5
mismatches: 0
```

## 8. Manifest parse check

```text
keys: version, definitionMarker, rules, modules, uncertifiedHttpOwningModules, preCertModules
modules: 28 entries (Returns absent)
preCert: Returns
uncertified: Support, Wallet
```

Valid JSON, unchanged key set, `92 insertions(+), 2 deletions(-)`.

## 9. Verdict

| Gate | Result |
|---|---|
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` |
| Path-Namespace-State | `EXACT` |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |
| Host final closure | `PRESERVED` |
| Focused structure guards | PASS (7/7) |
| Module + W1 guards | PASS |
| New failures vs baseline | 0 |

**Structure-State = `READY_FOR_CERTIFY`** → hand off to `tooba-architecture-certify` (W3).
