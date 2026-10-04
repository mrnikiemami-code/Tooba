# Focused validation — AccessControl (W3)

## Focused tests

| Suite | Command | Result |
| --- | --- | --- |
| AccessControl-scoped (module + all guards, incl. the new W3 cert guard) | `dotnet test … --filter "FullyQualifiedName~AccessControl"` | `Passed! Failed: 0, Passed: 49, Skipped: 4, Total: 53` |
| AccessControl + source-size guard | `dotnet test … --filter "FullyQualifiedName~AccessControl\|FullyQualifiedName~TmarSourceSize"` | `Failed: 3` — all 3 are pre-existing repo-wide (see below); all AccessControl facts pass |
| Solution build (all 5 module projects) | `dotnet build` via the test project restore/build | `0 Error(s)` |

The 4 skipped tests are the pre-existing Testcontainers-gated persistence tests, skipped by design
in this environment and unrelated to architecture.

## Focused scans (repo-wide over `Tooba.AccessControl.*` production `.cs`)

| Scan | Hits |
| --- | --- |
| `Results.Json` / `Results.BadRequest` / `Results.Problem` | 0 |
| `new ProblemDetails` | 0 |
| `.Message.StartsWith` / `.Message.Contains` / `.Message.Equals` | 0 |
| `when (… .Message …)` | 0 |
| `"access.*"` raw string literal | 0 |
| `Console.Write` / `Console.Error` / `Debug.Write` | 0 |
| `ActivitySource.StartActivity` / `traceparent` | 0 |
| foreign `Tooba.<Other>.Application/Infrastructure/Domain` using | 0 |
| foreign `Tooba.<Other>.Contracts` using | 9 (allowed) |
| namespace mismatch | 0 |
| `using X = Y;` alias workaround | 0 |
| `TypeForwardedTo` | 0 |
| `global using` alias | 0 |
| new migration added | 0 |

## Focused builds

| Project | Result |
| --- | --- |
| `Tooba.AccessControl.Contracts` | ✅ builds |
| `Tooba.AccessControl.Domain` | ✅ builds |
| `Tooba.AccessControl.Application` | ✅ builds |
| `Tooba.AccessControl.Infrastructure` | ✅ builds |
| `Tooba.AccessControl.Endpoints` | ✅ builds |
| `Tooba.Host.Tests` (guard project) | ✅ builds |

## Required guards

| Guard | Result |
| --- | --- |
| `AccessControlValidatorTests` | ✅ pass |
| `AccessControlManifestDiskReconciliationGuardTests` | ✅ 7/7 pass |
| `AccessControlModuleAmsc001W1MigrateGuardTests` | ✅ pass |
| `AccessControlStructureRecert001GuardTests` | ✅ pass |
| `AccessControlStructureRepair001GuardTests` | ✅ pass |
| `AccessControlModuleAmcW1SolutionGuardTests` | ✅ pass |
| `AccessControlModuleAmcW2SemanticGuardTests` | ✅ pass |
| `AccessControlModuleAmcW3ResultGuardTests` | ✅ pass |
| `AccessControlModuleAmcW4StructureGuardTests` | ✅ pass |
| `AccessControlModuleAmcW5CertGuardTests` | ✅ pass |
| `AccessControlModuleAmc002W2CertGuardTests` | ✅ pass |
| `AccessControlModuleAmsc001W3CertGuardTests` (new, W3) | ✅ 7/7 pass |
| `AccessControlFoundationTests` / `AccessControlRuntimeScopeTests` | ✅ pass |
| `TmarCompleteReferenceStructureGateTests` | ❌ 3 facts — **pre-existing, out of scope** |
| `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | ❌ 1 fact — **pre-existing, out of scope** |
| `TmarSourceSizeAndInfraAppTests` (3 facts) | ❌ — **pre-existing, out of scope** |

## Pre-existing repo-wide failures — proof of independence

Each failing guard was run at the W0 baseline `a3ba1a4f` (before any AccessControl AMSC work) and
reproduces identically:

| Guard | `a3ba1a4f` | W1 tip `c9e009f8` | W2 tip `740c1210` | W3 working tree |
| --- | --- | --- | --- | --- |
| `TmarCompleteReferenceStructureGateTests` | `Failed: 3 / 3` | `Failed: 3 / 3` | `Failed: 3 / 3` | `Failed: 3 / 3` |
| `TmarSourceSizeAndInfraAppTests` | `Failed: 3 / 6` | `Failed: 3 / 6` | `Failed: 3 / 6` | `Failed: 3 / 6` |
| `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | fails | fails | fails | fails |

Proof method: `git stash push -u` → `git checkout a3ba1a4f` → run filtered tests → `git checkout main`
→ `git stash pop`.

### Exact pre-existing causes (outside AccessControl scope)

| # | Guard | Cause | Owner |
| --- | --- | --- | --- |
| 1 | `TmarCompleteReferenceStructureGateTests` | expects a 21-module manifest set that omits `Catalog`; manifest declares 22 modules | shared gate |
| 2 | `TmarCompleteReferenceStructureGateTests` | `Tooba.Catalog.Contracts/Cart/CatalogCartQuantityContracts.cs` and `Cart/ICatalogCartPresentationLookup.cs` declare `Tooba.Catalog.Contracts` instead of `…Contracts.Cart` | `Catalog` module |
| 3 | `TmarDurableGuardTests` | asserts a stale 16-module `structureLock.certifiedModules` list; SoT holds 21 | shared gate / SoT |
| 4 | `TmarSourceSizeAndInfraAppTests.Hand_written_source_size…` | size baseline still keys `AccessControlDirectory.cs` at its pre-`Directories/` path and still lists deleted Host/other-module files; a stray `.tmp-baseline/` working copy (untracked, `.gitignore`d) is scanned because `ShouldExcludeRelativePath` only matches `.tmp-*` **file names**, not directory names | shared guard + size baseline |
| 5 | `TmarSourceSizeAndInfraAppTests.Source_size_inventory_evidence…` | `docs/evidence/TB-TMAR-BOUNDARY-V1-R1/source-size-inventory.json` records 7256 files; the real scan (excluding the stray `.tmp-baseline/`) finds 1845 | shared guard / evidence doc |
| 6 | `TmarSourceSizeAndInfraAppTests.Infrastructure_to_foreign_Application_edges…` | baseline drift in other modules | shared guard |

The only AccessControl-adjacent item (#4) is caused by a **stale baseline path key plus a stray
untracked working copy** — not by any production change to the module. The module's real file
`Infrastructure/Directories/AccessControlDirectory.cs` has one physical home and no duplicate.

**No guard, baseline or allowlist was weakened to reach PASS.**

## Verdict

All required AccessControl-scoped guards and focused builds pass. The residual failures are
pre-existing, repo-wide, and lie outside this task's authorized scope; they are reported for
Architect decision rather than silently repaired.

```text
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
```
