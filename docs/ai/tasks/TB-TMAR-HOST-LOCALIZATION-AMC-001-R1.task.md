PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-LOCALIZATION-AMC-001-R1
Parent-Task: TB-TMAR-HOST-LOCALIZATION-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Localization AMC Repair
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_LOCALIZATION_FAILURE_SEMANTICS_REPAIR
Title: Remove message-text failure classification from Localization Endpoints and reconcile Recovery

BASELINE
Parent implementation: 7144379c0abfec00bb37f0ffb7ac33c44ca42a3d
Parent docs/stamp: cf9f08f032fff4708cf87b1e6b0475a574d0a64b

PRESERVE

Host/Localization = ABSENT / HOST_ZERO
HTTP owner = Tooba.Localization.Endpoints
Content language reference guard remains Content.Infrastructure via Localization.Contracts
Host retains only thin HostLocalizationAdminAuthorizer
ApiResponseFactory remains canonical presentation
schema/frontend unchanged

R1 BLOCKER
LocaleAdminEndpoints still classifies failures from exception message text:

InvalidOperationException ioe => ioe.Message
StartsWith("localization.language.")

This violates:

NO message-text classification
expected failures must be typed/code-based
unknown exceptions propagate

REQUIRED REPAIR

EXPECTED FAILURE TRANSPORT
Audit create/update/patch/list language paths.

Expected business failures must use:

SemanticException + SemanticError(LanguageErrorCodes.*)
or
a Localization-owned typed/Contracts fault carrying a stable Code.

Preferred: owner layer throws SemanticException directly.

Do NOT:

inspect ex.Message/ioe.Message
use StartsWith/Contains on exception-derived text
broadly catch InvalidOperationException and reinterpret it
use localized text as machine code
ENDPOINT PRESENTATION
LocaleAdminEndpoints may map only stable exception types:
PlatformHttpException -> ApiResponseFactory.FromPlatformException
SemanticException -> ApiResponseFactory.FromSemanticException
explicitly typed stable-code Localization fault if truly required

Unknown exceptions must propagate.
Remove TryMapLanguageFault if no longer needed.
No raw failure Results.Json path.

LANGUAGE ERROR CODES
Preserve Localization.Contracts.Errors.LanguageErrorCodes.
No duplicate parallel error catalog.

CONTENT REFERENCE GUARD
Do not move/reopen:
Content.Infrastructure.Adapters.ContentLanguageReferenceGuard
It must stay behind Localization.Contracts.ILanguageReferenceGuard.
ContentDbContext must remain outside Host.

HOST BOUNDARY
Preserve Host/Localization ABSENT.
HostLocalizationAdminAuthorizer remains thin.
No Localization Application/Domain/Infrastructure in Host.
No sink-folder regression.

DURABLE GUARDS
Strengthen HostLocalizationAmcGuardTests/focused guards to prove:

Host/Localization absent
ex.Message/ioe.Message code selection ZERO
message.Contains/StartsWith classification ZERO
broad InvalidOperationException business remap ZERO
expected failures use stable typed/code path
unknown InvalidOperationException is not remapped
ApiResponseFactory preserved
Content guard remains Content.Infrastructure via Localization.Contracts
FOCUSED TESTS
Add deterministic tests proving:
expected language rejection yields exact stable LanguageErrorCodes.*
unexpected InvalidOperationException propagates
changing exception message cannot change machine-code classification

RECOVERY / SOT — MANDATORY DoD
Update all:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

Required final state:

lastAcceptedTask = TB-TMAR-HOST-LOCALIZATION-AMC-001-R1
lastAcceptedCommit = <actual R1 implementation SHA>
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-LOCALIZATION-AMC-001-R1
currentHostEvacuation.currentTask = TB-TMAR-HOST-LOCALIZATION-AMC-001-R1
currentHostEvacuation.activeModule = Localization
currentHostEvacuation.currentHostCheckpoint = Localization
active state = LOCALIZATION_CLOSED_HOST_ZERO_R1_USER_REVIEW_REQUIRED
workflowStop = USER_REVIEW_HOST_LOCALIZATION_AMC_001_R1_CLOSED_HOST_ZERO
nextTask = USER_REVIEW_HOST_LOCALIZATION_AMC_001_R1_CLOSED_HOST_ZERO
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO

If docs/stamp is separate, lastAcceptedCommit MUST still point to implementation commit.

SCOPE LIMIT
Do NOT:

reopen Host/Localization
move HTTP back to Host
redesign Localization structure
move Content guard
touch frontend
change schema/migrations
start another Host folder
run solution-wide refactors
touch accesscontrol-first-slice-map.md

FOCUSED VALIDATION ONLY
Build:

Localization.Contracts/Application/Infrastructure/Endpoints as affected
Content.Infrastructure if affected
Host
Run:
HostLocalizationAmcGuardTests
focused Localization failure-semantics tests
focused endpoint presentation tests
TmarDurableGuardTests
No solution-wide test run.

EVIDENCE
Create docs/evidence/TB-TMAR-HOST-LOCALIZATION-AMC-001-R1/:

blocker.md
failure-semantics.md
message-classification-zero.md
endpoint-boundary.md
behavior-parity.md
recovery.md
validation.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-LOCALIZATION-AMC-001-R1.task.md

SUCCESS
PASS only if:

Host/Localization remains ABSENT
HOST_ZERO preserved
message-text classification ZERO
ex.Message code selection ZERO
broad InvalidOperationException remap ZERO
expected failures typed/code-based
LanguageErrorCodes preserved
unknown exception propagation YES
ApiResponseFactory preserved
Content guard remains Contracts-only
sink-folder regression ZERO
schema/frontend unchanged
Recovery reconciled to R1
lastAcceptedCommit = actual R1 implementation SHA
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO

GIT
Work from latest origin/main.
No reset/clean/rebase/force-push.
Preserve user work.
Commit/push main only on PASS.

CANONICAL RESULT
Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-LOCALIZATION-AMC-001-R1
Parent-Task: TB-TMAR-HOST-LOCALIZATION-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Host-Localization-State:
Localization-Host-Zero-State:
Message-Text-Classification-State:
Ex-Message-Code-Selection-State:
Broad-InvalidOperation-Remap-State:
Expected-Failure-Transport-State:
Language-Error-Codes-State:
Unknown-Exception-Propagation-State:
Api-Error-Presentation-State:
Content-Reference-Guard-State:
Host-Authorizer-State:
Sink-Folder-Regression-State:
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
Do not start another Host folder.
Wait for Architect/user review.

END_TOOBA_TASK