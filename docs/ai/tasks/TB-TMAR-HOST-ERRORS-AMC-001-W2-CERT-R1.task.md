PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT-R1
Parent-Task: TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ERRORS_CERT_RECOVERY_REPAIR
Title: Repair duplicate/wrong Security docsStamp introduced by Errors certification stamp
Estimated-Time-Minutes: 5
Hard-Timebox-Minutes: 8

ARCHITECT REVIEW STATE

Host/Errors W2-CERT:
FUNCTIONALLY_ACCEPTED_PENDING_RECOVERY_REPAIR

Architect independently verified the certification itself:

HOST_ERRORS_AMC_CERTIFIED is technically valid
Host/Errors exact one-file tree is valid
ToobaExceptionHandler thin canonical boundary is valid
PlatformExceptionMapper / MappedPlatformError absent
canonical presentation chain valid
Foundation 503/503/404 matrix valid
anti-enumeration behavior preserved
reservation.policy descriptor ownership valid
production change in CERT = ZERO
implementation authority remains W1:
e190e213c491fd530d86c7e5680cb0607b5e98d3

CERT certification commit:
8d5e6a2dce7b34e2ceeb4166e5d324467bc3a8f3

CERT later stamp commit:
657f858fb814be54f54aeeffa5a986bec315e458

BLOCKER — EXACT SOT CORRUPTION

The later stamp commit 657f858... incorrectly inserted:

"docsStamp": "8d5e6a2dce7b34e2ceeb4166e5d324467bc3a8f3"

inside the HISTORICAL/current Security certification block
hostSecurityAmc001W3Cert,
while that block already contains its correct Security certification stamp:

"docsStamp": "73a80ee28ed9dc054be5adae0f7115e72c115ded"

This creates a duplicate JSON property in the Security block and falsely associates
the Errors certification commit with Security certification metadata.

The Errors certification block must own the Errors certification docs stamp.
The Security block must retain only its own original Security certification stamp.

This is Recovery/SoT integrity debt only.
Production certification findings remain accepted.

SKILL — MANDATORY

Use repository recovery/documentation conventions only.

DOCS/SOT REPAIR ONLY.

NO production code.
NO test behavior change except durable Recovery assertion if needed.
NO MultiTenancy start.
NO next Host folder.
NO recertification work beyond SoT consistency verification.

REQUIRED REPAIR

FIX SECURITY BLOCK

In:
docs/architecture/tmar-current-state.json

Locate:
hostSecurityAmc001W3Cert

Final state must contain exactly ONE docsStamp property:

"docsStamp": "73a80ee28ed9dc054be5adae0f7115e72c115ded"

Remove the wrongly inserted Errors cert stamp:
8d5e6a2dce7b34e2ceeb4166e5d324467bc3a8f3

Do NOT modify any other accepted Security certification semantics.

ERRORS CERT BLOCK

Locate:
hostErrorsAmc001W2Cert

It must contain exactly ONE docsStamp:

"docsStamp": "8d5e6a2dce7b34e2ceeb4166e5d324467bc3a8f3"

If missing, add it there.

Do not use the later SHA-record stamp 657f858... as the certification commit itself.
Canonical split:

certification commit = 8d5e6a2d...
later stamp/materialization commit = 657f858...
TOP-LEVEL POINTERS

Preserve:

lastAcceptedTask:
TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT

lastAcceptedCommit:
e190e213c491fd530d86c7e5680cb0607b5e98d3

lastAcceptedCommitKind:
IMPLEMENTATION_COMMIT

latestAcceptedImplementationWave:
TB-TMAR-HOST-ERRORS-AMC-001-W1

currentHostCheckpoint:
Errors

Host/Errors certification:
HOST_ERRORS_AMC_CERTIFIED

Host/Security certification:
HOST_SECURITY_AMC_CERTIFIED

Host/Admin certification:
HOST_ADMIN_FULLY_CERTIFIED

automaticNextImplementationTask:
NONE

R1 REVIEW POINTER

On R1 PASS, current review gate becomes:

nextTask:
USER_REVIEW_HOST_ERRORS_AMC_001_W2_CERT_R1

workflowStop:
USER_REVIEW_HOST_ERRORS_AMC_001_W2_CERT_R1

staleCurrentPointerState:
ZERO

nextHostFolderStarted:
false

Implementation SHA remains unchanged.

JSON UNIQUENESS VALIDATION

Add a focused validation that ensures:

no duplicate property name exists within hostSecurityAmc001W3Cert
no duplicate property name exists within hostErrorsAmc001W2Cert
each block has exactly one docsStamp
stamps match their own task lineage

Prefer direct parsed/raw structural assertion that can detect duplicate JSON properties.
Do NOT rely solely on normal deserialization that silently takes the last duplicate property.

CURRENT AUTHORITY DOCS

Update only current review pointer wording if required:

docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md only if it contains current review gate

Do not rewrite historical content.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT-R1/

Required:

recovery-repair.md
validation.md

State:

Errors certification technical verdict preserved
production change ZERO
implementation SHA unchanged
only duplicate/wrong docsStamp metadata repaired

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT-R1.task.md

VALIDATION

Focused only:

JSON parse
duplicate-property detection
HostErrors cert guard
TmarDurableGuard current pointer
Security cert block stamp exact
Errors cert block stamp exact

No build unless test project compile requires.
No solution-wide tests.

GIT

Latest origin/main.
No reset.
No clean.
No rebase.
No force push.
Preserve unrelated user work.
Docs/tests only.

SUCCESS CRITERIA

PASS only if:

production code change ZERO
Host/Errors certification preserved
Security certification preserved
Security block has exactly one correct docsStamp = 73a80ee...
Errors cert block has exactly one correct docsStamp = 8d5e6a2...
no duplicate JSON property remains in either block
top-level implementation SHA stays e190e213...
automatic next NONE
current review gate points to R1
stale pointer ZERO
MultiTenancy not started
focused validation PASS
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT-R1
Parent-Task: TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Repair-State:
Production-Code-Change-State:
Errors-Certification-State:
Security-Certification-State:
Host-Admin-Certification-State:
Security-DocsStamp-State:
Errors-Cert-DocsStamp-State:
Duplicate-Json-Property-State:
Top-Level-Implementation-Commit-State:
Current-Host-Checkpoint-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Stale-Current-Pointer-State:
MultiTenancy-State:
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

Do not start MultiTenancy AMC.
Do not start another Host folder.

Wait for Architect/user review.

END_TOOBA_TASK