# Validation — AccessControl (W2)

## Focused validation (no broad solution-wide test run)

| Check | Command | Result |
| --- | --- | --- |
| New W2 guard + all AccessControl-scoped guards + module tests | `dotnet test … --filter "FullyQualifiedName~AccessControl"` | `Passed! Failed: 0, Passed: 42, Skipped: 4, Total: 46` |
| Solution build | `dotnet build src/backend/Modules/AccessControl/…` | `0 Error(s)` |
| Manifest JSON parse | `JsonDocument.Parse` inside the new guard | OK |
| `.slnx` parse / entry resolution | new guard resolves every declared `.csproj` on disk | OK |
| Production diff | `git diff --stat` over `src/backend/Modules/AccessControl` | ZERO |

The 4 skipped tests are the pre-existing Testcontainers-gated persistence tests; they are skipped
by design in this environment and are unrelated to structure.

### New W2 guard facts (all pass)

`AccessControlManifestDiskReconciliationGuardTests`:

1. `AccessControl_manifest_has_exactly_one_entry_certified_under_arch_complete_002`
2. `AccessControl_manifest_represents_exactly_the_five_production_projects`
3. `AccessControl_manifest_root_allowlists_equal_real_disk_root_cs_files`
4. `AccessControl_solution_folder_references_exactly_the_declared_production_projects`
5. `AccessControl_application_has_no_technical_axis_first_request_root`
6. `AccessControl_has_no_unjustified_single_file_request_or_use_case_leaf_folder`
7. `AccessControl_production_paths_match_namespaces_exactly`

### AccessControl-scoped guard suite (all pass)

```text
AccessControlFoundationTests
AccessControlRuntimeScopeTests
AccessControlValidatorTests
AccessControlModuleAmc002W2CertGuardTests
AccessControlModuleAmcW1SolutionGuardTests
AccessControlModuleAmcW2SemanticGuardTests
AccessControlModuleAmcW3ResultGuardTests
AccessControlModuleAmcW4StructureGuardTests
AccessControlModuleAmcW5CertGuardTests
AccessControlModuleAmsc001W1MigrateGuardTests
AccessControlManifestDiskReconciliationGuardTests   (new, W2)
AccessControlStructureRecert001GuardTests
AccessControlStructureRepair001GuardTests
```

## Pre-existing repo-wide guard failures (out of scope, proven)

| Guard | `a3ba1a4f` (W0 baseline) | `c9e009f8` (W1 tip) | W2 working tree |
| --- | --- | --- | --- |
| `TmarCompleteReferenceStructureGateTests` (3 facts) | `Failed: 3 / 3` | `Failed: 3 / 3` | `Failed: 3 / 3` |
| `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | pre-existing | pre-existing | pre-existing |

Proof method: `git stash push -u` → `git checkout a3ba1a4f` → run filtered tests → restore. The
failures reproduce identically before any AccessControl AMSC work existed, so they are **not**
regressions from W0, W1 or W2.

### Exact pre-existing defects (not repaired)

| # | Defect | Owner | Evidence |
| --- | --- | --- | --- |
| 1 | Gate expects 21 manifest modules; manifest declares 22 (`Catalog` absent from expectation) | shared gate | `TmarCompleteReferenceStructureGateTests.cs:27`, `:119` |
| 2 | `Tooba.Catalog.Contracts/Cart/CatalogCartQuantityContracts.cs` and `Cart/ICatalogCartPresentationLookup.cs` declare `Tooba.Catalog.Contracts` instead of `Tooba.Catalog.Contracts.Cart` | `Catalog` module | `TmarCompleteReferenceStructureGateTests.cs:154` |
| 3 | Gate expects 16 `structureLock.certifiedModules`; SoT holds 21 | shared gate / SoT | `TmarDurableGuardTests.cs:125-132` |

Each lies in the shared structure gate, the `Catalog` module, or the repo-wide SoT — none is on
the AccessControl surface. Repairing them would exceed this task's authorized scope and would
modify unrelated files, which the governance rules forbid. Reported for Architect decision.

## Behavior preservation

| Aspect | Result |
| --- | --- |
| Production `.cs` changed | 0 |
| `.csproj` changed | 0 |
| `Tooba.slnx` changed | 0 |
| Routes / status codes / error codes changed | NONE |
| DTO shape changed | NONE |
| Schema / migrations changed | NONE |
| Host production tree changed | 0 (final closure preserved) |

## Source of truth

```text
branch = main
pre-W2 HEAD == origin/main (c9e009f8)
working tree = only W2 evidence + W2 guard + SoT record (no production change)
```
