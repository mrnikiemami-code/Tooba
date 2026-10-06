# TB-TMAR-NOTIFICATION-AMSC-001-W3-R4 — validation

Bounded metadata-reconciliation validation only (no full Host suite, no unrelated repairs).

## Checks

| Check | Result |
| --- | --- |
| JSON parse `tmar-current-state.json` + `tmar-module-structure-manifests.json` (BOM-tolerant) | PASS |
| SoT R1 `commitFull = 7b8ab79e6f16d4eccd2eeaba5974936c109dba9b` (R1 `certifiedCommit` still W3 `763dab13…`) | PASS |
| SoT R2 `commitFull = 028ef769c4face48308a9dffb7f9714826e1be7c` (R2 `state` preserved) | PASS |
| SoT R3 `commitFull = e3eb185ba109779f395d64e35b3704e1439b40ae` + `recoveryFollowupState = RECONCILED_BY_TB_TMAR_NOTIFICATION_AMSC_001_W3_R4` (all cert fields preserved) | PASS |
| SoT R4 additive record `NOTIFICATION_AMSC_001_RECOVERY_CLOSED` with RECORDED_* states, no self-referential commit | PASS |
| Master Recovery contains all three full SHAs (`7b8ab79e…`, `028ef769…`, `e3eb185b…`) and the closing R4 checkpoint | PASS |
| Manifest stale phrase `"one folder per use case"` absent; current truth `"zero per-use-case leaf directories"` present | PASS |
| Manifest structural fields unchanged (membership, allowlists, forbidden lists, `structureCertified`, `lockVersion`) | PASS |
| Durable metadata guard: `NotificationModuleAmsc001W3R3RecertGuardTests` → **5/5 passed** (4 prior + new `Recovery_chain_metadata_is_fully_reconciled`) | PASS |
| Zero-child-directory scan under the four request axes (Customer/Seller Commands/Queries) | PASS (flat) |
| Global Host checkpoint `HOST_ROOT_FINAL_CERTIFIED` + repository-global `workflowStop`/`automaticNextImplementationTask` unchanged | PASS |

## Diff scope proof

`git diff HEAD` for this wave touches exactly:

- `docs/architecture/tmar-current-state.json` (R1/R2/R3 `commit`+`commitFull` additions, R3
  `recoveryFollowupState`, additive `notificationModuleAmsc001W3R4`)
- `docs/architecture/tmar-module-structure-manifests.json` (one line: Notification Application
  `rootAllowlistJustification`)
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` (R3 SHA in lineage, follow-up CLOSED marker,
  additive R4 closing checkpoint)
- `src/backend/Host/Tooba.Host.Tests/Architecture/NotificationModuleAmsc001W3R3RecertGuardTests.cs`
  (minimal metadata-lock test added; no prior assertion changed, no guard weakened)
- `docs/ai/tasks/TB-TMAR-NOTIFICATION-AMSC-001-W3-R4.task.md` + this evidence folder

No Notification production file, csproj, Host production file, frontend file, route/DTO/status/error
behavior, validator, resource/resx or schema/migration is touched. Pre-existing unrelated working-tree
artifacts preserved untouched.
