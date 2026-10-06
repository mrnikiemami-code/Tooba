# TB-TMAR-NOTIFICATION-AMSC-001-W3-R4.task.md — Persisted claim artifact

Mode: RECOVERY_CERT_METADATA_RECONCILIATION_ONLY
Title: Close Notification recovery chain and reconcile stale manifest certification metadata

Persisted claim artifact — full Architect body received via Bridge `GET /api/tasks/next?channelId=tooba-main` at claim row `1c79886b-b845-4663-bb1f-84ee4f554c7c` (createdAtUtc 2026-10-06T17:13:31.4261584Z).

- Starting HEAD (required): `e3eb185ba109779f395d64e35b3704e1439b40ae` (W3-R3)
- Channel: tooba-main, WorkerId: tooba-worker-01, AgentType: cursor
- Parent-Task: TB-TMAR-NOTIFICATION-AMSC-001-W3-R3
- Current authoritative certification: W3-R3 at `e3eb185b`, COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002, CERTIFIED, gate = W3-R2, PROFESSIONAL_SHALLOW, zero per-use-case leaves.
- Accepted lineage: W0 `5777ff4a` → W1 `c660b933` → W2 `e2f23975` → W3 `763dab13` → W3-R1 `7b8ab79e` → W3-R2 `028ef769` → W3-R3 `e3eb185b`.

## Defects to reconcile (metadata/SoT/manifest only — no production/structure/behavior change)

A. SoT commit metadata: `notificationModuleAmsc001W3R1/R2/R3` each lack their own final commit → add `commit` + `commitFull` (R1 `7b8ab79e6f16d4eccd2eeaba5974936c109dba9b` — keep R1 `certifiedCommit = 763dab13…` unchanged; R2 `028ef769c4face48308a9dffb7f9714826e1be7c` — preserve `state`/`structureState`; R3 `e3eb185ba109779f395d64e35b3704e1439b40ae` — preserve every certification field; set R3 `recoveryFollowupState = RECONCILED_BY_TB_TMAR_NOTIFICATION_AMSC_001_W3_R4` without erasing the historical requirement).
B. Master Recovery: make current lineage explicit through R3; state W3 historical/superseded, W3-R2 valid handoff, W3-R3 at `e3eb185b` current authority, W3-R4 closes Recovery/metadata only, global Host checkpoint untouched, automaticNextImplementationTask = NONE.
C. Manifest metadata drift: replace ONLY `Tooba.Notification.Application.rootAllowlistJustification` stale "one folder per use case" text with truthful current shallow-tree text; do NOT change membership/allowlists/forbidden lists/structureCertified/lockVersion (certificationNote may sync one exact stale phrase if needed).

Plus: additive `notificationModuleAmsc001W3R4` (state = NOTIFICATION_AMSC_001_RECOVERY_CLOSED, r1/r2/r3CommitState RECORDED_*, manifestMetadataState = RECONCILED_TO_CURRENT_SHALLOW_TREE, globalHostCheckpointState = PRESERVED, workflowStop = USER_REVIEW_NOTIFICATION_AMSC_001_W3_R4, automaticNextImplementationTask = NONE; NO self-referential R4 commit field). Optional minimal metadata lock in the existing R3 recert guard. Evidence set (recovery-reconciliation.md, manifest-metadata-reconciliation.md, validation.md). One commit, push, `HEAD == origin/main`, Result to Bridge, STOP — no R5/W4/next module.
