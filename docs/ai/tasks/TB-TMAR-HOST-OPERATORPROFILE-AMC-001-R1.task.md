PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-OPERATORPROFILE-AMC-001-R1
Parent-Task: TB-TMAR-HOST-OPERATORPROFILE-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host OperatorProfile AMC Repair
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_OPERATORPROFILE_REPAIR
Title: Remove broad InvalidOperationException remap from OperatorProfile and reconcile Recovery

BASELINE
Accepted parent implementation:
059e682e85ecb579502e611655ffa24ba25d1c7c

Parent docs/stamp:
dcd4e89dc8d0ed47e35a20e8b27093ad8e789f8a

PRESERVE

src/backend/Host/Tooba.Host/OperatorProfile/ = ABSENT
OperatorProfile HOST_ZERO
HTTP owner = Tooba.OperatorProfile.Endpoints
CQRS/MediatR preserved
ApiResponseFactory preserved
Host retained only thin HostOperatorProfileAdminAuthorizer
Endpoints → Domain = ZERO
Endpoints → Infrastructure = ZERO
no title = ex.Message
no message-text classification
frontend unchanged
schema unchanged

R1 BLOCKER
Current:
src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Infrastructure/OperatorProfileDirectory.cs

UpsertAsync(...) currently wraps the whole operation in:

catch (InvalidOperationException)

and remaps every such exception to:

SemanticException(new SemanticError(OperatorProfileErrorCodes.ProfileRejected))

This is too broad.

Unexpected InvalidOperationException from EF/runtime/internal code must NOT be classified as a known business rejection.

ARCHITECTURE DECISION
Expected OperatorProfile rejection must be transported through a stable typed/code-based path at the owner boundary.

Unknown/unexpected exceptions must propagate unchanged.

No exception-message inspection is allowed.

REQUIRED REPAIR

DOMAIN / OWNER FAILURE SEMANTICS
Audit all expected rejection paths involved in OperatorProfile create/update.

Preferred fix:

Domain validation/rejection throws SemanticException directly with:
OperatorProfileErrorCodes.ProfileRejected

OR:

use a dedicated OperatorProfile-owned typed exception/fault with stable code, caught narrowly at owner boundary and converted to SemanticException.

Do NOT:

catch broad InvalidOperationException
inspect ex.Message
use localized message text to select error code
convert unknown infrastructure/runtime faults into operator.profile.rejected
INFRASTRUCTURE DIRECTORY
OperatorProfileDirectory.UpsertAsync must not broadly catch InvalidOperationException.

Expected outcomes:

known profile validation rejection → stable operator.profile.rejected
unknown exception → propagates

If Domain is updated to throw SemanticException directly, the directory should normally need no catch for that path.

ENDPOINT PRESENTATION
Keep:
ApiResponseFactory
SemanticException -> api.FromSemanticException(...)
PlatformHttpException handling if already present
unknown exceptions rethrow / propagate

No parallel raw JSON error path.

HOST BOUNDARY
Preserve:
Host/OperatorProfile absent
Host authorizer thin
no business logic reintroduced into Host
no sink-folder regression
DURABLE GUARD
Strengthen HostOperatorProfileAmcGuardTests or add focused OperatorProfile failure guard proving:
Host/OperatorProfile absent
Endpoints do not inspect ex.Message
Endpoints do not catch broad InvalidOperationException
OperatorProfileDirectory.UpsertAsync does not broadly catch InvalidOperationException
expected rejection code remains operator.profile.rejected
unknown exception is not remapped to operator.profile.rejected
Endpoints → Domain/Infrastructure remain ZERO
FOCUSED TEST
Add/adjust focused test to prove:
expected invalid profile input produces SemanticException/ProfileRejected
injected/forced unknown InvalidOperationException from a lower dependency or equivalent unexpected path is NOT remapped

Use the smallest deterministic test seam.
Do not redesign the module.

RECOVERY / SOT — MANDATORY DoD
Update all authoritative surfaces:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

Required final state:

lastAcceptedTask = TB-TMAR-HOST-OPERATORPROFILE-AMC-001-R1
lastAcceptedCommit = <actual R1 implementation SHA>
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-OPERATORPROFILE-AMC-001-R1
currentHostEvacuation.currentTask = TB-TMAR-HOST-OPERATORPROFILE-AMC-001-R1
currentHostEvacuation.activeModule = OperatorProfile
currentHostEvacuation.currentHostCheckpoint = OperatorProfile
active state = OPERATORPROFILE_CLOSED_HOST_ZERO_R1_USER_REVIEW_REQUIRED
workflowStop = USER_REVIEW_HOST_OPERATORPROFILE_AMC_001_R1_CLOSED_HOST_ZERO
nextTask = USER_REVIEW_HOST_OPERATORPROFILE_AMC_001_R1_CLOSED_HOST_ZERO
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO

If docs/stamp is a separate commit:

lastAcceptedCommit must still point to the R1 IMPLEMENTATION commit.

Historical accepted lineage must remain intact.

SCOPE LIMIT
Do NOT:

reopen Host/OperatorProfile
change routes
redesign CQRS
move logic to Host
modify unrelated modules
touch frontend
change schema/migrations
start another Host folder
run solution-wide refactors
touch unrelated user file accesscontrol-first-slice-map.md

FOCUSED VALIDATION ONLY

Build:

OperatorProfile.Domain if touched
OperatorProfile.Application if touched
OperatorProfile.Infrastructure
OperatorProfile.Endpoints
Host
directly affected tests

Run:

HostOperatorProfileAmcGuardTests
focused OperatorProfile domain/directory failure tests
TmarDurableGuardTests

No solution-wide test run.

EVIDENCE
Create:
docs/evidence/TB-TMAR-HOST-OPERATORPROFILE-AMC-001-R1/

Required:

blocker.md
failure-semantics.md
endpoint-boundary.md
behavior-parity.md
recovery.md
validation.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-OPERATORPROFILE-AMC-001-R1.task.md

SUCCESS CRITERIA
PASS only if all are true:

Host/OperatorProfile remains ABSENT
HOST_ZERO preserved
broad InvalidOperationException remap = ZERO
message-text classification = ZERO
expected profile rejection remains operator.profile.rejected
unknown exception propagation = YES
ApiResponseFactory preserved
Endpoints → Domain = ZERO
Endpoints → Infrastructure = ZERO
behavior parity preserved
schema change = NONE
frontend unchanged
Recovery fully reconciled to OperatorProfile R1
lastAcceptedCommit points to actual R1 implementation SHA
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO

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
Task-ID: TB-TMAR-HOST-OPERATORPROFILE-AMC-001-R1
Parent-Task: TB-TMAR-HOST-OPERATORPROFILE-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Host-OperatorProfile-State:
OperatorProfile-Host-Zero-State:
Broad-InvalidOperation-Remap-State:
Message-Text-Classification-State:
Expected-Rejection-Transport-State:
Profile-Rejected-Code-State:
Unknown-Exception-Propagation-State:
Api-Error-Presentation-State:
Endpoints-Domain-Reference-State:
Endpoints-Infrastructure-Reference-State:
Behavior-Parity-State:
Schema-Change-State:
Frontend-State:
Guard-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
Last-Accepted-Commit-State:
Stale-Current-Pointer-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
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
Do not start Caching.
Do not start another Host folder.
Wait for Architect/user review.

END_TOOBA_TASK