PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-SECURITY-AMC-001-W1-R1
Parent-Task: TB-TMAR-HOST-SECURITY-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_SECURITY_W1_RECOVERY_RECONCILIATION
Title: Reconcile Security W1 Recovery/SoT stamp metadata without production changes
Estimated-Time-Minutes: 7
Hard-Timebox-Minutes: 10

ARCHITECT REVIEW STATE

W1 implementation:
FUNCTIONALLY_ACCEPTED_PENDING_RECOVERY_REPAIR

Verified implementation commit:
baa05e6b7fa373cb6d354a80ca2eab37d4472f7b

Verified later docs/stamp commit:
298ba95df231c64e29f56d2cbba747b7dfd782a3

Architect independently verified:

SellerPanelAccess, HostPartySellerAuthorizer, HostSupportSellerAuthorizer now use SemanticException/SemanticError
HostOrderSellerAuthorizer preserves ResolveAsync failure parity by catching SemanticException and returning ex.Error
hard-coded runtime titles under Host/Security = ZERO
Security production file count remains 19
Host/Admin certification preserved
W1 implementation behavior is acceptable

BLOCKER — RECOVERY/SOT INCONSISTENCY

Current docs/architecture/tmar-current-state.json top-level fields are internally inconsistent.

Examples currently observed:

lastAcceptedTask = TB-TMAR-HOST-SECURITY-AMC-001-W1
lastAcceptedCommit = baa05e6b7fa373cb6d354a80ca2eab37d4472f7b
BUT
lastAcceptedResultEvidenceCommit still points to old Admin certification commit:
23acf1bfa514f48d4a111a732d5bcaae45f78cbd
lastAcceptedSoTStamp still points to old Admin certification commit:
23acf1bfa514f48d4a111a732d5bcaae45f78cbd
lastAcceptedCommitSemantics still describes:
TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W3-CERT
rather than Security W1
top-level stamp semantics therefore do not describe the actual current checkpoint

This violates canonical Recovery truth even though production code is correct.

SKILL — MANDATORY

Use the repository recovery/documentation conventions from:

.cursor/skills/tooba-architecture-migrate/SKILL.md
for SoT synchronization discipline only.

DOCS/RECOVERY REPAIR ONLY.

NO production code changes.
NO test behavior changes except exact Recovery guard assertions if required.
NO Security W2 start.
NO certification.

REQUIRED REPAIR

TOP-LEVEL CURRENT STATE

Reconcile docs/architecture/tmar-current-state.json so the current checkpoint is self-consistent.

Required truth:

lastAcceptedTask:
TB-TMAR-HOST-SECURITY-AMC-001-W1

lastAcceptedCommit:
baa05e6b7fa373cb6d354a80ca2eab37d4472f7b

lastAcceptedCommitKind:
IMPLEMENTATION_COMMIT

latestAcceptedImplementationWave:
TB-TMAR-HOST-SECURITY-AMC-001-W1

currentHostCheckpoint:
Security

nextTask:
USER_REVIEW_HOST_SECURITY_AMC_001_W1_R1
(or canonical R1 review token used by repo convention)

workflowStop:
USER_REVIEW_HOST_SECURITY_AMC_001_W1_R1

automaticNextImplementationTask:
NONE

staleCurrentPointerState:
ZERO

nextHostFolderStarted:
false

RESULT / SOT STAMP SEMANTICS

The docs-only reconciliation commit created by THIS R1 must be recorded separately.

After R1 commit, top-level fields must follow canonical split semantics:

implementation SHA remains:
baa05e6b7fa373cb6d354a80ca2eab37d4472f7b

lastAcceptedResultEvidenceCommit / lastAcceptedSoTStamp:
point to THIS R1 docs/recovery stamp commit

corresponding Kind fields:
RESULT_EVIDENCE_DOCS_STAMP_NOT_IMPLEMENTATION
RESULT_EVIDENCE_DOCS_STAMP

lastAcceptedCommitSemantics:
explicitly state that Security W1 is the accepted implementation and R1 is docs/recovery reconciliation only

Do NOT point implementation authority to R1 docs commit.

W1 BLOCK

Preserve/update the hostSecurityAmc001W1 block so it remains historically correct:

implementationCommit = baa05e...
W1 semantics preserved
recovery repair lineage may reference W1-R1 if useful

Add a dedicated R1 block if consistent with current SoT conventions:
hostSecurityAmc001W1R1

Recommended contents:

taskId
parentTaskId
state = RECOVERY_SOT_RECONCILED
implementationCommit = baa05e...
docsStamp = actual R1 commit
stalePointerState = ZERO
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_SECURITY_AMC_001_W1_R1
RECOVERY CONTEXT DOCS

Reconcile current-checkpoint wording in:

docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md if it carries current authoritative pointer
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md if it carries current authoritative pointer

Do not rewrite historical sections unnecessarily.

DURABLE GUARD

Update only exact Recovery assertions needed for current checkpoint:

TmarDurableGuardTests or equivalent

Do NOT touch HostSecurity structure 18→19 guard in this R1.
That remains W2.

Do NOT broaden regex/count assertions to weak contains-style checks.

W1 EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-SECURITY-AMC-001-W1-R1/

Required:

recovery-reconciliation.md
validation.md

State clearly:

production change = NONE
implementation SHA unchanged
W1 implementation accepted
this task repairs stale top-level recovery/stamp metadata only

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-SECURITY-AMC-001-W1-R1.task.md

VALIDATION

Focused only:

parse current-state JSON
Recovery durable guard
verify current checkpoint fields
verify implementation SHA unchanged
verify R1 stamp split semantics
verify Security W1 block intact
verify Host/Admin certification still present
verify Security structure W2 still deferred

No build required unless Recovery guard project requires compile.

No solution-wide tests.

GIT

Latest origin/main.
No reset.
No clean.
No rebase.
No force push.
Preserve unrelated user work.

Commit docs/tests/SoT only.
Push on PASS.

SUCCESS CRITERIA

PASS only if:

production code change = ZERO
W1 implementation remains baa05e6b...
current top-level task/checkpoint = Security W1-R1 user review
result evidence / SoT stamp point to actual R1 docs commit
implementation vs docs-stamp semantics explicitly separated
stale Admin-cert semantics removed from current top-level pointer
staleCurrentPointerState = ZERO
automaticNextImplementationTask = NONE
Security W2 NOT started
Host/Admin certification preserved
focused Recovery guard PASS
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-SECURITY-AMC-001-W1-R1
Parent-Task: TB-TMAR-HOST-SECURITY-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Repair-State:
Production-Code-Change-State:
W1-Implementation-State:
W1-Implementation-Commit-State:
Current-Host-Checkpoint-State:
Top-Level-LastAcceptedTask-State:
Top-Level-LastAcceptedCommit-State:
LastAcceptedCommit-Kind-State:
Result-Evidence-Commit-State:
SoT-Stamp-State:
Commit-Semantics-State:
Security-W1-Block-State:
Security-W1-R1-Block-State:
Stale-Admin-Cert-Current-Pointer-State:
Stale-Current-Pointer-State:
Host-Admin-Certification-State:
Security-Structure-Reconcile-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Focused-Validation-State:
Recovery-Guard-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.

Do not start W2.
Do not certify Security.
Do not start another Host folder.

Wait for Architect/user review.

END_TOOBA_TASK
