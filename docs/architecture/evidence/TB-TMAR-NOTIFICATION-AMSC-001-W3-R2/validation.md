# TB-TMAR-NOTIFICATION-AMSC-001-W3-R2 — validation

Bounded validation only (no full solution test suite), all run at the W3-R2 working tree.

## Builds (affected projects)

| Project | Result |
| --- | --- |
| `Tooba.Notification.Application` | 0 errors, 0 warnings |
| `Tooba.Notification.Endpoints` | 0 errors, 0 warnings |
| `Tooba.Notification.Tests` | 0 errors (1 pre-existing xUnit2013 analyzer warning) |
| `Tooba.Host` | 0 errors (12 pre-existing warnings) |

## Focused tests

| Run | Result |
| --- | --- |
| `dotnet test Tooba.Notification.Tests` (module behavior + module architecture guards) | **18 passed / 0 failed** |
| `dotnet test Host.Tests --filter FullyQualifiedName~NotificationModuleAmsc001` (W1 11 + W3 9 + new W3-R2 6) | **26 passed / 0 failed** |
| `dotnet test Host.Tests --filter FullyQualifiedName~Notification` (all Notification-named Host guards incl. `NotificationFoundationTests`) | **29 passed / 0 failed / 2 skipped** (Postgres Testcontainers skips) |

## Structure audits

- Exact path↔namespace: `NotificationModuleAmsc001W3R2StructureRepairGuardTests.Request_axis_namespaces_are_exactly_the_axis_namespaces` + `NotificationModuleAmsc001W3CertGuardTests.Path_namespace_alignment_is_exact_for_all_production_files` — PASS.
- ZERO child directories under the four request axes — PASS (`Request_axes_carry_zero_child_directories`).
- All ten request sources + six validators colocated — PASS (`All_ten_requests_and_six_validators_are_colocated_on_their_axis`).
- Stale leaf path scan: all ten old leaf directories absent — PASS (`Stale_use_case_leaf_directories_are_absent`); repo-wide namespace-segment grep for old leaf namespaces returns zero hits.
- Solution grouping: `Tooba.slnx` `/Modules/Notification/` untouched, entries resolve on disk — CANONICAL.
- JSON parse: `tmar-current-state.json` parses (`node -e "require(...)"`) — OK.

## Behavior preservation

- Routes, DTO shapes, validator rules, error codes/resources, DI lifetimes, authorization, persistence and integration events: unchanged (diff scope proof: 16 renamed `.cs` files with namespace-only edits + 3 reference repoint files + 3 guard files + SoT/evidence).
- Schema/migrations: untouched (`git status` shows no Notification Infrastructure file modified).
- Host final closure: preserved — `Program.cs` touched ONLY for the assembly-scan anchor namespace repoint (no ownership/business logic change); `HostNotificationSellerAuthorizer` composition untouched.

## SoT

- Additive block `notificationModuleAmsc001W3R2` appended to `tmar-current-state.json`.
- `tmar-module-structure-manifests.json` NOT modified (physical allowlist truth unchanged; certification membership untouched).
- `TOOBA-TMAR-MASTER-RECOVERY.md` NOT modified in this Structure wave.
