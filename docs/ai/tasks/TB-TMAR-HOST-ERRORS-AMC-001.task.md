PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ERRORS-AMC-001
Parent-Task: TB-TMAR-HOST-SECURITY-AMC-001-W3-CERT
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ERRORS_ANALYZE
Title: Analyze Host/Errors global exception boundary and dead legacy mapper residue
Estimated-Time-Minutes: 10
Hard-Timebox-Minutes: 14

ARCHITECT REVIEW STATE

Parent Security certification:
ACCEPTED

Architect independently verified:

HOST_SECURITY_AMC_CERTIFIED
KEEP_THIN_PLATFORM_SECURITY_BOUNDARY_CERTIFIED_CURRENT_19
exact Security tree = 19
Seller = 14
dead Security adapters = 0
W1 implementation authority remains:
baa05e6b7fa373cb6d354a80ca2eab37d4472f7b
W3 certification commit:
73a80ee28ed9dc054be5adae0f7115e72c115ded
W3 tip/stamp commit:
bd033d38f968c45a29b9e74b88b685c4db256bbf
Host/Admin FULLY_CERTIFIED preserved
Security production changed ZERO during Cert

NEXT ACTIVE RECOVERY UNIT

src/backend/Host/Tooba.Host/Errors/

Current repository snapshot shows exactly two production files:

PlatformExceptionMapper.cs
ToobaExceptionHandler.cs

Do NOT assume this remains exact; re-enumerate from disk.

IMPORTANT ARCHITECT OBSERVATION

Program.cs currently registers:

IExceptionPresentationService -> ExceptionPresentationService
AddExceptionHandler<ToobaExceptionHandler>()

Program.cs contains NO reference to PlatformExceptionMapper.

ToobaExceptionHandler delegates directly to the canonical
IExceptionPresentationService.

PlatformExceptionMapper still contains a legacy parallel mapping path:

PlatformHttpException -> platform.Title
SemanticException -> hard-coded HTTP 400 + semantic.Error.Code as title
BadHttpRequestException -> hard-coded "Bad Request"
unknown -> hard-coded "Internal Server Error"
builds ProblemDetails directly

This strongly suggests dead/legacy residue, but ANALYZE must prove consumers before deletion.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-analyze/SKILL.md

ANALYSIS ONLY.

Do NOT edit production code.
Do NOT delete PlatformExceptionMapper yet.
Do NOT certify.
Do NOT start another Host folder.
Do NOT reopen Security/Admin.

PROTECTED ACCEPTED STATE

Must remain untouched:

HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED
Foundation canonical error presentation
Identity authentication boundary
module endpoint presentation registrations
frontend frozen

MANDATORY ANALYSIS

EXACT FILE ENUMERATION

Enumerate:
src/backend/Host/Tooba.Host/Errors/

Record:

exact production .cs count
exact filenames
namespaces
types per file

Expected current:
2 files

PRODUCTION CONSUMER AUDIT

For:

PlatformExceptionMapper
MappedPlatformError
ToobaExceptionHandler

Find:

all production references
Program registrations
tests only references
docs/guard references separately

No consumer guessing.

Explicitly answer:

Is PlatformExceptionMapper production-dead?
Is MappedPlatformError production-dead?
Is ToobaExceptionHandler the sole active Host exception boundary?
Is IExceptionPresentationService the canonical presentation authority?
PLATFORMEXCEPTIONMAPPER SEMANTIC AUDIT

If active or test-referenced, classify its behavior:

PlatformHttpException uses Title
SemanticException forced to 400
SemanticError.Code used as title
BadHttpRequestException hard-coded "Bad Request"
unknown hard-coded "Internal Server Error"
direct ProblemDetails construction
direct traceId/errorCode extension
Development detail behavior

Compare against:
BuildingBlocks ExceptionPresentationService
and ApiResponseFactory/error catalog/localizer behavior.

Determine whether it is:

DEAD_ZERO_CONSUMER_RESIDUE
ACTIVE_LEGACY_PARALLEL_PRESENTATION
KEEP_AS_GLOBAL_HOST_ERROR_PLATFORM
MUST_SPLIT
BLOCKED
TOOBAEXCEPTIONHANDLER AUDIT

Verify:

active DI registration
IExceptionHandler role
delegates only to IExceptionPresentationService
no local classification
no hard-coded user-facing text
no ex.Message classification
no sensitive detail leakage
no duplicate catalog/localizer authority
no module business authority

Likely disposition:
KEEP_AS_THIN_GLOBAL_HOST_EXCEPTION_ADAPTER
but prove independently.

CANONICAL PRESENTATION AUTHORITY

Trace current production exception path:

ASP.NET exception middleware
→ ToobaExceptionHandler
→ IExceptionPresentationService
→ ExceptionPresentationService
→ catalog/localizer/safe mapper as applicable

Document exact actual chain.

Verify:

only one active global exception presentation authority
no second direct ProblemDetails pipeline in Host/Errors
no message-text code selection
unknown exception safe behavior
development detail leakage constraints
HARD-CODED RUNTIME TEXT

Audit both files for user-facing runtime prose.

PlatformExceptionMapper currently visibly contains:

"Bad Request"
"Internal Server Error"

Determine:

active runtime debt
dead-code-only debt
whether deletion is preferable to localization repair

Do NOT repair in Analyze.

SEMANTIC STATUS CORRECTNESS

PlatformExceptionMapper currently maps every SemanticException to 400.

Compare with current catalog where SemanticError codes may map to:
401 / 403 / 404 / 409 / 429 / 503 etc.

If mapper is active, this is a material semantics bug.
If mapper is dead, classify as stale dangerous residue.

Document exact conclusion.

PLATFORMHTTPEXCEPTION LEGACY PATH

Audit whether active global error presentation still needs PlatformHttpException compatibility.

Do not globally remove PlatformHttpException.

Answer:

Are there remaining production throw sites outside Host/Errors?
Does ExceptionPresentationService already support them?
Does PlatformExceptionMapper add any unique required compatibility?

Only analyze; do not expand scope.

DEPENDENCY BOUNDARY

Host/Errors must have ZERO:

module Application
module Infrastructure
module Domain
DbContext
persistence
business commands
module-specific policy

Allowed:

ASP.NET exception infrastructure
BuildingBlocks presentation contracts
SENSITIVE DATA / SECURITY

Audit:

stack traces
exception.Message exposure
SQL/file path/connection string leakage
Development-only detail
traceId safety

Verify active path fails safe in Production.

OBSERVABILITY / CORRELATION

Audit:

traceId source
custom correlation
ActivitySource/Meter
duplicated trace ID logic
custom logging

No new mechanism.

PATH / NAMESPACE

Current files physically under:
Host/Errors/

but namespace shown by repo currently is:
Tooba.Host

This potentially violates strict path↔namespace lock.

Classify precisely:

VIOLATION requiring future move/namespace repair
intentional global Host namespace historical exception
blocked due widespread call sites

Do NOT automatically accept mismatch.

Our architecture lock says exact physical path ↔ namespace unless explicitly justified and certified.

If ToobaExceptionHandler and PlatformExceptionMapper use Tooba.Host while path is /Errors,
record as structural debt.

GUARD / TEST INVENTORY

Find all tests/guards referencing:

Host/Errors
PlatformExceptionMapper
ToobaExceptionHandler
exception presentation

Identify stale expectations if PlatformExceptionMapper is dead.

DECISIVE MIGRATION PLAN

Recommend fewest safe waves <=20 min.

Likely if evidence confirms:
W1:

delete dead PlatformExceptionMapper + MappedPlatformError
move/namespace ToobaExceptionHandler to exact Tooba.Host.Errors
update Program using/registration if required
update exact guards/tests
preserve active exception behavior exactly

W2:

CERT Host/Errors thin global exception boundary

But do not force this if current consumers prove mapper active.

Every wave:

exact files
exact behavior preserved
exact tests/guards
exact time estimate
NO SCOPE EXPANSION

Do NOT:

redesign BuildingBlocks ExceptionPresentationService
redesign ApiResponseFactory
rewrite module error catalogs
fix unrelated reservation.policy duplicate
change endpoint responses
change schema/frontend
reopen Authentication
change production in Analyze

FOCUSED VALIDATION

No solution-wide tests.
No production build required unless a consumer ambiguity cannot be resolved statically.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-ERRORS-AMC-001/

Required:

analyze.md
consumers.md
presentation-authority.md
semantic-status.md
boundary-security.md
path-namespace.md
guard-impact.md
migration-plan.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-ERRORS-AMC-001.task.md

RECOVERY / SOT

Analysis-only.

Do NOT advance latest implementation SHA.

Record:

currentHostCheckpoint = Errors
errorsProductionFileCount = actual
PlatformExceptionMapper disposition
ToobaExceptionHandler disposition
canonicalPresentationAuthority
pathNamespaceState
recommendedNextTask
Host/Security certification = PRESERVED
Host/Admin certification = PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_ERRORS_AMC_001
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Latest accepted implementation remains:
baa05e6b7fa373cb6d354a80ca2eab37d4472f7b

GIT

Latest origin/main.
No reset.
No clean.
No rebase.
No force push.

Docs-only task/evidence/SoT stamp allowed.
No production code change.

Preserve unrelated user work.

SUCCESS CRITERIA

PASS only if:

exact Errors tree enumerated
all production consumers proven
mapper active/dead state proven
ToobaExceptionHandler active role proven
canonical exception presentation chain documented
hard-coded runtime text classified
SemanticException status mismatch risk classified
PlatformHttpException compatibility classified
path↔namespace debt classified
sensitive data exposure audited
observability/correlation audited
boundary clean
exact guard/test impact documented
fewest safe migration waves proposed
Security/Admin certifications preserved
production ZERO
implementation SHA unchanged
automatic next NONE
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ERRORS-AMC-001
Parent-Task: TB-TMAR-HOST-SECURITY-AMC-001-W3-CERT
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Mode-State:
Active-Host-Folder:
Production-File-Count:
File-Enumeration-State:
PlatformExceptionMapper-State:
MappedPlatformError-State:
ToobaExceptionHandler-State:
Production-Consumer-Audit-State:
Canonical-Presentation-Authority-State:
Active-Global-Exception-Path-State:
Parallel-ProblemDetails-Path-State:
Hardcoded-Runtime-Text-State:
SemanticException-Status-Mapping-State:
PlatformHttpException-Compatibility-State:
Exception-Message-Classification-State:
Sensitive-Detail-Leakage-State:
TraceId-State:
Observability-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
Path-Namespace-State:
Guard-Impact-State:
Recommended-Wave-Count:
Recommended-Next-Task:
Production-Code-Change-State:
Host-Security-Certification-State:
Host-Admin-Certification-State:
Recovery-State:
Last-Accepted-Implementation-Commit-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.

Do not start Errors W1.
Do not start another Host folder.
Do not reopen Security/Admin.

Wait for Architect/user review.

END_TOOBA_TASK