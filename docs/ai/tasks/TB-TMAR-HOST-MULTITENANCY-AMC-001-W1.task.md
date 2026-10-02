PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-MULTITENANCY-AMC-001-W1
Parent-Task: TB-TMAR-HOST-MULTITENANCY-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_MULTITENANCY_STRUCTURE_LIFETIME_HYGIENE
Title: Split MultiTenancy types, align namespace, and remove RequestServices service-locator usage while preserving tenant-resolution semantics
Estimated-Time-Minutes: 15
Hard-Timebox-Minutes: 18

ARCHITECT REVIEW STATE

Parent Analyze:
ACCEPTED WITH ONE ARCHITECT CORRECTION

Architect independently verified:

Host/MultiTenancy currently has 1 production file and 2 production types
both types are legitimately Host-owned
HttpCommerceContextAccessor is a thin Host HTTP/request context adapter
TenantResolutionMiddleware is global Host MultiTenancy platform middleware
foreign Application/Infrastructure/Domain/DbContext = ZERO
fail-closed 503/503/404 behavior is current and correct
Host/Errors, Host/Security, Host/Admin certifications remain protected
path↔namespace is currently wrong: Tooba.Host under /MultiTenancy
file cohesion requires split

ARCHITECT CORRECTION TO ANALYZE

The Analyze classified:

httpContext.RequestServices.GetRequiredService<IStoreCommerceContextAssigner>()

as a lifetime-required acceptable bridge.

That conclusion is too permissive.

ASP.NET Core conventional middleware may receive scoped services as parameters of InvokeAsync.
Therefore the scoped IStoreCommerceContextAssigner can be supplied directly by DI per request.

Architecture lock remains:
NO service locator when a canonical DI path exists.

W1 MUST remove the RequestServices.GetRequiredService<IStoreCommerceContextAssigner>() call
and receive IStoreCommerceContextAssigner as an InvokeAsync parameter.

No custom factory is needed.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

MIGRATE ONLY.

Do NOT certify MultiTenancy in W1.
Do NOT open Configuration/Persistence/StoreContext recovery.
Do NOT start another Host folder.
Do NOT reopen Errors/Security/Admin.

PRIMARY ACTIVE FOLDER

src/backend/Host/Tooba.Host/MultiTenancy/

CURRENT SOURCE

Current file:

TenantResolutionMiddleware.cs

Current types:

HttpCommerceContextAccessor
TenantResolutionMiddleware

TARGET PHYSICAL SHAPE

After W1 exactly:

src/backend/Host/Tooba.Host/MultiTenancy/

HttpCommerceContextAccessor.cs
TenantResolutionMiddleware.cs

Exact production file count:
2

Exact namespace for BOTH:
Tooba.Host.MultiTenancy

No aliases.
No shim.
No type forwarding.
No duplicate old copies.

OWNERSHIP LOCK

HttpCommerceContextAccessor:
KEEP_AS_THIN_HOST_CONTEXT_ADAPTER

TenantResolutionMiddleware:
KEEP_AS_GLOBAL_HOST_MULTITENANCY_PLATFORM

Do NOT move either to StoreContext or BuildingBlocks.

StoreContext remains owner of:

StoreCommerceContext
ICurrentStoreCommerceContext
IStoreCommerceContextAssigner
neutral store-commerce semantics

Host remains owner of:

HTTP host→tenant resolution
HttpContext-backed commerce context adapter
request pipeline context population

REQUIRED W1 CHANGES

SPLIT HTTPCOMMERCECONTEXTACCESSOR

Move type into:

src/backend/Host/Tooba.Host/MultiTenancy/HttpCommerceContextAccessor.cs

Namespace:
Tooba.Host.MultiTenancy

Preserve exactly:

internal sealed
interfaces:
ICurrentCommerceContext
ICurrentEdition
ICurrentTenant
ICommerceContextAssigner
scoped DI semantics
ItemKey value:
"Tooba.CommerceContext"
Current priority:
_assigned ?? HttpContext.Items[ItemKey]
Assign null guard
explicit ICurrentEdition / ICurrentTenant projections

Do NOT change public/behavioral semantics.

TENANTRESOLUTIONMIDDLEWARE FILE

Keep:

src/backend/Host/Tooba.Host/MultiTenancy/TenantResolutionMiddleware.cs

Namespace:
Tooba.Host.MultiTenancy

Remove HttpCommerceContextAccessor definition from this file.

Preserve middleware behavior exactly except DI hygiene described below.

REMOVE REQUESTSERVICES SERVICE LOCATOR

Current prohibited pattern:

httpContext.RequestServices.GetRequiredService<IStoreCommerceContextAssigner>()

Target:

InvokeAsync(HttpContext httpContext, IStoreCommerceContextAssigner storeCommerceAssigner)

or the exact conventional middleware signature accepted by ASP.NET Core.

Then:

storeCommerceAssigner.Assign(storeCommerce);

Required:

no RequestServices in Host/MultiTenancy production code
no IServiceProvider injection
no factory abstraction introduced
no scoped service injected into middleware constructor
conventional middleware lifetime remains valid
PROGRAM NAMESPACE / DI

Update Program as required:

Add/import:
using Tooba.Host.MultiTenancy;

Preserve DI registrations:

HttpCommerceContextAccessor scoped
ICurrentCommerceContext -> same scoped accessor
ICurrentEdition -> same scoped accessor
ICurrentTenant -> same scoped accessor
ICommerceContextAssigner -> same scoped accessor

Preserve:
app.UseMiddleware<TenantResolutionMiddleware>();

Do NOT change lifetimes.

CONTEXT ASSIGNMENT INVARIANT

Preserve HTTP success path order:

A. Resolve CommerceContext + StoreCommerceContext
B. write CommerceContext to HttpContext.Items
C. assign StoreCommerceContext through IStoreCommerceContextAssigner
D. set canonical Activity tags
E. BeginScope
F. call next middleware

No context assignment reordering unless required by compiler with exact semantic parity.

TENANT RESOLUTION SEMANTICS — LOCKED

Preserve exact behavior:

Edition Unset:

503
platform.edition.unconfigured

Marketplace:

MarketplaceConnectionReference missing => 503 platform.connection.unconfigured
uses DeploymentStoreCommerce
no tenant Host lookup

SingleStore:

normalize Request.Host through HostNormalizer
host allowlist lookup from ControlPlaneRegistry.Hosts
unknown host => 404 platform.resolution.failed
inactive/disabled/suspended => same 404/code
active tenant only continues
connection reference resolve behavior unchanged
tenant StoreCommerce unchanged

No tenant-id header becomes source of truth.

ANTI-ENUMERATION

Must preserve:

unknown tenant host
disabled tenant
suspended/non-active tenant

all externally indistinguishable:

same 404
same platform.resolution.failed
no status/existence detail
no connection reference
no connection string
SKIP PREFIXES

Preserve exactly:

/health
/ready
/__platform-error
/__platform-conflict

Preserve StartsWithSegments behavior.

Do NOT add/remove prefixes.

CANONICAL ERROR PRESENTATION

Preserve Errors W1/W2 contract:

uses IExceptionPresentationService
SemanticException + FoundationErrorCodes
no local ProblemDetails
no IProblemDetailsService
no PlatformExceptionMapper
no hard-coded user-facing title
no ex.Message classification
OBSERVABILITY

Preserve:

current traceId source
Activity tags:
tooba.edition
tooba.deployment
tooba.tenant_id
structured BeginScope fields:
Edition
DeploymentId
TenantId

No new ActivitySource.
No new Meter.
No custom correlation.

Do not log connection strings/references.

LOGGING

Preserve current operational warning behavior unless a direct focused test proves canonical logging duplicates create an actual semantic issue.

Do NOT redesign logging in W1.

No raw exception.Message in log templates.

PATH ↔ NAMESPACE

After W1:

HttpCommerceContextAccessor.cs
→ namespace Tooba.Host.MultiTenancy

TenantResolutionMiddleware.cs
→ namespace Tooba.Host.MultiTenancy

Program/tests usings updated accordingly.

State:
EXACT

DEPENDENCY BOUNDARY

Both files must remain ZERO for:

module Application
module Infrastructure
module Domain
foreign DbContext
persistence repository
cross-module persistence
business command
module business state mutation

Allowed:

ASP.NET
BuildingBlocks neutral contracts
StoreContext.Contracts
Host Configuration types already used by tenant registry
Host logging/presentation seams
CONFIGURATION / PERSISTENCE ADJACENT OWNERSHIP

Do NOT move:

ControlPlaneRegistry
IDatabaseConnectionResolver implementation
PlatformOptionsValidator
HostNormalizer

Do not certify their folders in W1.

Record them only as adjacent dependencies.

HOST ERRORS GUARD IMPACT

Errors W1/W2 guards currently source-scan:
MultiTenancy/TenantResolutionMiddleware.cs

That file remains, so preserve their assertions.

If they assume old namespace Tooba.Host, update ONLY namespace-specific expectations as required.

HostErrorsAmcCertGuardTests must still PASS.

MULTITENANCY STRUCTURE GUARD

Add:
HostMultiTenancyAmcW1GuardTests

At minimum lock:

exact two production files
exact two production types
exact namespace Tooba.Host.MultiTenancy
no RequestServices
no GetRequiredService<IStoreCommerceContextAssigner>
InvokeAsync receives IStoreCommerceContextAssigner
accessor implements exact four BuildingBlocks contracts
ItemKey unchanged
Program exact scoped registrations
middleware registration present
skip prefixes unchanged
canonical presentation path unchanged
PlatformExceptionMapper absent
no hard-coded user-facing runtime text
no ex.Message classification
foreign App/Infra/Domain/DbContext zero

Do not call this a certification guard yet.
Full certification is W2-CERT.

FOCUSED TESTS

Run focused:

TenantResolutionTests
TenantResolutionPlatformErrorTests
HostErrorsAmcW1GuardTests
HostErrorsAmcCertGuardTests
HostMultiTenancyAmcW1GuardTests
PlatformOptionsValidatorTests only if compile/reference changes require
TmarDurableGuard current-state assertions

Add a focused DI/lifetime test if needed proving:

scoped IStoreCommerceContextAssigner resolves per request through InvokeAsync parameter
middleware activation succeeds

No solution-wide tests.

BUILD

Focused:

Tooba.Host
Tooba.Host.Tests

No solution-wide build unless required by deterministic compiler failure.

PROTECTED CERTIFICATIONS

Must remain:

HOST_ERRORS_AMC_CERTIFIED
HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED

No production edits under:

Host/Errors
Host/Security
Host/Admin
HOST-ONLY SCOPE LOCK

User explicitly requested:
FINISH HOST ONLY.

Do NOT start any module recovery.

Do NOT modify module production code in W1.

StoreContext module may be inspected only for interface contract truth.
No StoreContext production edit.

PRODUCTION FILE COUNT

Before:
1 file / 2 types

After:
2 files / 2 types

This is a structure split, not behavior expansion.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-MULTITENANCY-AMC-001-W1/

Required:

structure-split.md
namespace.md
di-lifetime.md
resolution-parity.md
anti-enumeration.md
boundary-security.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-MULTITENANCY-AMC-001-W1.task.md

RECOVERY / SOT

On PASS:

lastAcceptedTask = TB-TMAR-HOST-MULTITENANCY-AMC-001-W1
lastAcceptedCommit = actual W1 implementation SHA
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-MULTITENANCY-AMC-001-W1
currentHostCheckpoint = MultiTenancy

Record:

multiTenancyProductionFileCount = 2
multiTenancyProductionTypeCount = 2

httpCommerceContextAccessor =
KEEP_AS_THIN_HOST_CONTEXT_ADAPTER

tenantResolutionMiddleware =
KEEP_AS_GLOBAL_HOST_MULTITENANCY_PLATFORM

fileCohesion =
SPLIT_COMPLETE

pathNamespace =
EXACT_Tooba.Host.MultiTenancy

requestServicesServiceLocator =
ZERO

scopedStoreCommerceAssignerInjection =
INVOKEASYNC_PARAMETER

failClosedAntiEnumeration =
PRESERVED

skipPrefixes =
PRESERVED_EXACT

canonicalPresentation =
IExceptionPresentationService_PRESERVED

Host/Errors certification =
HOST_ERRORS_AMC_CERTIFIED_PRESERVED

Host/Security certification =
HOST_SECURITY_AMC_CERTIFIED_PRESERVED

Host/Admin certification =
HOST_ADMIN_FULLY_CERTIFIED_PRESERVED

automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_MULTITENANCY_AMC_001_W1
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Certification state:
NOT_CERTIFIED_W2_REQUIRED

Docs stamp separate from implementation SHA if needed.

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

file split complete
exact two files
exact two types
namespace exact Tooba.Host.MultiTenancy
RequestServices zero
GetRequiredService service locator zero
scoped IStoreCommerceContextAssigner injected through InvokeAsync
DI lifetimes preserved
context assignment order preserved
503/503/404 matrix preserved
anti-enumeration preserved
skip prefixes exact
canonical presentation preserved
hard-coded runtime user-facing text zero
message classification zero
foreign App/Infra/Domain/DbContext zero
no module production edits
Host/Errors cert preserved
Host/Security cert preserved
Host/Admin cert preserved
focused builds/tests PASS
W1 guard PASS
implementation SHA advanced to actual W1 implementation
automatic next NONE
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-MULTITENANCY-AMC-001-W1
Parent-Task: TB-TMAR-HOST-MULTITENANCY-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Migration-State:
MultiTenancy-Production-File-Count:
MultiTenancy-Production-Type-Count:
Structure-Split-State:
HttpCommerceContextAccessor-State:
TenantResolutionMiddleware-State:
Path-Namespace-State:
RequestServices-State:
ServiceLocator-State:
Scoped-StoreCommerce-Assigner-State:
DI-Lifetime-State:
Context-Assignment-Parity-State:
Tenant-Resolution-Parity-State:
FailClosed-AntiEnumeration-State:
Skip-Prefix-State:
Canonical-Presentation-State:
Hardcoded-Runtime-User-Facing-Text-State:
Exception-Message-Classification-State:
Sensitive-Data-State:
Observability-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
W1-Guard-State:
Host-Errors-Certification-State:
Host-Security-Certification-State:
Host-Admin-Certification-State:
Schema-Change-State:
Frontend-State:
Focused-Build-State:
Focused-Test-State:
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

Do not start W2-CERT.
Do not start another Host folder.
Do not open module recovery.
Do not reopen Errors/Security/Admin.

Wait for Architect/user review.

END_TOOBA_TASK