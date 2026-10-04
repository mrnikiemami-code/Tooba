# Structure verdict — AccessControl (W2)

**Task:** `TB-TMAR-ACCESSCONTROL-AMSC-001-W2`
**Skill:** `tooba-architecture-structure`
**Parent:** `TB-TMAR-ACCESSCONTROL-AMSC-001-W1` (`c9e009f8`)
**Module:** `src/backend/Modules/AccessControl/Tooba.AccessControl.*`
**Lock:** `ARCH-COMPLETE-002` / `TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD`

## Structure-State

```text
Structure-State = READY_FOR_CERTIFY
```

## Classification summary

| Axis | State |
| --- | --- |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` |
| Path-Namespace-State | `EXACT` |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |
| File-Cohesion-State | `COHESIVE` (`AccessControlDirectory.cs` `OVERSIZED_ONLY` → `WATCH`, baselined) |
| Technical-axis-first request tree | `ZERO` |
| Unjustified single-file request/use-case leaf folder | `ZERO` |
| Root dump | `ZERO` |
| Host final closure | `PRESERVED` |
| Cross-module boundary | `CONTRACTS_ONLY` |
| Microservice extractable | `YES` |

## READY gates (section 27)

| Gate | Result |
| --- | --- |
| Folder-Granularity-State = `PROFESSIONAL_SHALLOW` | ✅ |
| Solution-Explorer-State = `CANONICAL` | ✅ |
| Path-Namespace-State = `EXACT` | ✅ |
| Physical-Copy-State = `CLEAN` | ✅ |
| Root-Allowlist-State = `ENFORCED` | ✅ |
| No unjustified single-file request leaf folders | ✅ |
| No unjustified technical-axis-first request tree | ✅ |
| No root dump | ✅ |
| No unresolved god-file / over-split structure blocker | ✅ |
| Host final closure preserved | ✅ |
| Focused structure guards pass | ✅ (AccessControl-scoped) |

## W2 deliverables

| Deliverable | Path |
| --- | --- |
| Durable manifest↔disk + structure guard | `src/backend/Host/Tooba.Host.Tests/Architecture/AccessControlManifestDiskReconciliationGuardTests.cs` |
| Evidence root | `docs/architecture/evidence/TB-TMAR-ACCESSCONTROL-AMSC-001-W2/` |
| SoT record | `docs/architecture/tmar-current-state.json` → `accessControlModuleAmsc001W2` |

## Behavior / schema

```text
behaviorChange = NONE
schemaChange   = NONE
routesChanged  = NONE
```

W2 changed **no** production file. `physical-tree-after.md` is identical to
`physical-tree-before.md`.

## Out-of-scope pre-existing drift (reported, not repaired)

Three **repo-wide** guard expectations fail on `main` independently of AccessControl and were
proven to fail at the W0 baseline `a3ba1a4f` as well:

1. `TmarCompleteReferenceStructureGateTests` 21-module expectation vs the 22-module manifest
   (`Catalog` missing from the expectation).
2. `TmarCompleteReferenceStructureGateTests` namespace assertion on
   `Tooba.Catalog.Contracts/Cart/*.cs` (`Catalog` Cart-contract folder drift).
3. `TmarDurableGuardTests` stale 16-module `structureLock.certifiedModules` expectation vs the
   21-entry SoT list.

These live in the shared gate, the `Catalog` module and the repo-wide SoT — all outside this
task's authorized AccessControl scope. They are recorded in `manifest-structure.md` and
`validation.md` and were **not** modified.

## Handoff

```text
Structure → Certify
Structure-State = READY_FOR_CERTIFY
```

Ready for `tooba-architecture-certify` (W3). No self-authorization of the next task.
