PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT
Parent-Task: TB-TMAR-HOST-ERRORS-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ERRORS_CERTIFY
Title: Independently certify Host/Errors as a one-file thin canonical global exception boundary
Estimated-Time-Minutes: 11
Hard-Timebox-Minutes: 15

ARCHITECT REVIEW STATE

W1:
ACCEPTED FOR CERTIFICATION ENTRY

Architect independently verified:

PlatformExceptionMapper physically removed
MappedPlatformError removed
Host/Errors now contains exactly one production file
ToobaExceptionHandler namespace = Tooba.Host.Errors
Program registers canonical ToobaExceptionHandler
MultiTenancy no longer directly constructs ProblemDetails for resolution failures
MultiTenancy routes expected resolution failures through IExceptionPresentationService
platform.edition.unconfigured = 503
platform.connection.unconfigured = 503
platform.resolution.failed = 404
all three are Foundation code/descriptor/resource-backed
anti-enumeration behavior preserved for unknown/inactive tenants
Host/Security certification preserved
Host/Admin certification preserved

W1 implementation commit:
e190e213c491fd530d86c7e5680cb0607b5e98d3

W1 later docs/SoT stamp lineage:
4a379419389f58903d8d1bc5d7e7f33368bfe8e6
f35dae31d1c5748d36c2ac3d632dc26484076e1d

BOUNDED DEPENDENCY-SEAM NOTE

W1 also removed three duplicate reservation.policy.* descriptor registrations from:
Tooba.Order.Endpoints/Errors/OrderErrorCatalogContributor.cs

Architect independently verified:

Catalog already owns the same three stable machine codes
HTTP remains 400
Order keeps stable codes/resources
this was required because canonical presentation now resolves the composed error catalog
this is NOT an Order recovery wave
no Order business/domain/persistence migration was opened

W2 CERT MUST re-verify this caused no duplicate/missing descriptor regression,
but MUST NOT make further module production changes.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-certify/SKILL.md

CERTIFY ONLY.

No production migration.
No new Host folder.
No MultiTenancy certification.
No module recovery.
No further Order/Catalog production changes.

PRIMARY CERTIFICATION SCOPE

src/backend/Host/Tooba.Host/Errors/

Expected exact production tree:

Errors/

ToobaExceptionHandler.cs

Expected count:
1

Expected namespace:
Tooba.Host.Errors

Expected classification:
KEEP_AS_THIN_GLOBAL_HOST_EXCEPTION_ADAPTER

Certification label on PASS:
HOST_ERRORS_AMC_CERTIFIED

Boundary label:
HOST_ERRORS_CANONICAL_GLOBAL_EXCEPTION_BOUNDARY_CERTIFIED

MANDATORY CERTIFICATION AUDITS

EXACT TREE

Re-enumerate from disk.

Verify:

Errors directory present
exactly 1 .cs
exact filename ToobaExceptionHandler.cs
PlatformExceptionMapper absent
MappedPlatformError absent
no alias/shim/TypeForwardedTo
no hidden duplicate copy elsewhere in Host production
PATH ↔ NAMESPACE

Verify:
path:
Host/Tooba.Host/Errors/ToobaExceptionHandler.cs

namespace:
Tooba.Host.Errors

State:
EXACT

ACTIVE CONSUMER / DI

Verify:

Program has AddExceptionHandler<ToobaExceptionHandler>()
Program resolves exact namespace via using or fully-qualified reference
UseExceptionHandler pipeline remains active
no duplicate IExceptionHandler registrations for the same global boundary
ToobaExceptionHandler is actually reachable production code
HANDLER THINNESS

ToobaExceptionHandler must:

implement IExceptionHandler
inject only canonical presentation dependency needed
delegate to IExceptionPresentationService.WriteAsync
return true after writing
contain no local status mapping
contain no ProblemDetails construction
contain no hard-coded runtime title/message
contain no catalog lookup
contain no local logging
contain no business logic
CANONICAL PRESENTATION AUTHORITY

Independently verify current active chain:

UseExceptionHandler
→ ToobaExceptionHandler
→ IExceptionPresentationService
→ ExceptionPresentationService
→ ISafeErrorMapper
→ ApiResponseFactory
→ IProblemDetailsService

Required:
single global canonical presentation authority.

PARALLEL PRESENTATION RESIDUE

Across Host production search for:

PlatformExceptionMapper
MappedPlatformError

Required:
ZERO.

For MultiTenancy resolution failure path verify:

no local ProblemDetails construction
no direct IProblemDetailsService use for those failures
no local mapper
IExceptionPresentationService used

Do NOT certify the rest of MultiTenancy.

FOUNDATION PLATFORM RESOLUTION ERROR MATRIX

Verify exact descriptors:

platform.edition.unconfigured

503
Foundation-owned exactly once
EN resource
FA resource

platform.connection.unconfigured

503
Foundation-owned exactly once
EN resource
FA resource

platform.resolution.failed

404
Foundation-owned exactly once
EN resource
FA resource

No fallback-only result.

FAIL-CLOSED ANTI-ENUMERATION

Re-verify touched MultiTenancy behavior only:

unknown host => 404 platform.resolution.failed
disabled tenant => same 404/code
suspended tenant => same 404/code
no existence/status distinction exposed
no connection reference exposed
no internal detail exposed

This is behavior protection, NOT MultiTenancy certification.

COMPOSED ERROR CATALOG INTEGRITY

Because W1 surfaced/fixed the pre-existing duplicate registration:

Verify composed relevant catalog has EXACTLY ONE descriptor each for:

reservation.policy.initial.invalid
reservation.policy.retry.invalid
reservation.policy.max.invalid

Verify:

owner = Catalog contributor
HTTP = 400
no Order duplicate descriptor remains
no machine code removed from Order behavior paths
localization still resolves EN/FA where expected

No further Catalog/Order code edits are allowed in Cert.

If missing or behavior changed:
STOP INCOMPLETE and report exact blocker.

HARDCODED RUNTIME TEXT

Host/Errors:
ZERO hard-coded user-facing runtime prose.

Touched MultiTenancy resolution failure path:
ZERO hard-coded user-facing title text.

Foundation resources are allowed localized presentation sources.

EXCEPTION MESSAGE CLASSIFICATION

Host/Errors:
ZERO.

Touched MultiTenancy resolution path:
ZERO.

Forbidden:

ex.Message classification
exception.Message classification
Message.Contains / StartsWith / Equals code selection
SENSITIVE DETAIL

Verify canonical error responses do not expose:

stack trace
connection string
connection reference
SQL
internal path
tenant status/existence
exception.Message
TRACE / CORRELATION

Verify:

canonical presentation uses IProblemDetailsContextProvider
traceId remains present
no duplicate custom correlation system in Host/Errors
no custom ActivitySource/Meter in Host/Errors
LOGGING

Verify:

ToobaExceptionHandler adds no duplicate logging
ExceptionPresentationService remains canonical presentation logger
MultiTenancy warning, if retained, is structured and non-sensitive
no secret/header/token logging

Do not redesign logging in Cert.

CONTRACT / MICROSERVICE BOUNDARY

Host/Errors must have ZERO:

module Application
module Infrastructure
module Domain
DbContext
persistence
business command
business policy
module state mutation

Allowed:

ASP.NET exception infrastructure
BuildingBlocks Presentation abstraction
COHESION

ToobaExceptionHandler:
one responsibility only.

No reason to split.

DURABLE CERTIFICATION GUARD

Create:
HostErrorsAmcCertGuardTests

It must lock at minimum:

exact one-file tree
exact namespace
mapper types absent
Program canonical registration
handler delegates to IExceptionPresentationService
no direct ProblemDetails construction in Errors
no hard-coded runtime titles
no message classification
no forbidden module layers
MultiTenancy mapper reference ZERO
platform resolution descriptor/status matrix
EN/FA resources
anti-enumeration focused behavior
Security/Admin certifications preserved in SoT

Do not weaken HostErrorsAmcW1GuardTests.

PROTECTED STATE

Must remain:

HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED

No production changes in:
Host/Security
Host/Admin

MULTITENANCY STATUS

Explicitly keep:

MULTITENANCY_AMC = NOT_OPENED

The touched presentation seam may be reverified only.

Do NOT claim:

MultiTenancy path/namespace certified
MultiTenancy full structure certified
MultiTenancy fully clean

Recommended next Host folder after user/Architect review may be:
Host/MultiTenancy

But automatic next remains NONE.

PRODUCTION CHANGE RULE

Expected:
Production-Code-Change-State = ZERO
Production-Repair-Required-State = NONE

Certification task may modify only:

tests/guards
docs/evidence
SoT/current certification metadata

If production repair is needed:
STOP INCOMPLETE.

FOCUSED VALIDATION

Build:

Host
Host.Tests
BuildingBlocks only if cert test compile requires

Focused tests:

HostErrorsAmcW1GuardTests
HostErrorsAmcCertGuardTests
ErrorContractTests
TenantResolutionTests relevant fail-closed cases
tenant platform error status cases
Foundation catalog/localization focused tests
composed catalog uniqueness for touched codes
TmarDurableGuard current-state assertions

No solution-wide tests.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT/

Required:

certification-summary.md
physical-tree.md
canonical-presentation.md
platform-error-matrix.md
anti-enumeration.md
catalog-integrity.md
boundary-security.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT.task.md

RECOVERY / SOT

On PASS:

Top-level:

lastAcceptedTask = TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT
lastAcceptedCommit remains W1 implementation:
e190e213c491fd530d86c7e5680cb0607b5e98d3
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-ERRORS-AMC-001-W1
currentHostCheckpoint = Errors
nextTask = USER_REVIEW_HOST_ERRORS_AMC_001_W2_CERT
workflowStop = USER_REVIEW_HOST_ERRORS_AMC_001_W2_CERT
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Add/update block:
hostErrorsAmc001W2Cert

Required fields:

certificationState = HOST_ERRORS_AMC_CERTIFIED
boundaryState = HOST_ERRORS_CANONICAL_GLOBAL_EXCEPTION_BOUNDARY_CERTIFIED
errorsProductionFileCount = 1
platformExceptionMapper = ABSENT_CERTIFIED
mappedPlatformError = ABSENT_CERTIFIED
toobaExceptionHandler = THIN_CANONICAL_CERTIFIED
pathNamespace = EXACT_Tooba.Host.Errors
canonicalPresentationAuthority = IExceptionPresentationService
parallelProblemDetailsPath = ZERO
platformResolutionCodes = FOUNDATION_CERTIFIED_503_503_404
hardcodedRuntimeText = ZERO
exceptionMessageClassification = ZERO
foreignApplication = ZERO
foreignInfrastructure = ZERO
foreignDomain = ZERO
foreignDbContext = ZERO
businessAuthority = ZERO
multiTenancyCertification = NOT_OPENED
productionRepairRequired = false
implementationCommit = e190e213...
hostSecurityCertification = HOST_SECURITY_AMC_CERTIFIED_PRESERVED
hostAdminCertification = HOST_ADMIN_FULLY_CERTIFIED_PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_ERRORS_AMC_001_W2_CERT

Certification/docs commit recorded separately from implementation SHA.

GIT

Latest origin/main.
No reset.
No clean.
No rebase.
No force push.
Preserve unrelated user work.
Push only on PASS.

SUCCESS CRITERIA

PASS only if:

Errors exact count = 1
only ToobaExceptionHandler remains
namespace exact Tooba.Host.Errors
PlatformExceptionMapper absent everywhere in Host production
MappedPlatformError absent
active handler registration exact
handler thin
canonical presentation chain exact
parallel ProblemDetails path ZERO
platform resolution 503/503/404 exact
EN/FA resources resolve
anti-enumeration behavior exact
reservation.policy touched composed catalog integrity PASS
hard-coded runtime text ZERO
message classification ZERO
sensitive detail leakage ZERO
trace/correlation canonical
foreign module layers ZERO
business authority ZERO
durable cert guard PASS
Security/Admin certifications preserved
MultiTenancy NOT_OPENED
production changes ZERO
production repair NONE
implementation SHA stays e190e213...
automatic next NONE
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT
Parent-Task: TB-TMAR-HOST-ERRORS-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Certification-State:
Boundary-Certification-State:
Errors-Production-File-Count:
Exact-Tree-State:
Path-Namespace-State:
PlatformExceptionMapper-State:
MappedPlatformError-State:
ToobaExceptionHandler-State:
Handler-DI-State:
Canonical-Presentation-Authority-State:
Parallel-ProblemDetails-Path-State:
Platform-Resolution-Error-Matrix-State:
Foundation-Descriptor-Uniqueness-State:
Localization-Resolution-State:
FailClosed-AntiEnumeration-State:
ReservationPolicy-Catalog-Integrity-State:
Hardcoded-Runtime-User-Facing-Text-State:
Exception-Message-Classification-State:
Sensitive-Detail-Leakage-State:
Trace-Correlation-State:
Logging-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
Cohesion-State:
Durable-Cert-Guard-State:
MultiTenancy-Certification-State:
Host-Security-Certification-State:
Host-Admin-Certification-State:
Production-Repair-Required-State:
Production-Code-Change-State:
Focused-Build-State:
Focused-Test-State:
Guard-State:
Recovery-State:
Last-Accepted-Implementation-Commit-State:
Certification-Commit-State:
Stale-Current-Pointer-State:
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

Do not start MultiTenancy AMC.
Do not start another Host folder.
Do not modify Order/Catalog production code.
Do not reopen Security/Admin.

Wait for Architect/user review.

END_TOOBA_TASK