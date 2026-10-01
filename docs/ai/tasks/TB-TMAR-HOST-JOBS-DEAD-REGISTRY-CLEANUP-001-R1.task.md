PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-JOBS-DEAD-REGISTRY-CLEANUP-001-R1
Parent-Task: TB-TMAR-HOST-JOBS-DEAD-REGISTRY-CLEANUP-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Jobs Dead Registry Cleanup Recovery Repair
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: RECOVERY_ONLY_RECONCILIATION
Title: Reconcile Recovery/SoT after removal of dead write-only BackgroundWorkerRegistry

BASELINE IMPLEMENTATION
Accepted implementation commit:
913ce3ca6c7e89a04f6ed9ebb74d1bc8a8660179

Implementation behavior already verified:

Host/Jobs/BackgroundWorkerRegistry.cs deleted
Host/Jobs folder absent
IBackgroundWorkerRegistry deleted from WorkerSeams
Program DI registration removed
Outbox / Cart / Payment / Order writer injections removed
real worker metrics/logging preserved
focused build/tests passed

IMPORTANT
This is a Recovery/SoT repair only.

DO NOT modify production behavior unless required only to correct stale recovery metadata.
DO NOT recreate Host/Jobs.
DO NOT reintroduce registry/seam/stubs.
DO NOT start another Host folder.

REQUIRED RECOVERY RECONCILIATION

Update all authoritative recovery surfaces:

docs/architecture/tmar-current-state.json

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/ai/TOOBA-RECOVERY-CONTEXT.md

REQUIRED FINAL STATE

lastAcceptedTask = TB-TMAR-HOST-JOBS-DEAD-REGISTRY-CLEANUP-001-R1
lastAcceptedCommit = 913ce3ca6c7e89a04f6ed9ebb74d1bc8a8660179
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-JOBS-DEAD-REGISTRY-CLEANUP-001-R1
currentHostEvacuation.currentTask = TB-TMAR-HOST-JOBS-DEAD-REGISTRY-CLEANUP-001-R1
currentHostEvacuation.activeModule = Jobs
currentHostEvacuation.currentHostCheckpoint = Jobs
active state = JOBS_DEAD_WRITE_ONLY_REGISTRY_REMOVED_USER_REVIEW_REQUIRED
workflowStop = USER_REVIEW_HOST_JOBS_DEAD_REGISTRY_CLEANUP_001_R1
nextTask = USER_REVIEW_HOST_JOBS_DEAD_REGISTRY_CLEANUP_001_R1
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
historical prior accepted Composition R1 lineage remains intact

Add/ensure a durable Jobs cleanup history block recording:

taskId = TB-TMAR-HOST-JOBS-DEAD-REGISTRY-CLEANUP-001-R1
classification = HOST_ZERO_DEAD_INFRA_REMOVED
Host/Jobs = ABSENT
BackgroundWorkerRegistry = REMOVED
IBackgroundWorkerRegistry = REMOVED
GetState = REMOVED
production readers before removal = ZERO
writer-only consumers removed from Outbox/Cart/Payment/Order
real metrics/logging preserved
implementationCommit = 913ce3ca6c7e89a04f6ed9ebb74d1bc8a8660179
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_JOBS_DEAD_REGISTRY_CLEANUP_001_R1

If a docs/stamp commit is created:

lastAcceptedCommit MUST remain 913ce3ca6c7e89a04f6ed9ebb74d1bc8a8660179
lastAcceptedCommitKind MUST remain IMPLEMENTATION_COMMIT
docs/stamp SHA must be recorded separately if your recovery format supports it

PLATFORM KEEP AREAS
Ensure Jobs is NOT listed as a retained platform keep area anymore.

GENERIC WORKER SEAMS
Ensure generic worker seam metadata no longer references IBackgroundWorkerRegistry or BackgroundWorkerRunState.

NO STALE REFERENCES
Search recovery/docs for stale claims that:

Host/Jobs exists
BackgroundWorkerRegistry is retained
IBackgroundWorkerRegistry is an active seam
Jobs is a platform KEEP area

Repair only stale recovery/docs references that contradict the accepted implementation.

VALIDATION
Focused only:

verify Host/Jobs path absent
verify Program.cs has no BackgroundWorkerRegistry or IBackgroundWorkerRegistry
verify WorkerSeams has no IBackgroundWorkerRegistry / BackgroundWorkerRunState
verify Outbox/Cart/Payment/Order no longer reference IBackgroundWorkerRegistry
verify tmar-current-state.json points to the implementation SHA above
verify automaticNextImplementationTask = NONE
verify staleCurrentPointerState = ZERO

No solution-wide tests required.
No production refactor.

EVIDENCE
Create:
docs/evidence/TB-TMAR-HOST-JOBS-DEAD-REGISTRY-CLEANUP-001-R1/

Required:

recovery-before-after.md
dead-registry-state.md
validation.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-JOBS-DEAD-REGISTRY-CLEANUP-001-R1.task.md

SUCCESS CRITERIA
PASS only if:

production code behavior unchanged from implementation commit
Host/Jobs remains ABSENT
BackgroundWorkerRegistry remains REMOVED
IBackgroundWorkerRegistry remains REMOVED
Jobs removed from platform keep areas
recovery lastAcceptedTask points to R1
recovery lastAcceptedCommit points to 913ce3ca6c7e89a04f6ed9ebb74d1bc8a8660179
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
current checkpoint = Jobs
workflow stop = USER_REVIEW_HOST_JOBS_DEAD_REGISTRY_CLEANUP_001_R1
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
historical Composition R1 remains historical and intact
unrelated user work preserved

GIT
Work from latest origin/main.
No reset.
No clean.
No rebase.
No force push.
Preserve unrelated user work.
Commit/push main only on PASS.

CANONICAL RESULT
Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-JOBS-DEAD-REGISTRY-CLEANUP-001-R1
Parent-Task: TB-TMAR-HOST-JOBS-DEAD-REGISTRY-CLEANUP-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Host-Jobs-State:
BackgroundWorkerRegistry-State:
IBackgroundWorkerRegistry-State:
GetState-State:
Production-Reader-State:
Writer-Consumer-State:
Worker-Metrics-Logging-State:
Platform-Keep-Areas-State:
Generic-Worker-Seams-State:
Production-Code-Change-State:
Recovery-State:
Last-Accepted-Commit-State:
Last-Accepted-Commit-Kind:
Current-Checkpoint-State:
Stale-Current-Pointer-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Historical-Composition-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.
Do not start another Host folder.
Wait for Architect/user review.

END_TOOBA_TASK