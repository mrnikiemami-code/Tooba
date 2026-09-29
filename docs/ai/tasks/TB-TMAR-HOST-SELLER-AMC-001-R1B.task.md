PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-SELLER-AMC-001-R1B
Parent-Task: TB-TMAR-HOST-SELLER-AMC-001-R1A
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Seller AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: SELLER_R1_RECOVERY_POINTER_REPAIR
Title: Reconcile authoritative recovery pointers to accepted Seller R1A checkpoint

ACCEPTED IMPLEMENTATION
Architect accepts:
TB-TMAR-HOST-SELLER-AMC-001-R1A
Implementation commit:
520c9918fefeedd245e54b28792fb16c5d0da41d

Accepted facts:

Host/Security/Seller canonical
foreign Application/Domain/Infrastructure/Persistence = ZERO
Order view access implementation = Order.Infrastructure owned
full Host/Seller still OPEN
Seller-R2 NOT STARTED

DO NOT MODIFY PRODUCTION CODE.

MANDATORY SKILLS

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

Do NOT run migrate.

EXACT DEFECT
Recovery is split:

workflowStop/current review already points to USER_REVIEW_HOST_SELLER_AMC_001_R1A
but
top-level lastAcceptedTask / lastAcceptedCommit
currentHostEvacuation.currentTask
currentHostEvacuation.latestAcceptedImplementationWave
authoritative latest accepted sections
still point to the old Development checkpoint.

Repair only this inconsistency.

CANONICAL FINAL RECOVERY STATE

Top-level:

lastAcceptedTask = TB-TMAR-HOST-SELLER-AMC-001-R1A
lastAcceptedCommit = 520c9918fefeedd245e54b28792fb16c5d0da41d
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
nextTask = USER_REVIEW_HOST_SELLER_AMC_001_R1B
nextTaskState = USER_DECISION_REQUIRED
nextTaskGate = USER_DECISION_REQUIRED_NO_AUTOMATIC_NEXT_IMPLEMENTATION_TASK
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO

currentHostEvacuation:

activeModule = Seller
activeModuleState = SELLER_IN_PROGRESS_USER_REVIEW_REQUIRED
currentTask = TB-TMAR-HOST-SELLER-AMC-001-R1A
latestAcceptedImplementationWave = TB-TMAR-HOST-SELLER-AMC-001-R1A
currentHostCheckpoint = Seller
currentPhase = SELLER_R1A_ACCEPTED_R1B_RECOVERY_RECONCILED_USER_REVIEW_STOP
workflowStop = USER_REVIEW_HOST_SELLER_AMC_001_R1B
automaticNextImplementationTask = NONE
nextHostFolderStarted = false
nextHostFolder = NONE_USER_DECISION_REQUIRED
staleCurrentPointerState = ZERO

Preserve Development closure as HISTORICAL accepted lineage.

RECOVERY DOCUMENTS
Reconcile CURRENT/AUTHORITATIVE sections only:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

Required facts:

latest accepted implementation = Seller R1A
implementation commit = 520c9918fefeedd245e54b28792fb16c5d0da41d
current Host checkpoint = Seller
Seller security boundary R1A accepted
full Host/Seller still OPEN
5 Seller business files remain
Seller-R2 not started
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_SELLER_AMC_001_R1B

Do not rewrite unrelated history.

SOT BLOCK
Add:
hostSellerAmcR1B

Required:

parentTask = TB-TMAR-HOST-SELLER-AMC-001-R1A
productionCodeChangeState = ZERO
acceptedImplementationTask = TB-TMAR-HOST-SELLER-AMC-001-R1A
acceptedImplementationCommit = 520c9918fefeedd245e54b28792fb16c5d0da41d
recoveryPointerState = RECONCILED
staleCurrentPointerState = ZERO
fullSellerFolderCertification = NOT_YET
sellerR2State = NOT_STARTED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_SELLER_AMC_001_R1B
certificationState = PASS

VALIDATION ONLY
Run only:

JSON parse
recovery pointer consistency guard
durable recovery guard
exact CURRENT/AUTHORITATIVE section checks
verify Seller R1A commit still present
verify Host/Seller still has 5 business files
verify Host/Security/Seller remains 10 files
verify ZERO production files changed in R1B

No builds.
No module tests.
No solution-wide tests.
No production edits.

EVIDENCE
Create:
docs/evidence/TB-TMAR-HOST-SELLER-AMC-001-R1B/

recovery-diff.md
pointer-consistency.md
validation.md
certification.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-SELLER-AMC-001-R1B.task.md

SUCCESS CRITERIA
PASS only if:

all four authoritative recovery sources agree
Seller R1A is latest accepted implementation
Development remains historical
currentHostCheckpoint = Seller
full Host/Seller remains OPEN
Seller-R2 not started
staleCurrentPointerState = ZERO
automaticNextImplementationTask = NONE
zero production code change
user work preserved

GIT
Work from latest main.
No reset/clean/rebase/force-push.
Preserve user work.
Commit and push main only on PASS.

CANONICAL RESULT
Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-SELLER-AMC-001-R1B
Parent-Task: TB-TMAR-HOST-SELLER-AMC-001-R1A
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Production-Code-Change-State:
Accepted-Implementation-State:
Accepted-Implementation-Commit-State:
Top-Level-SoT-State:
CurrentHostEvacuation-State:
Master-Recovery-State:
Architect-Bootstrap-State:
Recovery-Context-State:
Development-Lineage-State:
Seller-Checkpoint-State:
Full-Seller-Folder-State:
Seller-R2-State:
Stale-Current-Pointer-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Focused-Validation-State:
Certification-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.
Do not start Seller-R2.
Wait for Architect/user review.

END_TOOBA_TASK
