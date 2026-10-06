# TB-TMAR-LOCALIZATION-AMSC-001-W3-R1 — Recovery / SoT reconciliation

- Task: `TB-TMAR-LOCALIZATION-AMSC-001-W3-R1`
- Parent: `TB-TMAR-LOCALIZATION-AMSC-001-W3` (commit `c3d01dc9`)
- Mode: `RECOVERY_SOT_RECONCILIATION_ONLY`
- Production code changed: **NO**

---

## 1. Why this wave exists

W3 could not know its own commit SHA while it was being written, so the master-recovery lineage line
was recorded as `TB-TMAR-LOCALIZATION-AMSC-001-W3` Certify *(this commit)* and the SoT
`localizationModuleAmsc001W3.commit` was left as `PENDING`. That is a truthful placeholder, not a
fact. This wave replaces it with the real SHA and adds the reconciliation record, exactly as the
Content / CustomerProfile / Identity / Inventory modules did in their own W3-R1 waves.

## 2. Accepted lineage (now fully explicit)

```text
TB-TMAR-LOCALIZATION-AMSC-001-W0 Analyze   5a0b882b
TB-TMAR-LOCALIZATION-AMSC-001-W1 Migrate   074fc3a4
TB-TMAR-LOCALIZATION-AMSC-001-W2 Structure 6bc3ee2d
TB-TMAR-LOCALIZATION-AMSC-001-W3 Certify   c3d01dc9
```

Certified commit: `c3d01dc90498bc73b58fe769729e7aa5f9e243a6`.

Each wave's recorded `startingHead` is the previous wave's commit, so the chain is verifiable end to
end. The `c3d01dc9..origin/main` push was verified (`HEAD == origin/main` after `git fetch`).

## 3. What changed

| Artifact | Change |
| --- | --- |
| `tmar-current-state.json` → `localizationModuleAmsc001W3.commit` | `PENDING` → `c3d01dc9` (only this field) |
| `tmar-current-state.json` → `localizationModuleAmsc001W3R1` | added (additive record) |
| `TOOBA-TMAR-MASTER-RECOVERY.md` | lineage line now records `c3d01dc9` instead of *(this commit)*; reconciliation block appended |
| `docs/architecture/evidence/TB-TMAR-LOCALIZATION-AMSC-001-W3-R1/` | this reconciliation + its patch script |

## 4. What did NOT change

- **W3 certification is preserved unchanged**: `state = LOCALIZATION_AMSC_001_CERTIFIED`,
  `verdict = COMPLETE_REFERENCE_PATTERN`, `lockVersion = ARCH-COMPLETE-002`,
  `structureCertified = true`, `structureState = CERTIFIED`, `httpApplicability = HTTP_OWNING`,
  `endpointReachableRequests = 4`, `crossModuleBoundaryState = CONTRACTS_ONLY`,
  `foreignAppInfraDomainCoupling = ZERO`, `microserviceExtractable = true`,
  `blockingResidualDebt = ZERO`.
- **No production code, no guard, no manifest, no schema/migration** was touched. The Localization
  `modules[]` manifest entry keeps its single `structureCertified: true` / `ARCH-COMPLETE-002` record.
- **Historical AMC-001 truth preserved**: `localizationAmc001` (`TB-TMAR-LOCALIZATION-AMC-001`) is
  left at its truthful pre-W3 values and is not rewritten into AMSC truth; it stays in the repository
  as historical evidence, explicitly marked `HISTORICAL / SUPERSEDED FOR CURRENT LOCALIZATION MODULE
  RECOVERY` in the master-recovery checkpoint.
- **Repository-global Host root checkpoint NOT superseded or displaced**: `lastAcceptedTask =
  TB-TMAR-HOST-ROOT-FINAL-CERT-001`, `lastAcceptedCommit`, `latestAcceptedImplementationWave`,
  `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`, `nextHostFolder`, `workflowStop` and
  `automaticNextImplementationTask = NONE` are untouched, exactly as W3 left them.
- The frozen frontend and the `PAUSED_AT_SAFE_W5_CHECKPOINT` Checkout lock are untouched.

## 5. Verification

| Check | Result |
| --- | --- |
| `LocalizationModuleAmsc001` (W1 + W2 + W3) | **22 passed / 0 failed** |
| `Localization*` + `ErrorCatalogUniqueCodeGuardTests` | **49 passed / 0 failed** |
| W3 commit pushed and `HEAD == origin/main` | verified |
| Guards weakened | 0 |

The five pre-existing, Localization-unrelated failures recorded in the W3 certification
(`TmarDurableGuardTests` repository-global recovery pins and `TmarSourceSizeAndInfraAppTests`
workspace/stale-evidence/baseline drift) remain red for their own reasons and were not touched.

## 6. Stop gate

`USER_REVIEW_LOCALIZATION_AMSC_001_W3_R1`. `automaticNextImplementationTask = NONE`.
