# Recovery SoT — TB-TMAR-RECOVERY-SOT-SYNC-001

## Read this first

A fresh chat / architect MUST recover the CURRENT TMAR checkpoint from, in this order:

1. `docs/architecture/tmar-current-state.json` — machine-readable SoT
2. the latest accepted recovery block / SoT stamp (this file, and the
   **Latest Accepted TMAR Checkpoint — authoritative** head section of
   `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`)
3. Master Recovery history below those

Historical `Next task:` / `Current next task:` / `Next TMAR task:` / `next task =` / `Next-Recommended-Task:`
lines inside older sections of `TOOBA-TMAR-MASTER-RECOVERY.md`, `TOOBA-ARCHITECT-BOOTSTRAP.md` and
`docs/ai/TOOBA-RECOVERY-CONTEXT.md` are preserved lineage evidence and are **NON-AUTHORITATIVE**. They must
not be resumed.

## Current checkpoint

```text
latestAcceptedTask              = TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001
latestAcceptedTaskState         = CERTIFIED
lastAcceptedCommit              = 498c46bd36c1d72934e97b137625cb07de84272a
lastAcceptedCommitKind          = IMPLEMENTATION_COMMIT
lastAcceptedSoTStamp            = 736f23d34acb4f3989144f27675d1768fc7a65a9
lastAcceptedSoTStampKind        = RECOVERY_DOCS_ONLY_STAMP_COMMIT

rootGlobalBoundariesR3          = CERTIFIED_PRESERVED
  implementationCommit          = c63f6ebb818e7e35a548c5b3e20eb18a25a244c8
  sotStampCommit                = 7d8ea21155109def56866eee2acdab2067fb457b

currentHostEvacuationState      = RECONCILED_NOT_HISTORICAL_ADDRESSBOOK
activeModule                    = NONE
staleCurrentPointerState        = ZERO

workflowStop                    = USER_REVIEW_AFTER_RECOVERY_SOT_SYNC_001
nextTask                        = USER_REVIEW_AFTER_RECOVERY_SOT_SYNC_001
nextTaskState                   = USER_DECISION_REQUIRED
nextTaskGate                    = USER_DECISION_REQUIRED_NO_AUTOMATIC_NEXT_IMPLEMENTATION_TASK
automaticNextImplementationTask = NONE

executionMode                   = BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
frontendFrozen                  = true
checkoutState                   = PAUSED_AT_SAFE_W5_CHECKPOINT
```

## Commit semantics (do not conflate)

`lastAcceptedCommit` is the **implementation** commit (`498c46bd`, Authorization post-cert cleanup:
Host readiness now consumes only `AccessControl.Contracts.Readiness.IAuthorizationReadinessProbe`, and
`AppliedVersion` now means successfully applied only).

`lastAcceptedSoTStamp` is the **later docs-only** acceptance stamp commit (`736f23d3`,
`docs(sot): stamp authorization post-cert cleanup acceptance commit 498c46bd`). It is a distinct commit;
it records the acceptance but implements nothing.

Root Global Boundaries R3 follows the same pattern: implementation `c63f6ebb`, docs-only SoT stamp
`7d8ea211`.

## Historical checkpoints that must NOT be resumed

- Fulfillment evacuation + structure certification (closed)
- AddressBook inventory + structure certification (closed; the AccessBook/AccessControl Host folders are absent)
- Authentication Host inventory / bounded internal split / canonicalization (closed at its own checkpoint)
- Host/Admin canonicalization + Admin AMC waves (closed)
- Content AMC R1–R4 (closed)
- Root Global Boundaries R1/R2/R3 (R3 is the final certified state)
- Payment pre-cert validation / directory split (closed)
- Cart store-commerce fail-fast / StoreContext golden / Cart multi-currency line slices (closed)

## Current Host evacuation

There is **no active Host folder**. `currentHostEvacuation.activeModule = NONE`,
`nextHostFolder = NONE_USER_DECISION_REQUIRED`, `nextHostFolderStarted = false`. Do not invent a next Host
folder and do not nominate a new migration task.

## Current Gate

```text
USER_DECISION_REQUIRED_NO_AUTOMATIC_NEXT_IMPLEMENTATION_TASK
```

Worker STOPS. No polling. No automatic next task. No invented Worker IDLE.

## Evidence / artefacts

- `docs/evidence/TB-TMAR-RECOVERY-SOT-SYNC-001/analyze.md`
- `docs/evidence/TB-TMAR-RECOVERY-SOT-SYNC-001/reconciliation.md`
- `docs/evidence/TB-TMAR-RECOVERY-SOT-SYNC-001/validation.md`
- `docs/evidence/TB-TMAR-RECOVERY-SOT-SYNC-001/certification.md`
- `docs/evidence/TB-TMAR-RECOVERY-SOT-SYNC-001/recovery-sot.md` (this file)
- `docs/ai/tasks/TB-TMAR-RECOVERY-SOT-SYNC-001.task.md`
