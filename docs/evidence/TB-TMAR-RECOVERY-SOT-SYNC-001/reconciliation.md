# Reconciliation — TB-TMAR-RECOVERY-SOT-SYNC-001

## 1. Canonical target checkpoint

```text
latestAcceptedTask                = TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001   (CERTIFIED)
lastAcceptedCommit                = 498c46bd36c1d72934e97b137625cb07de84272a   (IMPLEMENTATION_COMMIT)
lastAcceptedSoTStamp              = 736f23d34acb4f3989144f27675d1768fc7a65a9   (RECOVERY_DOCS_ONLY_STAMP_COMMIT)
rootGlobalBoundariesR3            = CERTIFIED_PRESERVED
  implementation commit           = c63f6ebb818e7e35a548c5b3e20eb18a25a244c8
  docs-only SoT stamp commit      = 7d8ea21155109def56866eee2acdab2067fb457b
currentHostEvacuationState        = RECONCILED_NOT_HISTORICAL_ADDRESSBOOK
activeModule                      = NONE
workflowStop                      = USER_REVIEW_AFTER_RECOVERY_SOT_SYNC_001
nextTask                          = USER_REVIEW_AFTER_RECOVERY_SOT_SYNC_001
nextTaskGate                      = USER_DECISION_REQUIRED_NO_AUTOMATIC_NEXT_IMPLEMENTATION_TASK
nextTaskState                     = USER_DECISION_REQUIRED
automaticNextImplementationTask   = NONE
```

All four SHAs above were resolved on the current `main` via `git rev-list --max-count=1 <sha>` and
`git merge-base --is-ancestor <sha> HEAD` (exit 0 each time).

## 2. Commit-semantics disambiguation

Repository convention observed in `git log`: an accepted implementation lands as its own commit, and a
**later, separate** docs-only `docs(sot): stamp …` commit records the acceptance in the SoT documents.
The pre-existing `lastAcceptedCommit` field therefore means *implementation commit*.

To keep that meaning truthful while still recording the acceptance stamp, the following fields were added
or repaired at the top level of `tmar-current-state.json`:

- `lastAcceptedCommitKind` = `IMPLEMENTATION_COMMIT`
- `lastAcceptedSoTStamp` = `736f23d34acb4f3989144f27675d1768fc7a65a9`
- `lastAcceptedSoTStampKind` = `RECOVERY_DOCS_ONLY_STAMP_COMMIT`
- `lastAcceptedCommitSemantics` = explicit statement that the two commits are distinct and must not be conflated

The implementation commit and the stamp commit are deliberately **not** the same SHA and neither field
was overwritten with the other.

## 3. `currentHostEvacuation` reconciliation

| Field | Before | After |
| --- | --- | --- |
| `activeModule` | `Content`-era / AddressBook-era active pointer | `NONE` |
| `activeModuleState` | active folder narrative | `NO_HOST_FOLDER_ACTIVE_USER_DECISION_REQUIRED` |
| `currentHostEvacuationState` | (absent) | `RECONCILED_NOT_HISTORICAL_ADDRESSBOOK` |
| `currentTask` | AddressBook/Content folder task | `TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001` |
| `currentPhase` | folder phase | `AUTHORIZATION_POSTCERT_CLEANUP_COMPLETE_USER_REVIEW_STOP` |
| `workflowStop` | `USER_REVIEW_ADDRESSBOOK_CHECKPOINT` | `USER_REVIEW_AFTER_RECOVERY_SOT_SYNC_001` |
| `automaticNextImplementationTask` | (absent) | `NONE` |
| `nextHostFolder` | implied AddressBook | `NONE_USER_DECISION_REQUIRED` |
| `nextHostFolderStarted` | implied | `false` |
| `addressBookLineage.state` | implied current | `HISTORICAL_COMPLETED_NOT_CURRENT` |
| `accessControlHistory.state` | implied current | `HISTORICAL_COMPLETED_NOT_CURRENT` |
| `staleCurrentPointerState` | (absent) | `ZERO` |

Historical facts were **preserved**, not deleted:
`historicalHostFolderLineage`, `historicalLineageNote`, `addressBookLineage` (all certification facts,
route counts, validator coverage, commits), `accessControlHistory`, `authenticationFolderDisposition`,
`deferredHostFolders`, and every later Host-folder SoT block (`hostContentAmc*`, `hostAdminAmc*`,
`hostCustomer*`, `hostDevelopmentAmc`, `hostCachingArchitectDirect`, `hostAuthorizationAmc`,
`hostRootGlobalBoundaries001R*`, `authorizationPostcertCleanup001`, …).

## 4. Documentation reconciliation

### `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`
- Added an authoritative **"Latest Accepted TMAR Checkpoint — authoritative"** head section with the
  task, both commit SHAs and their kinds, R3 lineage, `RECONCILED_NOT_HISTORICAL_ADDRESSBOOK`, and the
  stop state.
- Added an explicit **"Non-authoritative historical pointers warning"** naming exactly which files contain
  old pointers and the precedence order (`tmar-current-state.json` → latest accepted block/SoT stamp →
  Master Recovery history).
- Marked the surviving historical pointer lines as `HISTORICAL at the time it was recorded`:
  `Next task: USER_REVIEW_CART_STORE_COMMERCE_FAILFAST_001`,
  `Accepted SoT stamp: 552c928c…`, `Next task: USER_REVIEW_ADDRESSBOOK_CHECKPOINT`,
  `Gate: USER_REVIEW_REQUIRED_AFTER_ADDRESSBOOK_CERTIFICATION_STOP`.
- Reconciled the authoritative tail block: `Next TMAR task (CURRENT — reconciled) =
  USER_REVIEW_AFTER_RECOVERY_SOT_SYNC_001`, `Gate (CURRENT) =
  USER_DECISION_REQUIRED_NO_AUTOMATIC_NEXT_IMPLEMENTATION_TASK`, `Automatic next implementation task: NONE`.
- Demoted the stale Content R4 heading from `Current Live State (Content R4)` to
  `Historical Content R4 Checkpoint (NOT current)` and marked the Content lineage facts historical.
- Marked the AccessControl→AddressBook sequencing line and the AccessBook "current next task"
  (`TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001`) as NON-AUTHORITATIVE.

### `docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md`
- Replaced the AddressBook next-task/gate lines with the reconciled current values plus a latest-accepted
  line carrying both SHAs and both R3 SHAs.
- Fixed the later summary block (`Current next task (CURRENT — reconciled)`,
  `Current gate (CURRENT)`, `Latest accepted TMAR task`) and corrected the certified set to include
  `Fulfillment, AccessControl, AddressBook, Content`, marking the older
  "Fulfillment NOT certified, pre-cert validator repair required next" wording HISTORICAL.
- Marked the Host traversal "After Fulfillment … then AddressBook" instruction as historical and
  explicitly forbade starting a Host folder from it.

### `docs/ai/TOOBA-RECOVERY-CONTEXT.md`
- Added a **"Latest Accepted TMAR Checkpoint — authoritative (reconciled by TB-TMAR-RECOVERY-SOT-SYNC-001)"**
  section immediately after the channel block, with the task, both commit SHAs/kinds, R3 lineage,
  `Current-Host-Evacuation`, `Active-Host-Folder: NONE`, `workflowStop`, `Next-Task`, `Next-Task-Gate`
  and `Automatic-Next-Implementation-Task: NONE`.
- Historical `Next-Recommended-Task` / `Current Issued Task` / `Last Implementation Task` lines were left
  as lineage; the new top section explicitly supersedes them.

### `docs/architecture/tmar-current-state.json`
- Top-level checkpoint fields reconciled as listed in §1 and §2.
- `currentHostEvacuation` reconciled as listed in §3.
- Added canonical `workflowStop = USER_REVIEW_AFTER_RECOVERY_SOT_SYNC_001` (the required exact token).
- Added the dedicated `recoverySotSync001` block with all required PASS fields.

## 5. `recoverySotSync001` block (dedicated SoT)

```json
"recoverySotSync001": {
  "task": "TB-TMAR-RECOVERY-SOT-SYNC-001",
  "parentTask": "TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001",
  "state": "RECONCILED",
  "latestAcceptedTask": "TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001",
  "authorizationPostcertCleanupState": "CERTIFIED",
  "rootGlobalBoundariesR3State": "CERTIFIED_PRESERVED",
  "staleCurrentPointersState": "ZERO",
  "currentHostEvacuationState": "RECONCILED_NOT_HISTORICAL_ADDRESSBOOK",
  "automaticNextImplementationTask": "NONE",
  "workflowStop": "USER_REVIEW_AFTER_RECOVERY_SOT_SYNC_001",
  "nextTask": "USER_REVIEW_AFTER_RECOVERY_SOT_SYNC_001",
  "nextTaskState": "USER_DECISION_REQUIRED",
  "commitSemanticsState": "EXPLICIT_IMPLEMENTATION_COMMIT_VS_DOCS_ONLY_SOT_STAMP",
  "implementationCommit": "498c46bd36c1d72934e97b137625cb07de84272a",
  "sotStampCommit": "736f23d34acb4f3989144f27675d1768fc7a65a9",
  "rootGlobalBoundariesR3ImplementationCommit": "c63f6ebb818e7e35a548c5b3e20eb18a25a244c8",
  "rootGlobalBoundariesR3SotStampCommit": "7d8ea21155109def56866eee2acdab2067fb457b",
  "masterRecoveryCurrentState": "AUTHORIZATION_POSTCERT_CLEANUP_IS_LATEST_ACCEPTED_CHECKPOINT",
  "tmarCurrentStateState": "RECONCILED",
  "architectBootstrapState": "RECONCILED",
  "recoveryContextState": "RECONCILED",
  "historicalNextPointersState": "PRESERVED_AND_EXPLICITLY_MARKED_NON_AUTHORITATIVE",
  "recoveryGuardState": "STRENGTHENED",
  "productionCodeChangeState": "ZERO",
  "focusedValidationState": "PASS",
  "certificationState": "PASS",
  "evidence": "docs/evidence/TB-TMAR-RECOVERY-SOT-SYNC-001/",
  "taskArtifact": "docs/ai/tasks/TB-TMAR-RECOVERY-SOT-SYNC-001.task.md"
}
```

## 6. Durable recovery guard

`TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` was repaired to the
reconciled truth (it had pinned the AddressBook-era `lastAcceptedTask` and the 9-module certified set;
the current `structureLock.certifiedModules` is 10 modules including `Content`).

`TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative`
was added as the smallest sufficient new guard. It proves, deterministically from the repository:

1. `lastAcceptedTask = TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001`;
2. `lastAcceptedCommitKind = IMPLEMENTATION_COMMIT`, `lastAcceptedSoTStampKind =
   RECOVERY_DOCS_ONLY_STAMP_COMMIT`, and `lastAcceptedCommit != lastAcceptedSoTStamp`;
3. every recorded current SHA exists in git history and is an ancestor of `HEAD`
   (`git rev-list --max-count=1` + `git merge-base --is-ancestor`) for the Authorization pair and the
   R3 pair;
4. stop/gate is `USER_REVIEW_AFTER_RECOVERY_SOT_SYNC_001` /
   `USER_DECISION_REQUIRED_NO_AUTOMATIC_NEXT_IMPLEMENTATION_TASK` / `NONE`, never an implementation task;
5. `currentHostEvacuation` does not regress to AddressBook/Authentication/Admin/Fulfillment
   (`activeModule = NONE`, `staleCurrentPointerState = ZERO`, `nextHostFolderStarted = false`, and the
   AddressBook/AccessControl lineage blocks are `HISTORICAL_COMPLETED_NOT_CURRENT`);
6. the `recoverySotSync001` block carries the required PASS fields;
7. Master Recovery and Architect Bootstrap authoritative regions (text before the explicit
   `HISTORICAL / SUPERSEDED` boundary) contain the Authorization checkpoint and both stop markers, and no
   authoritative `next task:` line points at a historical `TB-TMAR-*` task without being marked
   HISTORICAL / NON-AUTHORITATIVE / superseded;
8. the stale `Current Live State (Content R4)` heading no longer appears in the authoritative region;
9. the reconciled stop token is unique (3 occurrences of the `workflowStop` slot, 2 of the `nextTask`
   slot, zero elsewhere in historical blocks);
10. no `production-code-change` marker exists for this task.
