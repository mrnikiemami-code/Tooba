PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-CONTENT-AMC-001-R2
Parent-Task: TB-TMAR-HOST-CONTENT-AMC-001-R1
Parent-Commit: 627f58b36a6ffc18b07d902083f89f6352bfd0c2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_REPAIR
Title: Close remaining Content typed-fault/result-pipeline blockers after R1

ARCHITECT VERDICT ON R1

R1 made substantial correct progress and closed the originally migrated coupling defects:

Host/Content remains ZERO.
Content.Endpoints -> Content.Infrastructure = ZERO.
Content.Infrastructure -> Media.Application = ZERO.
Content.Infrastructure -> Localization.Application = ZERO.
Content uses Media.Contracts / Localization.Contracts.
CQRS/MediatR 12.5 + ISender introduced.
ApiResponseFactory introduced.
Content Contracts/error catalog/resources introduced.

However R1 is NOT Architect-accepted because expected-failure transport still uses exception-message classification and HTTP-semantic exceptions below the HTTP boundary.

BLOCKERS TO CLOSE

ContentOperation still classifies expected outcomes from InvalidOperationException.Message:

ContentErrorCodes.IsKnownCode(ex.Message)
new SemanticError(ex.Message)
This violates the ZERO message-classification requirement even when matching is exact equality rather than Contains.

Content Application/Infrastructure still uses PlatformHttpException for expected business/application failures.
HTTP status semantics must not be owned by Content Application/Infrastructure.

Media readiness boundary still uses code-as-exception-message:

Media bridge throws InvalidOperationException("media.asset.missing")
Content adapter catches InvalidOperationException
Content adapter rethrows InvalidOperationException(ContentErrorCodes.MediaNotFound)
This is not a typed Contracts boundary.

Any equivalent code-as-message or broad exception mapping discovered only within the directly touched Content/Media-contract path must be repaired.

CreateArticle success currently uses direct Results.Json(..., statusCode: 201). Verify the repository's existing canonical 201/Created presentation pattern. If a canonical helper exists, use it. If the canonical factory intentionally lacks a Created helper and raw success JSON is required to preserve shipped response shape/status, document and guard that explicit exception; do not invent a parallel response system.

REQUIRED SKILLS

Read and follow in order:

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

The recently hardened Host-evacuation gates are binding:

Host ZERO is not sufficient.
Known certification prerequisites cannot be residual debt.
Task success criteria cannot downgrade certification gates.

Also read:

AGENTS.md
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
docs/architecture/TMAR-architecture-locks.md
docs/architecture/tmar-current-state.json
the existing R1 task/evidence
one or more already accepted module examples for typed expected faults / ContractOperationException(Code) / Result conversion, but keep those modules read-only.

SCOPE

Primary:

src/backend/Modules/Content/
only the minimum Media.Contracts / Media adapter surface directly required to make the readiness boundary typed
only the minimum Localization.Contracts surface if directly implicated
focused Host Content guards/tests
R2 evidence + SoT

Do NOT:

inspect/start the next Host folder
reopen unrelated modules
perform broad Media or Localization recovery
touch frontend
change schema/migrations
redesign Content behavior
change routes, permissions, or success DTO shapes
introduce a new generic shared error/fault framework if the repository already has a canonical typed mechanism

TARGET ARCHITECTURE

Expected failures must move through typed semantics, never exception-message semantics.

Preferred shape, following an already accepted repository pattern:

Infrastructure / module contract boundary:
throw new ContractOperationException(stableCode) or return an existing typed result/fault contract

Application:
catch ONLY the typed expected fault when necessary and convert by Code to:
Result.Failure(new SemanticError(code))

Endpoints:
ISender -> Result/Result<T> -> ApiResponseFactory

Unknown exceptions:
propagate to the global exception boundary.

No:

ex.Message classification
code-as-message InvalidOperationException
PlatformHttpException from Content Application/Infrastructure for expected outcomes
catch-all conversion of unknown exceptions to business failures

MEDIA CONTRACT BOUNDARY

The Media readiness contract must communicate expected "asset missing/not ready" semantics without string parsing.

Use the smallest existing canonical pattern:

typed ContractOperationException(Code), OR
an already-established typed result/fault contract.

Requirements:

Media.Contracts owns the contract semantics it exposes.
Media implementation emits typed stable semantics.
Content translates only by stable Code where Content needs its own public code.
no direct Media.Application dependency.
no foreign persistence/domain leakage.
no exception-message matching.

CONTENT OPERATION

Repair or eliminate ContentOperation so that:

no InvalidOperationException.Message is inspected;
no code set exists solely to recognize exception messages;
only typed expected failures are converted;
unknown exceptions escape.

If individual handlers can return Result directly without a wrapper, prefer the repository-consistent smallest design.

PLATFORM HTTP EXCEPTION

Within the touched Content surface:

enumerate every PlatformHttpException use;
classify whether it is HTTP boundary-only or leaked into Application/Infrastructure;
remove expected-business/application uses below Endpoints;
replace with typed stable fault/result flow.

Do not mechanically rewrite unrelated platform exceptions outside the bounded touched path.

VALIDATION / ERROR OWNERSHIP

Preserve:

existing Content stable machine codes;
exactly one canonical descriptor owner;
existing localization resources;
current FluentValidation matrix unless a request shape actually changed.

Do not duplicate Media-owned descriptors inside Content merely because Content consumes a Media contract.
If Content intentionally translates a Media-owned code to a Content-owned code, document the mapping and preserve single ownership for each code.

CREATED / 201 RESPONSE

Inspect repository canonical presentation support before changing.

Acceptable outcomes:
A. use an existing canonical Created helper/factory method; or
B. preserve the current Results.Json(value, statusCode: 201) success path only if it is the established repository exception for preserving a raw success DTO and no canonical equivalent exists.

Whichever is chosen:

no ad-hoc error mapping;
errors still go through ApiResponseFactory;
document the reason in evidence;
add a focused guard if an explicit exception is retained.

MANDATORY SEARCHES IN TOUCHED SURFACE

Prove ZERO for expected-failure message classification:

.Message.Contains(
.Message.StartsWith(
IsKnownCode(ex.Message
SemanticError(ex.Message
exact equality against ex.Message
regex/switch/heuristics over exception text
code-as-message InvalidOperationException for expected Content/Media contract outcomes

Prove no PlatformHttpException remains in Content Application/Infrastructure expected business flow.

DURABLE GUARDS

Add or strengthen guards that fail if R2 regresses:

Content Application/Infrastructure expected failure classification by Exception.Message = ZERO.
Content Application/Infrastructure PlatformHttpException expected-business transport = ZERO.
Media readiness contract uses typed stable semantics, not InvalidOperationException("code").
Content→Media remains Contracts-only.
Endpoints→Infrastructure remains ZERO.
Host/Content remains ZERO.
endpoint business calls remain ISender-based.
ApiResponseFactory remains canonical for expected failures.
unknown exceptions are not swallowed/mapped to business errors.
Created/201 presentation choice is explicitly canonical or explicitly guarded/documented.

FOCUSED VALIDATION

Build/test only affected projects:

Content.Contracts
Content.Application
Content.Infrastructure
Content.Endpoints
Media.Contracts
Media.Infrastructure only if the readiness adapter changes
Host
Host.Tests / focused module tests as required

Run:

R2 architecture guards
existing HostContentAmc guards
Content CQRS/validator/result tests impacted by fault changes
Media readiness contract tests
error catalog/localization uniqueness test
any focused behavior tests required to prove unchanged HTTP status/error code behavior

Tests are evidence, not navigation.
One deterministic local repair per focused failure; rerun affected validation only.
Do not weaken tests or guards.

BEHAVIOR PRESERVATION

Must preserve:

routes
success DTO shape
expected HTTP status codes
stable Content public error codes
permissions
grid behavior
seed behavior
schema/migrations = NONE
frontend = UNCHANGED

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-CONTENT-AMC-001-R2/

Required:

analyze.md
typed-fault-boundaries.md
message-classification-audit.md
created-response.md
validation.md
closure.md

SOT

Update docs/architecture/tmar-current-state.json under:
hostContentAmcR2

Record:

parent task/commit
Host/Content ZERO
typed fault state
message classification state
PlatformHttpException state
Media readiness contract state
Content→Media boundary
Endpoints→Infrastructure state
Result/ApiResponseFactory state
Created/201 decision
focused validation
blockers/debt
schema/frontend state
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_CONTENT_R2_CHECKPOINT

Persist this task:
docs/ai/tasks/TB-TMAR-HOST-CONTENT-AMC-001-R2.task.md

PASS CRITERIA

PASS only if ALL hold:

Host/Content = ZERO
Content.Endpoints -> Content.Infrastructure = ZERO
Content -> Media = Contracts-only
expected-failure exception-message classification = ZERO
IsKnownCode(ex.Message) = ZERO
SemanticError(ex.Message) = ZERO
code-as-message InvalidOperationException for touched expected outcomes = ZERO
Content Application/Infrastructure expected business PlatformHttpException = ZERO
Media readiness boundary uses typed stable semantics
typed expected faults convert by stable Code or equivalent typed result
unknown exceptions propagate
CQRS/MediatR/ISender remains intact
validator coverage remains intact
Result/ApiResponseFactory remains canonical
Created/201 path is canonical or explicitly justified/guarded
error descriptor ownership remains unique
localization remains canonical
no schema/migration/frontend change
focused builds/tests/guards pass
evidence + SoT + task persisted
commit pushed
HEAD == origin/main
working tree clean
next Host folder NOT started

If any certification prerequisite remains, Status MUST NOT be PASS and it MUST NOT be moved to residual debt.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-CONTENT-AMC-001-R2
Parent-Task: TB-TMAR-HOST-CONTENT-AMC-001-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Host-Content-Zero-State:
Content-CQRS-State:
MediatR-State:
Message-Classification-State:
PlatformHttpException-State:
Typed-Fault-State:
Media-Readiness-Contract-State:
Content-To-Media-Boundary-State:
Endpoints-To-Infrastructure-State:
Result-Pipeline-State:
ApiResponseFactory-State:
Created-201-State:
Validator-Coverage-State:
Error-Catalog-State:
Localization-State:
Unknown-Exception-Propagation-State:
Schema-Migration-State:
Frontend-State:
Focused-Validation:
Known-PreExisting-Failures:
Remaining-Blockers:
Residual-Debt:
SoT-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree:
User-Work-Preserved:
Next-Host-Folder-Started: false
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After returning the canonical result:

STOP completely.
Do not inspect/start the next Host folder.
Do not self-issue another task.
Do not poll.
Wait for Architect review.

END_TOOBA_TASK
