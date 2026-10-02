PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-MULTITENANCY-AMC-001-W2-CERT
Parent-Task: TB-TMAR-HOST-MULTITENANCY-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_MULTITENANCY_CERTIFY
Title: Independently certify Host/MultiTenancy as thin Host request-context + tenant-resolution platform boundary
Estimated-Time-Minutes: 12
Hard-Timebox-Minutes: 16

ARCHITECT REVIEW STATE

W1:
ACCEPTED FOR CERTIFICATION ENTRY

Architect independently verified:

implementation commit = cfbc94d258de837fdc018ddb29db68233cc25783
exact 2 production files:
HttpCommerceContextAccessor.cs
TenantResolutionMiddleware.cs
exact 2 production types
namespace = Tooba.Host.MultiTenancy for both
RequestServices = ZERO
GetRequiredService<IStoreCommerceContextAssigner> in middleware = ZERO
scoped IStoreCommerceContextAssigner is injected via InvokeAsync
Program keeps scoped accessor registrations
tenant-resolution 503/503/404 parity preserved
anti-enumeration preserved
skip prefixes preserved
canonical IExceptionPresentationService path preserved
Host/Errors cert preserved
Host/Security cert preserved
Host/Admin cert preserved

W1 docs stamp:
5eb4dc2ff93633995c89e2201bef3ddccb60a14b

Later test-only compatibility commit:
c827953b9e587df3b002429b634e7f388ab8d676

That later test change correctly stops the historical Host/Errors cert guard from pinning the global top-level implementation pointer forever. It is test-only and does not alter production semantics.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-certify/SKILL.md

CERTIFY ONLY.

No production migration.
No production redesign.
No next Host folder.
No module recovery.
No StoreContext/Configuration/Persistence relocation.

PRIMARY CERTIFICATION SCOPE

src/backend/Host/Tooba.Host/MultiTenancy/

Expected exact tree:

MultiTenancy/

HttpCommerceContextAccessor.cs
TenantResolutionMiddleware.cs

Expected exact namespace:
Tooba.Host.MultiTenancy

Expected ownership:

HttpCommerceContextAccessor:
KEEP_AS_THIN_HOST_CONTEXT_ADAPTER

TenantResolutionMiddleware:
KEEP_AS_GLOBAL_HOST_MULTITENANCY_PLATFORM

Certification label on PASS:
HOST_MULTITENANCY_AMC_CERTIFIED

Boundary label:
HOST_MULTITENANCY_PLATFORM_BOUNDARY_CERTIFIED

MANDATORY CERTIFICATION AUDITS

EXACT TREE

Re-enumerate from disk.

Verify:

exact .cs count = 2
exact filenames
exact type count = 2
no duplicate old type/file
no alias/shim/TypeForwardedTo
no unexpected subfolder
PATH ↔ NAMESPACE

Both files:
namespace Tooba.Host.MultiTenancy

No exception.

ACCESSOR CERTIFICATION

HttpCommerceContextAccessor must remain:

internal sealed
scoped
implements exactly:
ICurrentCommerceContext
ICurrentEdition
ICurrentTenant
ICommerceContextAssigner
ItemKey unchanged = "Tooba.CommerceContext"
Current = assigned override first, then HttpContext.Items
Assign rejects null
no business logic
no module dependency
no static mutable state
no cross-request state leakage
ACCESSOR DI IDENTITY

Verify Program:

AddScoped<HttpCommerceContextAccessor>()
all four interfaces resolve to same scoped accessor instance
no duplicate concrete registration with conflicting lifetime
no singleton registration
no transient split identity

Add a focused test if missing:
resolve all four interfaces in same scope and prove ReferenceEquals to the same accessor instance.

MIDDLEWARE CERTIFICATION

TenantResolutionMiddleware must remain:

conventional Host middleware
constructor dependencies limited to platform/global seams
IStoreCommerceContextAssigner received per-request through InvokeAsync parameter
no RequestServices
no IServiceProvider
no service locator
no scoped service injected into constructor
CONTEXT ASSIGNMENT ORDER

Certify exact success-path order:

Resolve CommerceContext + StoreCommerceContext
write CommerceContext into HttpContext.Items
assign StoreCommerceContext
Activity tags
logging scope
_next

No fail-open execution after presentation failure.

EDITION / TENANT RESOLUTION MATRIX

Verify:

Unset:
503 platform.edition.unconfigured

Marketplace:

missing MarketplaceConnectionReference => 503 platform.connection.unconfigured
success uses DeploymentStoreCommerce
Tenant = null
no host allowlist lookup

SingleStore:

Request.Host normalized with HostNormalizer
registry Hosts allowlist
only TenantStatus.Active accepted
unknown/inactive => 404 platform.resolution.failed
active tenant uses record connection/store-commerce values
FAIL-CLOSED / ANTI-ENUMERATION

Prove identical external behavior for:

unknown host
disabled tenant
suspended/non-active tenant

Required:
same 404
same machine code
no tenant status detail
no existence detail
no connection reference
no connection string

TENANT HEADER AUTHORITY

Verify no tenant-id header is used as resolution source of truth.

Host/domain registry remains authority for SingleStore.

SKIP PREFIXES

Exact current list:

/health
/ready
/__platform-error
/__platform-conflict

Verify:

StartsWithSegments
no business route bypass
no wildcard bypass
no tenant-sensitive route sharing these roots

Do not change in cert.

CANONICAL PRESENTATION

Verify:

IExceptionPresentationService only
SemanticException + Foundation codes
no local ProblemDetails
no PlatformExceptionMapper
no IProblemDetailsService
no hard-coded runtime title
no message heuristic
ERROR DESCRIPTOR / LOCALIZATION

Reverify exact canonical matrix:

platform.edition.unconfigured -> 503
platform.connection.unconfigured -> 503
platform.resolution.failed -> 404

All:

exactly one Foundation descriptor
EN resource present
FA resource present
HOSTNORMALIZER BOUNDARY

Verify HostNormalizer is neutral BuildingBlocks primitive and MultiTenancy use is valid.

No relocation.

CONTROLPLANE REGISTRY BOUNDARY

Verify ControlPlaneRegistry is Host Configuration platform state only.

MultiTenancy may consume it but must not own option building/validation.

No relocation/certification of Configuration in this task.

DATABASE CONNECTION RESOLVER BOUNDARY

Verify IDatabaseConnectionResolver use is only:

resolve configured reference
ensure available connection

No raw connection string logging/presentation.
No persistence/business DB usage inside MultiTenancy.

No Persistence folder certification in this task.

STORECONTEXT BOUNDARY

Verify:

MultiTenancy only consumes StoreContext.Contracts
no StoreContext.Application/Infrastructure/Domain
no duplicate StoreCommerce rules
Host only assigns resolved StoreCommerce context
StoreContext remains neutral owner of store-commerce contracts/accessor
HARD-CODED RUNTIME USER-FACING TEXT

Across both files:
ZERO.

Allowed:

comments
machine codes
header/path literals
log templates
EXCEPTION MESSAGE CLASSIFICATION

ZERO:

ex.Message
exception.Message
Message.Contains
StartsWith
Equals for mapping
SENSITIVE DATA

Verify no output/logging of:

connection strings
connection references
auth headers/tokens
raw secrets
tenant status reason

TenantId/DeploymentId in structured observability is allowed if consistent with current platform policy.

OBSERVABILITY

Verify:

uses Activity.Current only
no custom ActivitySource
no custom Meter
no custom correlation ID
current tags exactly platform operational context
structured scope only
no sensitive logging
LOGGING DUPLICATION

Reassess current middleware warning plus canonical presentation logging.

If dual logging is intentional and non-sensitive, document as accepted operational + presentation signals.

Do not redesign in cert.

CONTRACT / MICROSERVICE BOUNDARY

Across both files ZERO:

foreign Application
foreign Infrastructure
foreign Domain
foreign DbContext
direct persistence repository
cross-module join
business commands
business workflow authority

Allowed:

ASP.NET
BuildingBlocks neutral abstractions
StoreContext.Contracts
Host platform Configuration dependencies
COHESION

Verify:

accessor and middleware are physically separated
one type per file
responsibilities distinct
no god file
no further split required
ACTIVE CONSUMER / DEAD CODE

Verify:

accessor registered and consumed
middleware registered via UseMiddleware
no dead type
no duplicate equivalent Host implementation
no old namespace consumers
ERRORS CERT COMPATIBILITY

HostErrorsAmcCertGuardTests must still pass.

Its historical Errors implementation assertion may refer to historical block, not current top-level pointer.

Do not weaken:

HOST_ERRORS_AMC_CERTIFIED
mapper absence
canonical presentation guarantees
DURABLE CERTIFICATION GUARD

Create:
HostMultiTenancyAmcCertGuardTests

Lock at minimum:

exact 2-file tree
exact namespaces
exact two types
one type per file
scoped accessor DI identity
InvokeAsync scoped assigner injection
RequestServices ZERO
service locator ZERO
exact skip prefixes
exact fail-closed 503/503/404
same 404/code for unknown/inactive
canonical presentation
no hard-coded runtime titles
no message classification
foreign layers ZERO
StoreContext Contracts-only
ControlPlane/connection resolver remain adjacent platform deps
Errors/Security/Admin cert states preserved in SoT

Do not weaken W1 guard.

PRODUCTION CHANGE RULE

Expected:
Production-Code-Change-State = ZERO
Production-Repair-Required-State = NONE

Cert may modify only:

tests/guards
docs/evidence
SoT/current certification metadata

If production defect is found:
STOP INCOMPLETE and name exact repair.

PROTECTED STATE

Must preserve:
HOST_ERRORS_AMC_CERTIFIED
HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED

No production changes under those folders.

HOST-ONLY LOCK

User explicitly requested:
FINISH HOST ONLY.

Do not open:

Catalog
Fulfillment
Checkout
any module recovery

No module production edits.

FOCUSED VALIDATION

Build:

Tooba.Host
Tooba.Host.Tests

Run:

HostMultiTenancyAmcW1GuardTests
HostMultiTenancyAmcCertGuardTests
TenantResolutionTests
TenantResolutionPlatformErrorTests
HostErrorsAmcW1GuardTests
HostErrorsAmcCertGuardTests
focused accessor DI identity test
TmarDurableGuard current Recovery assertions

No solution-wide tests.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-MULTITENANCY-AMC-001-W2-CERT/

Required:

certification-summary.md
physical-tree.md
accessor-di.md
middleware-lifetime.md
resolution-security.md
boundary-contracts.md
observability-sensitive-data.md
validation.md
recovery.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-MULTITENANCY-AMC-001-W2-CERT.task.md

RECOVERY / SOT

On PASS:

Top-level:

lastAcceptedTask = TB-TMAR-HOST-MULTITENANCY-AMC-001-W2-CERT
lastAcceptedCommit remains W1 implementation:
cfbc94d258de837fdc018ddb29db68233cc25783
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-MULTITENANCY-AMC-001-W1
currentHostCheckpoint = MultiTenancy
nextTask = USER_REVIEW_HOST_MULTITENANCY_AMC_001_W2_CERT
workflowStop = USER_REVIEW_HOST_MULTITENANCY_AMC_001_W2_CERT
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Add/update:
hostMultiTenancyAmc001W2Cert

Required:

certificationState = HOST_MULTITENANCY_AMC_CERTIFIED
boundaryState = HOST_MULTITENANCY_PLATFORM_BOUNDARY_CERTIFIED
productionFileCount = 2
productionTypeCount = 2
accessorState = THIN_HOST_CONTEXT_ADAPTER_CERTIFIED
middlewareState = GLOBAL_HOST_MULTITENANCY_PLATFORM_CERTIFIED
pathNamespace = EXACT_Tooba.Host.MultiTenancy
serviceLocator = ZERO
requestServices = ZERO
scopedStoreCommerceAssigner = INVOKEASYNC_PARAMETER_CERTIFIED
failClosedAntiEnumeration = CERTIFIED
skipPrefixes = CERTIFIED_EXACT
canonicalPresentation = CERTIFIED
hardcodedRuntimeText = ZERO
exceptionMessageClassification = ZERO
foreignApplication = ZERO
foreignInfrastructure = ZERO
foreignDomain = ZERO
foreignDbContext = ZERO
businessAuthority = ZERO
productionRepairRequired = false
implementationCommit = cfbc94d2...
hostErrorsCertification = HOST_ERRORS_AMC_CERTIFIED_PRESERVED
hostSecurityCertification = HOST_SECURITY_AMC_CERTIFIED_PRESERVED
hostAdminCertification = HOST_ADMIN_FULLY_CERTIFIED_PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_MULTITENANCY_AMC_001_W2_CERT

Certification/docs stamp recorded separately.

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

exact tree = 2 files
exact types = 2
exact namespace
accessor scoped DI identity proven
no cross-request leakage
RequestServices ZERO
service locator ZERO
InvokeAsync scoped assigner certified
context assignment order preserved
tenant resolution matrix exact
anti-enumeration exact
tenant header not authority
skip prefixes exact
canonical error presentation exact
platform error descriptor/localization exact
StoreContext Contracts-only
ControlPlane/DB resolver bounded platform dependencies
hard-coded runtime text ZERO
message classification ZERO
sensitive leakage ZERO
custom observability ZERO
foreign module layers ZERO
business authority ZERO
both types active
Errors/Security/Admin certs preserved
production repair NONE
production change ZERO
focused builds/tests PASS
durable cert guard PASS
certification = HOST_MULTITENANCY_AMC_CERTIFIED
implementation SHA remains cfbc94d2...
automatic next NONE
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-MULTITENANCY-AMC-001-W2-CERT
Parent-Task: TB-TMAR-HOST-MULTITENANCY-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Certification-State:
Boundary-Certification-State:
Production-File-Count:
Production-Type-Count:
Exact-Tree-State:
Path-Namespace-State:
Accessor-State:
Accessor-DI-Identity-State:
Accessor-CrossRequest-State:
Middleware-State:
Middleware-DI-State:
RequestServices-State:
ServiceLocator-State:
Scoped-StoreCommerce-Assigner-State:
Context-Assignment-Parity-State:
Tenant-Resolution-Matrix-State:
FailClosed-AntiEnumeration-State:
Tenant-Header-Authority-State:
Skip-Prefix-State:
Canonical-Presentation-State:
Platform-Error-Matrix-State:
Localization-Resolution-State:
HostNormalizer-Boundary-State:
ControlPlaneRegistry-Boundary-State:
DatabaseConnectionResolver-Boundary-State:
StoreContext-Boundary-State:
Hardcoded-Runtime-User-Facing-Text-State:
Exception-Message-Classification-State:
Sensitive-Data-State:
Observability-State:
Logging-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
Cohesion-State:
Active-Consumer-State:
Durable-Cert-Guard-State:
Host-Errors-Certification-State:
Host-Security-Certification-State:
Host-Admin-Certification-State:
Production-Repair-Required-State:
Production-Code-Change-State:
Focused-Build-State:
Focused-Test-State:
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

Do not start another Host folder.
Do not open module recovery.
Do not modify production code in Cert.

Wait for Architect/user review.

END_TOOBA_TASK