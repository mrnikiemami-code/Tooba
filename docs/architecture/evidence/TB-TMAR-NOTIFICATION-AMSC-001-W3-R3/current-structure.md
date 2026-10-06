# TB-TMAR-NOTIFICATION-AMSC-001-W3-R3 — current-structure

Fresh independent Structure re-verification of the current tree at starting HEAD
`028ef769c4face48308a9dffb7f9714826e1be7c` (defense in depth over the authoritative W3-R2
Structure handoff; Certify does not override Structure).

## Structure gate source

`TB-TMAR-NOTIFICATION-AMSC-001-W3-R2` — Structure handoff accepted by the Architect:
`Structure-State = READY_FOR_CERTIFY` (SoT `notificationModuleAmsc001W3R2.structureState =
READY_FOR_CERTIFY`; evidence
`docs/architecture/evidence/TB-TMAR-NOTIFICATION-AMSC-001-W3-R2/`; Bridge Result
`RESULT.bridge.txt` records `Structure-Skill-State: READY_FOR_CERTIFY`).

## Current physical tree (re-verified live)

```text
Contracts/      Commands/ Copy/ Dtos/ Errors/ Ports/ Resources/ Routes/        (root .cs: none)
Domain/         Aggregates/ ValueObjects/                                      (root .cs: none)
Application/    Composition/ Models/ Ports/ Rendering/ Validators/              (root .cs: none)
                Customer/Commands/*.cs (5 files, 0 child dirs)
                Customer/Queries/*.cs  (3 files, 0 child dirs)
                Seller/Commands/*.cs   (5 files, 0 child dirs)
                Seller/Queries/*.cs    (3 files, 0 child dirs)
Infrastructure/ DependencyInjection/ Directories/ Handlers/ Messaging/ Observability/ Projectors/ Persistence/Migrations/  (root .cs: none)
Endpoints/      Customer/ Seller/ Errors/ + NotificationEndpointModule.cs       (root: composition entry only)
```

## Certified structure facts (machine-proven, current run)

| Fact | State | Proof |
| --- | --- | --- |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` | `NotificationModuleAmsc001W3R2StructureRepairGuardTests` — zero child directories under the four request axes; `Commands/Queries`-only branch shape |
| Single-file request leaf folders | ZERO (before: 4; R2 repaired) | `Stale_use_case_leaf_directories_are_absent` — all ten old leaf paths absent |
| Per-use-case request leaf folders | ZERO | same guard, `Request_axes_carry_zero_child_directories` |
| Technical-axis-first roots | ZERO | `Application_is_capability_first_with_no_technical_axis_roots` — `Application/Commands|Queries|Errors|Services` absent |
| Root dump | ZERO | `Application_is_capability_first...` (Application root .cs = 0) + `NotificationArchitectureGuardTests.AssertNoRootDump` across all five projects |
| Path-Namespace-State | `EXACT` | `Path_namespace_alignment_is_exact_for_all_production_files` (all five production projects, EF migrations exempt) + `Request_axis_namespaces_are_exactly_the_axis_namespaces` |
| Physical-Copy-State | `CLEAN` | stale leaf path scan zero hits; one authoritative home per type; `git status` shows no leftover old paths |
| Root-Allowlist-State | `ENFORCED` | manifest allowlists unchanged and enforced; Endpoints root = `NotificationEndpointModule.cs` only |
| Solution-Explorer-State | `CANONICAL` | `Tooba.slnx` `/Modules/Notification/` group with the six projects, entries resolve on disk, untouched |
| Alias/TypeForwardedTo workaround | NONE | repo-wide scan zero hits; `Notification_golden_boundaries...` guard asserts absence |
| Stale/duplicate copies | ZERO | §20 audit re-run post-R2; no dual live home |
| File cohesion | `COHESIVE` | 16 axis files: request+handler / one validator per file; shared folders unchanged |
| Host final closure | PRESERVED | no `Host/Notifications` folder; composition + thin seller security adapter + migration descriptor only |

## Structure verdict

`Structure-State = CERTIFIED` — all gates green on the current tree; the R2 over-foldering drift
cannot reappear without failing `NotificationModuleAmsc001W3R2StructureRepairGuardTests`.
