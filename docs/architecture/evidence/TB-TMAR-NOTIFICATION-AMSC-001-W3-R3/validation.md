# TB-TMAR-NOTIFICATION-AMSC-001-W3-R3 — validation

Bounded current validation only (no full solution suite), run at the W3-R3 working tree before commit.

## Focused builds

| Project | Result |
| --- | --- |
| `Tooba.Notification.Application` | 0 errors, 0 warnings |
| `Tooba.Notification.Endpoints` | 0 errors, 0 warnings |
| `Tooba.Notification.Tests` | 0 errors |
| `Tooba.Host` | 0 errors (pre-existing warnings only) |

## Focused tests

| Run | Result |
| --- | --- |
| `dotnet test Tooba.Notification.Tests` (module behavior + module architecture guards) | **18 passed / 0 failed** |
| `dotnet test Host.Tests --filter FullyQualifiedName~NotificationModuleAmsc001` (W1 11 + W3 9 + R2 6) | **26 passed / 0 failed** |
| `dotnet test Host.Tests --filter FullyQualifiedName~NotificationModuleAmsc001W3R3` (new recert guard, 4 tests) | **4 passed / 0 failed** |
| `dotnet test Host.Tests --filter FullyQualifiedName~Notification` (all Notification Host guards incl. `NotificationFoundationTests`) | **29 passed / 2 skipped** (Postgres Testcontainers) — 0 failed |
| `dotnet test Host.Tests --filter FullyQualifiedName~ErrorCatalogUniqueCodeGuardTests` | **3 passed / 0 failed** |

## Structural scans

- Zero child directories under the four request axes (machine scan + R2/R3 guards) — PASS.
- Stale old leaf path scan: all ten paths absent — PASS.
- Exact path↔namespace audit: `Path_namespace_alignment_is_exact_for_all_production_files` — PASS.
- Repo-wide foreign-`using` scan across all Notification sources: zero production hits (single hit is
  the guard's own assertion string) — PASS.
- Manifest consistency: exactly one certified Notification entry; absent from
  `uncertifiedHttpOwningModules`; project list matches disk/solution — PASS.
- JSON parse: `tmar-current-state.json` and `tmar-module-structure-manifests.json` parse — PASS.

## Diff scope proof

`git diff 7b8ab79e..HEAD` over `src/backend/Modules/Notification` touches only the 16 flattened
Application axis files (namespace-only edits), 2 Endpoints using repoints, 1 behavior-test using
repoint — Infrastructure/Domain/Contracts/Host diff **empty**; schema/migrations untouched;
`Tooba.slnx` untouched. No unrelated module file changed by this task.

## Out-of-scope red (pre-existing, disclosed, unchanged)

Repository-global recovery guard pins (`TmarDurableGuardTests.Recovery_*`) and the Catalog Contracts
namespace debt in `TmarCompleteReferenceStructureGateTests` — pre-date this task, unrelated to
Notification, not repaired (module-local scope). No open-ended repair loop; no guard weakened.
