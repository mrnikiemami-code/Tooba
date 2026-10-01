PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ERRORS-AMC-001-W1
Parent-Task: TB-TMAR-HOST-ERRORS-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ERRORS_CANONICAL_PRESENTATION_CLOSURE
Title: Remove legacy PlatformExceptionMapper, route MultiTenancy through canonical presentation, and align Host/Errors namespace
Estimated-Time-Minutes: 17
Hard-Timebox-Minutes: 20

ARCHITECT REVIEW STATE

Parent Analyze:
ACCEPTED

Architect independently verified:

Host/Errors exact production files = 2
PlatformExceptionMapper is ACTIVE only through MultiTenancy.TenantResolutionMiddleware.WriteProblemAsync
MappedPlatformError exists only for that mapper
ToobaExceptionHandler is the active global IExceptionHandler
canonical presentation authority is IExceptionPresentationService / ExceptionPresentationService
PlatformExceptionMapper contains a parallel direct ProblemDetails path
PlatformExceptionMapper forces SemanticException to 400 if reached
mapper contains hard-coded runtime English titles
both Host/Errors files currently violate path↔namespace:
physical /Errors vs namespace Tooba.Host
Host/Security certification preserved
Host/Admin certification preserved
latest accepted implementation remains:
baa05e6b7fa373cb6d354a80ca2eab37d4472f7b

ADDITIONAL ARCHITECT DECISION

The parent migration plan proposed routing MultiTenancy to the canonical presentation path,
but left MultiTenancy's hard-coded PlatformHttpException titles out of scope.

That is NOT sufficient under the current architecture lock.

Because W1 must touch TenantResolutionMiddleware to retire PlatformExceptionMapper,
the touched failure path MUST NOT retain hard-coded runtime user-facing titles.

Therefore W1 also canonicalizes the three tenant/platform resolution error codes used by
TenantResolutionMiddleware into Foundation descriptors/resources and code-based SemanticException failures.

This is a bounded dependency-seam repair required to close Errors cleanly.
It does NOT open or certify the MultiTenancy folder.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

MIGRATE ONLY.

Do NOT certify Host/Errors in W1.
Do NOT start Host/MultiTenancy recovery.
Do NOT globally remove PlatformHttpException.
Do NOT redesign ExceptionPresentationService.
Do NOT start another Host folder.

PRIMARY ACTIVE FOLDER

src/backend/Host/Tooba.Host/Errors/

DEPENDENCY-SEAM FILE TOUCHED ONLY AS REQUIRED

src/backend/Host/Tooba.Host/MultiTenancy/TenantResolutionMiddleware.cs

Foundation files may be touched only for the three platform-resolution error codes/resources.

IN-SCOPE PRODUCTION FILES

Host/Errors:

PlatformExceptionMapper.cs DELETE
ToobaExceptionHandler.cs KEEP + namespace repair

Host/MultiTenancy:

TenantResolutionMiddleware.cs canonical presentation seam + code-based failures only

BuildingBlocks Foundation:

Presentation/Errors/FoundationErrorCodes.cs
Presentation/Errors/FoundationErrorCatalogContributor.cs
Localization/Resources/FoundationErrors.resx
Localization/Resources/FoundationErrors.fa.resx

No other production files without deterministic compile necessity.

ARCHITECTURE DECISION

Final Host/Errors production shape after W1:

Errors/

ToobaExceptionHandler.cs

Exact namespace:
Tooba.Host.Errors

Final production file count:
1

PlatformExceptionMapper:
REMOVED

MappedPlatformError:
REMOVED

Canonical active presentation path:
ASP.NET ExceptionHandler
→ ToobaExceptionHandler
→ IExceptionPresentationService
→ ExceptionPresentationService
→ ISafeErrorMapper
→ ApiResponseFactory
→ IProblemDetailsService

TenantResolutionMiddleware early fail-closed path:
directly call IExceptionPresentationService.WriteAsync
with a canonical code-based exception.

No direct ProblemDetails construction remains in Host/Errors or MultiTenancy.

REQUIRED W1 CHANGES

DELETE LEGACY MAPPER

Delete:

src/backend/Host/Tooba.Host/Errors/PlatformExceptionMapper.cs

This removes:

PlatformExceptionMapper
MappedPlatformError
direct ProblemDetails construction
forced SemanticException→400 branch
hard-coded "Bad Request"
hard-coded "Internal Server Error"
legacy PlatformHttpException.Title passthrough presentation

After deletion:

stale production references = ZERO
stale test references = ZERO
no shim
no alias
no replacement mapper
TOOBAEXCEPTIONHANDLER PATH↔NAMESPACE REPAIR

Keep file physically at:

src/backend/Host/Tooba.Host/Errors/ToobaExceptionHandler.cs

Change namespace to:

Tooba.Host.Errors

Update Program using/registration references as required.

Do NOT move file back to Host root to preserve old namespace.
Exact physical path-derived namespace is mandatory.

ToobaExceptionHandler remains:

thin
IExceptionHandler only
delegates solely to IExceptionPresentationService
no local mapping
no hard-coded title
no logging duplication
no catalog authority
MULTITENANCY CANONICAL PRESENTATION SEAM

Current TenantResolutionMiddleware injects:
IProblemDetailsService

Replace that local presentation dependency with:
IExceptionPresentationService

On expected resolution failure:

keep structured warning log if behaviorally required
invoke canonical presentation service
do NOT build ProblemDetails locally
do NOT call ApiResponseFactory locally
do NOT call SafeErrorMapper locally
do NOT use PlatformExceptionMapper
do NOT duplicate trace/correlation logic

Preserve short-circuit behavior:
after writing the problem response, middleware does not continue _next.

CANONICAL PLATFORM RESOLUTION CODES

Add stable Foundation constants exactly:

PlatformEditionUnconfigured = "platform.edition.unconfigured"
PlatformConnectionUnconfigured = "platform.connection.unconfigured"
PlatformResolutionFailed = "platform.resolution.failed"

Do NOT rename strings.

FOUNDATION DESCRIPTORS

Register exactly once:

platform.edition.unconfigured

HTTP 503
Platform classification
Error severity appropriate for platform failure
localization key = code

platform.connection.unconfigured

HTTP 503
Platform classification
Error severity appropriate for platform failure
localization key = code

platform.resolution.failed

HTTP 404
NotFound or Platform classification based on existing canonical enum semantics,
BUT status MUST remain 404 and fail-closed enumeration behavior must remain unchanged.
localization key = code

Architect priority:
preserve observable HTTP status and security semantics over taxonomy cosmetics.

No duplicate descriptor authority.

FOUNDATION EN/FA RESOURCES

Add real EN/FA resources for all three:

platform.edition.unconfigured
platform.connection.unconfigured
platform.resolution.failed

Requirements:

no raw machine code shown when resource exists
no hard-coded title in TenantResolutionMiddleware
Persian resource present
English resource present
keys exact

Suggested semantics only; wording may follow repository conventions:

edition unavailable / service configuration unavailable
connection unavailable / service configuration unavailable
store/site not found for fail-closed 404

For platform.resolution.failed, preserve non-enumerating wording.
Do not reveal tenant existence/status.

MULTITENANCY FAILURE EXCEPTIONS

Replace these hard-coded PlatformHttpException constructions in TenantResolutionMiddleware:

edition unconfigured
connection unconfigured
fail-closed resolution

with code-based:
SemanticException(new SemanticError(FoundationErrorCodes.<...>))

Expected statuses now come from canonical catalog.

After W1 in the touched MultiTenancy failure path:
hard-coded runtime user-facing title text = ZERO.

Do NOT globally remove other PlatformHttpException throw sites elsewhere.

TENANT RESOLUTION BEHAVIOR PARITY

Preserve exactly:

ToobaEdition.Unset => 503
Marketplace missing connection reference => 503
unknown host => 404
inactive tenant => 404
disabled/suspended remain indistinguishable from unknown
connection resolution behavior unchanged
SkipPrefixes unchanged
Activity tags unchanged
logger scope unchanged
StoreCommerce assignment unchanged
tenant resolution order unchanged

No auth/business behavior change.

TRACE / CORRELATION

Before W1 MultiTenancy computes:
Activity.Current?.TraceId ?? HttpContext.TraceIdentifier

Do not create a second trace source after canonical presentation migration.

Existing local traceId may remain if still needed by:

Resolve CommerceContext.TraceId
structured warning log

But presentation itself must rely on canonical IProblemDetailsContextProvider via ExceptionPresentationService.

Do not inject/use custom ProblemDetails trace extension code.

PLATFORMHTTPEXCEPTION COMPATIBILITY

Do NOT remove PlatformHttpException type.

Other production code still uses it.

SafeErrorMapper compatibility remains intact.

This W1 removes only:

the Host/Errors legacy mapper
the three touched MultiTenancy title-based throw sites

No global migration.

TEST MIGRATION

Remove/replace mapper-specific unit test:

PlatformExceptionMapperTests

Do NOT retarget it to test a deleted type.

Replace coverage with tests that verify active canonical behavior:

A. MultiTenancy fail-closed unknown host:

404
errorCode = platform.resolution.failed
traceId present
no detail leakage
no tenant existence disclosure

B. Edition unconfigured:

503
errorCode = platform.edition.unconfigured

C. Marketplace connection unconfigured:

503
errorCode = platform.connection.unconfigured

D. EN/FA localization resolution for all three keys

E. ToobaExceptionHandler still delegates to IExceptionPresentationService

Keep existing:
ErrorContractTests active global integration behavior.

OFFER / ARCHITECTURE GUARD IMPACT

Inspect:
OfferArchitectureGuardTests and all guards referencing:
Errors/ToobaExceptionHandler.cs
or namespace Tooba.Host.

Update only exact namespace expectation if required.

Do NOT weaken path checks.

PATH / NAMESPACE SCOPE

Host/Errors after W1:
EXACT Tooba.Host.Errors

TenantResolutionMiddleware currently resides under MultiTenancy and may itself have historical
namespace Tooba.Host.

This task does NOT certify MultiTenancy and MUST NOT claim its folder/path issue closed.

Because it is a dependency-seam file touched only to remove the Errors mapper consumer:

do not silently mark MultiTenancy structure certified
record any pre-existing MultiTenancy path↔namespace debt as DEFERRED_TO_MULTITENANCY_AMC
do not broaden this wave into MultiTenancy folder recovery
CONTRACT / MICROSERVICE BOUNDARY

Host/Errors final file must have ZERO:

module Application
module Infrastructure
module Domain
module Contracts need unless unavoidable
DbContext
persistence
business authority

MultiTenancy touched seam must not gain foreign business dependencies.

Foundation changes are generic cross-cutting platform errors only.

HARD-CODED RUNTIME TEXT TARGET

After W1:

Host/Errors:
ZERO

Touched TenantResolution failure path:
ZERO

Do not fail W1 for unrelated pre-existing hard-coded text elsewhere outside touched scope.

EXCEPTION MESSAGE CLASSIFICATION

ZERO:

ex.Message classification
exception.Message classification
Message.Contains / StartsWith / Equals error selection
SENSITIVE DATA

Canonical response must not expose:

connection reference
connection string
SQL
internal file path
stack trace
tenant inactive/suspended reason

Production 404 must stay enumeration-safe.

OBSERVABILITY

Preserve:

structured warning log for commerce resolution failure if currently present
canonical ExceptionPresentationService logging behavior

Avoid duplicate logging explosion:
If calling IExceptionPresentationService would create a second warning for the same handled exception,
document exact outcome and choose the minimum one-log design.
Preferred:
single canonical presentation log, unless existing TenantResolution operational warning carries unique
non-sensitive context required by architecture.

Do not create a new logger/ActivitySource/Meter.

HOST/ERRORS STRUCTURE GUARD

Add a bounded durable guard or prepare W2 certification guard foundation proving:

Errors exact file count = 1
only ToobaExceptionHandler.cs
namespace Tooba.Host.Errors
PlatformExceptionMapper absent
MappedPlatformError absent
MultiTenancy no PlatformExceptionMapper reference
Program registers ToobaExceptionHandler through canonical namespace

W1 guard may be migration guard; full certification is W2.

PROTECTED CERTIFICATIONS

Must remain untouched:

HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED

No production changes in those folders.

FOCUSED VALIDATION

Build:

BuildingBlocks
Host
Host.Tests

Focused tests:

tenant resolution error contract tests
ErrorContractTests
Foundation error catalog/localization tests
Errors migration guard
OfferArchitectureGuardTests only if namespace dependency exists
TmarDurableGuard current Recovery assertions

No solution-wide tests.

One deterministic bounded repair + one rerun maximum.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-ERRORS-AMC-001-W1/

Required:

mapper-retirement.md
canonical-presentation.md
tenant-resolution-parity.md
foundation-errors.md
path-namespace.md
boundary-security.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-ERRORS-AMC-001-W1.task.md

RECOVERY / SOT

On PASS:

lastAcceptedTask = TB-TMAR-HOST-ERRORS-AMC-001-W1
lastAcceptedCommit = actual W1 implementation SHA
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-ERRORS-AMC-001-W1
currentHostCheckpoint = Errors

Record:

errorsProductionFileCount = 1
platformExceptionMapper = REMOVED
mappedPlatformError = REMOVED
toobaExceptionHandler = KEEP_THIN_CANONICAL
canonicalPresentationAuthority = IExceptionPresentationService
parallelProblemDetailsPath = ZERO
errorsPathNamespace = EXACT_Tooba.Host.Errors
tenantResolutionPresentation = CANONICAL_IExceptionPresentationService
tenantResolutionPlatformErrors = FOUNDATION_CODE_BASED
tenantResolutionHardcodedRuntimeText = ZERO
multiTenancyStructureCertification = NOT_OPENED_DEFERRED
Host/Security certification = PRESERVED
Host/Admin certification = PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_ERRORS_AMC_001_W1
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Docs stamp separate from implementation SHA if required.

GIT

Latest origin/main.
No reset.
No clean.
No rebase.
No force push.
Preserve unrelated user work.
Commit/push main only on PASS.

SUCCESS CRITERIA

PASS only if:

PlatformExceptionMapper deleted
MappedPlatformError deleted
production stale references ZERO
ToobaExceptionHandler retained
ToobaExceptionHandler namespace exact Tooba.Host.Errors
Program registration compiles through exact namespace
MultiTenancy no direct ProblemDetails construction for resolution failures
MultiTenancy uses IExceptionPresentationService canonical path
three platform resolution codes registered exactly once
HTTP parity 503/503/404 preserved
EN/FA resources resolve
touched failure path hard-coded user-facing text ZERO
fail-closed anti-enumeration behavior preserved
no exception-message classification
no sensitive detail leakage
Host/Errors production file count = 1
Errors path↔namespace exact
MultiTenancy structure not falsely certified
Host/Security certification preserved
Host/Admin certification preserved
schema NONE
frontend UNCHANGED
focused builds/tests PASS
Recovery points actual W1 implementation SHA
automaticNext NONE
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ERRORS-AMC-001-W1
Parent-Task: TB-TMAR-HOST-ERRORS-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Migration-State:
Errors-Production-File-Count:
PlatformExceptionMapper-State:
MappedPlatformError-State:
ToobaExceptionHandler-State:
Errors-Path-Namespace-State:
Canonical-Presentation-Authority-State:
Parallel-ProblemDetails-Path-State:
TenantResolution-Presentation-State:
Platform-Edition-Unconfigured-State:
Platform-Connection-Unconfigured-State:
Platform-Resolution-Failed-State:
Foundation-Descriptor-Uniqueness-State:
Foundation-English-Resources-State:
Foundation-Persian-Resources-State:
Http-Status-Parity-State:
FailClosed-AntiEnumeration-State:
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
MultiTenancy-Structure-Certification-State:
Host-Security-Certification-State:
Host-Admin-Certification-State:
Schema-Change-State:
Frontend-State:
Focused-Build-State:
Focused-Test-State:
Guard-State:
Recovery-State:
Last-Accepted-Commit-State:
Last-Accepted-Commit-Kind:
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

Do not start W2 CERT.
Do not start Host/MultiTenancy AMC.
Do not start another Host folder.
Do not reopen Security/Admin.

Wait for Architect/user review.

END_TOOBA_TASK