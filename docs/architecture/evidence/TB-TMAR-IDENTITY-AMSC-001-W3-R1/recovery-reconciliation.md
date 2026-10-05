# TB-TMAR-IDENTITY-AMSC-001-W3-R1 — Recovery / SoT reconciliation

## Mode

`RECOVERY_SOT_RECONCILIATION_ONLY`. Zero production change. Bounded to
`docs/architecture` SoT/recovery truth plus the minimum durable-guard reconciliation needed to lock
that truth.

Baseline: `branch = main`, `HEAD == origin/main == e6d467740dc0305c665d72712fbc5f115ba4b4bf` (W3).

## Precheck

| Check | Result |
|---|---|
| `git rev-parse HEAD` | `e6d467740dc0305c665d72712fbc5f115ba4b4bf` |
| `git rev-parse origin/main` | `e6d467740dc0305c665d72712fbc5f115ba4b4bf` |
| HEAD equals the task STARTING HEAD | `YES` |
| Repository truth differs from the task defect description | `NO` (`RECOVERY_CONFLICT` not raised) |

## Defect confirmed

`identityAmc001` — the pre-existing **historical** `TB-TMAR-IDENTITY-AMC-001` record — was rewritten
by W3 (`e6d46774`) into current AMSC truth.

### Exact `identityAmc001` diff: `7c79f8c6` (pre-W3) vs `e6d46774` (W3)

| Field | At `7c79f8c6` (historical truth) | At `e6d46774` (W3 rewrite) |
|---|---|---|
| `structureState` | `READY_FOR_CERTIFY` | `CERTIFIED` |
| `validatorCoverage` | `COMPLETE_6_OF_6_REQUIRED_PRESENT_7_NO_VALIDATOR_REQUIRED` | `COMPLETE_9_OF_9_REQUIRED_PRESENT_4_NO_VALIDATOR_REQUIRED` |
| `validatorRequiredCount` | `6` | `9` |
| `noValidatorRequiredCount` | `7` | `4` |
| `amsc001Certified` | *(absent)* | `true` |
| `amsc001CertificationNote` | *(absent)* | *"Re-certified under the AMSC-001 four-wave pipeline …"* |
| `amsc001EvidenceRoot` | *(absent)* | `docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W0..W3/` |
| `amsc001StopGate` | *(absent)* | `USER_REVIEW_IDENTITY_AMSC_001_W3` |

All other AMC-001 fields (`task`, `mode`, `skills`, `state`, `httpApplicability`, `endpointOwnership`,
`moduleMap`, `cqrs`, `folderGranularity`, `solutionExplorer`, `pathNamespace`, `rootAllowlist`,
`endpointReachableRequests`, `validationDiscovery`, `contractsBoundary`,
`foreignAppInfraDomainCoupling`, `customerProfileCoupling`, `hostAuthHttpResidue`,
`hostAuthPlatformRetained`, `manifestCertified`, `microserviceExtractable`, `frontendFrozen`,
`evidenceStructure`, `evidenceCertify`, `implementationCommit`, `implementationCommitKind`,
`docsStampCommit`, `docsStampCommitKind`) were byte-identical between the two commits.

## Restored historical fields

`identityAmc001` is restored to its truthful pre-W3 values, with the W3-appended AMSC fields removed:

| Field | Restored value |
|---|---|
| `task` | `TB-TMAR-IDENTITY-AMC-001` (unchanged) |
| `structureState` | `READY_FOR_CERTIFY` |
| `validatorCoverage` | `COMPLETE_6_OF_6_REQUIRED_PRESENT_7_NO_VALIDATOR_REQUIRED` |
| `validatorRequiredCount` | `6` |
| `noValidatorRequiredCount` | `7` |
| `implementationCommit` | `aafd14e0be51bdf3eae2ec2c6c1a09f92c613992` (unchanged) |
| `docsStampCommit` | `c7e473cd13d800de9cf3c429d884a10bb68819a1` (unchanged) |

Removed W3-added fields: `amsc001Certified`, `amsc001CertificationNote`, `amsc001EvidenceRoot`,
`amsc001StopGate`.

The historical AMC-001 record is **preserved**, not erased.

## Current AMSC authority preserved

`identityModuleAmsc001W0` … `identityModuleAmsc001W3` remain the authoritative current Identity
module state, unchanged by R1:

| Field (`identityModuleAmsc001W3`) | Value |
|---|---|
| `state` | `IDENTITY_AMSC_001_CERTIFIED` |
| `verdict` | `COMPLETE_REFERENCE_PATTERN` |
| `lockVersion` | `ARCH-COMPLETE-002` |
| `structureCertified` | `true` |
| `structureState` | `CERTIFIED` |
| `validatorCoverageState` | `EXHAUSTIVE_9_REQUIRED_PRESENT_4_NO_VALIDATOR_REQUIRED` |
| `endpointReachableRequests` | `13` |
| `foreignAppInfraDomainCoupling` | `ZERO` |
| `crossModuleJoinState` | `ZERO` |
| `crossModulePersistenceState` | `ZERO` |
| `blockingResidualDebt` | `ZERO` |
| `microserviceExtractable` | `true` |
| `automaticNextImplementationTask` | `NONE` |

`identityModuleAmsc001W2` keeps the historical W2 classification
(`validatorCoverageState = EXHAUSTIVE_9_REQUIRED_PRESENT_4_NO_VALIDATOR_REQUIRED`,
`validatorRequiredCount = 9`, `noValidatorRequiredCount = 4`).

## Additive R1 SoT record

`identityModuleAmsc001W3R1` was appended to `docs/architecture/tmar-current-state.json` with the
required keys (`task`, `parentTask`, `mode`, `state`, `productionCodeChanged`, `certifiedCommit`,
`historicalAmcState`, `historicalLineageState`, `masterRecoveryW3ShaState`, `currentAuthority`,
`globalHostCheckpointState`, `workflowStop`, `automaticNextImplementationTask`) plus the restored /
removed field ledgers and the zero-change proofs.

## Master Recovery W3 SHA: before / after

| | Value |
|---|---|
| Before R1 | `TB-TMAR-IDENTITY-AMSC-001-W3` Certify `this wave` — final W3 SHA **MISSING** |
| After R1 | `TB-TMAR-IDENTITY-AMSC-001-W3` Certify `e6d46774` (`e6d467740dc0305c665d72712fbc5f115ba4b4bf`) — **RECORDED** |

W0 `91eec1fd` / W1 `93a6b192` / W2 `7c79f8c6` SHAs preserved exactly.

## Historical marker

`docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` records the canonical authority marker
`HISTORICAL / SUPERSEDED FOR CURRENT IDENTITY MODULE RECOVERY` for the older AMC-001 lineage, with the
explicit meaning: AMC history remains truthful historical evidence; the AMSC-001 W0→W3 lineage is
authoritative for the current Identity certification; the repository-global Host root checkpoint is
**not** superseded. The new `Identity AMSC W3-R1 recovery reconciliation (module-local)` block carries
the `USER_REVIEW_IDENTITY_AMSC_001_W3_R1` stop gate.

## Global Host root checkpoint preservation

| Global pointer | Value (unchanged) |
|---|---|
| `lastAcceptedTask` | `TB-TMAR-HOST-ROOT-FINAL-CERT-001` |
| `lastAcceptedCommit` | `7a6c353a98a761df9124beb1fce23ed8424230de` |
| `latestAcceptedImplementationWave` | `TB-TMAR-HOST-ROOT-FINAL-CERT-001` |
| `currentHostCheckpoint` | `HOST_ROOT_FINAL_CERTIFIED` |
| `nextHostFolder` | `null` |
| repository-global `workflowStop` | `USER_REVIEW_HOST_ROOT_FINAL_CERT_001` |
| repository-global `automaticNextImplementationTask` | `NONE` |

`structureLock.certifiedModules` is untouched and still contains `Identity` exactly once.

## Zero-change proofs

| Surface | State |
|---|---|
| Production files changed (`src/backend/Modules/Identity/**`, `Host/Tooba.Host/**`, any `*.csproj`) | `ZERO` |
| Schema / migration change | `ZERO` (0 migration files touched) |
| Frontend change | `ZERO` (`FROZEN_UNCHANGED`) |
| `tmar-module-structure-manifests.json` structural change | `ZERO` (file not touched) |
| Routes / DTO shapes / error-code values / descriptors / resx / DI | `UNCHANGED` |
| Guards weakened / baselines widened | `NONE` |

## Exact changed-file list (R1)

```text
docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W3/certification.md
docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W3-R1/claim-task.js
docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W3-R1/patch-sot.js
docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W3-R1/patch-recovery.js
docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W3-R1/recovery-reconciliation.md
docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W3-R1/validation.md
docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W3-R1/task-receipt.json
docs/ai/tasks/TB-TMAR-IDENTITY-AMSC-001-W3-R1.task.md
src/backend/Host/Tooba.Host.Tests/Architecture/IdentityModuleAmcW5CertGuardTests.cs
src/backend/Host/Tooba.Host.Tests/Architecture/IdentityModuleAmsc001W3CertGuardTests.cs
```

## Durable guard reconciliation (recovery correction, not weakening)

- `IdentityModuleAmcW5CertGuardTests` — the historical AMC guard no longer forces `identityAmc001` to
  carry current AMSC values. `Identity_sot_marks_complete_reference_pattern_and_structure_ready` now
  asserts historical AMC truth (`structureState = READY_FOR_CERTIFY`, 6 REQUIRED + 7
  NO_VALIDATOR_REQUIRED) against `identityAmc001` and current AMSC truth against
  `identityModuleAmsc001W3`. No assertion was deleted and no baseline was widened.
- `IdentityModuleAmsc001W3CertGuardTests` — the AMSC certification guard now targets the AMSC W3
  record for current truth, pins the restored historical AMC truth plus the absence of the four
  W3-added AMSC fields, locks the additive `identityModuleAmsc001W3R1` record, and asserts the Master
  Recovery W3 SHA `e6d46774`, the historical marker and the R1 stop gate.

## Automatic next

`automaticNextImplementationTask = NONE`. Stop gate `USER_REVIEW_IDENTITY_AMSC_001_W3_R1`.
