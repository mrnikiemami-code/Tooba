PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-MULTITENANCY-AMC-001
Parent-Task: TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_MULTITENANCY_ANALYZE
Title: Analyze Host/MultiTenancy tenant resolution middleware and commerce-context accessor boundary
Estimated-Time-Minutes: 12
Hard-Timebox-Minutes: 16

ARCHITECT REVIEW STATE

Parent Errors certification/recovery repair:
ACCEPTED

Architect independently verified:

HOST_ERRORS_AMC_CERTIFIED preserved
Security cert docsStamp exact 73a80ee28ed9dc054be5adae0f7115e72c115ded
Errors cert docsStamp exact 8d5e6a2dce7b34e2ceeb4166e5d324467bc3a8f3
duplicate JSON property state = ZERO
latest accepted implementation remains:
e190e213c491fd530d86c7e5680cb0607b5e98d3
automaticNextImplementationTask = NONE
MultiTenancy has not yet been opened as its own recovery unit

NEXT ACTIVE RECOVERY UNIT

src/backend/Host/Tooba.Host/MultiTenancy/

Current repository snapshot shows exactly one production file:

TenantResolutionMiddleware.cs

Do NOT assume; re-enumerate from disk.

IMPORTANT CURRENT FILE FACTS TO VERIFY

TenantResolutionMiddleware.cs currently contains at least TWO production types:

HttpCommerceContextAccessor
TenantResolutionMiddleware

Current namespace appears to be:
Tooba.Host

while the physical path is:
Host/Tooba.Host/MultiTenancy/

This is a likely path↔namespace violation.

The file was partially touched by Host/Errors W1 only for canonical exception presentation:

IExceptionPresentationService
FoundationErrorCodes platform.*
SemanticException
That prior seam change does NOT certify MultiTenancy.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-analyze/SKILL.md

ANALYSIS ONLY.

Do NOT edit production code.
Do NOT migrate.
Do NOT certify.
Do NOT start another Host folder.
Do NOT reopen Errors/Security/Admin.

PROTECTED ACCEPTED STATE

Must remain untouched:

HOST_ERRORS_AMC_CERTIFIED
HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED
StoreContext foundation extraction accepted
frontend frozen
Checkout paused state unchanged

MANDATORY ANALYSIS

EXACT TREE / TYPE ENUMERATION

Enumerate:
src/backend/Host/Tooba.Host/MultiTenancy/

Record:

exact recursive production .cs count
exact filenames
all production types in each file
namespaces
public/internal visibility
nested/multiple-type cohesion

Expected current file count:
1

PER-TYPE DISPOSITION

Classify EACH production type separately:

KEEP_AS_GLOBAL_HOST_MULTITENANCY_PLATFORM
KEEP_AS_THIN_HOST_CONTEXT_ADAPTER
MOVE_TO_STORECONTEXT
MOVE_TO_BUILDINGBLOCKS
DEAD_ZERO_CONSUMER_RESIDUE
MUST_SPLIT
BLOCKED_NEEDS_ARCHITECT_DECISION

Do not classify the whole file first.

Explicitly disposition:

HttpCommerceContextAccessor
TenantResolutionMiddleware
CONSUMER / DI INVENTORY

For each type and implemented interface identify:

Program registration
middleware registration
DI lifetime
production consumers
test-only consumers
module consumers by neutral contracts

For HttpCommerceContextAccessor, explicitly trace:

ICurrentCommerceContext
ICurrentEdition
ICurrentTenant
ICommerceContextAssigner

Determine whether one concrete Host type implementing all four is still appropriate after StoreContext extraction.

OWNERSHIP TEST — STORECONTEXT VS HOST

StoreContext foundation already owns canonical contracts for:

Current commerce context
Store commerce context

Analyze whether Host should still own:

HttpContext-backed accessor implementation
request-scoped assignment bridge
tenant/edition resolution middleware

Expected distinction to test:

Host may own:

HTTP-specific context population
host/domain resolution
proxy/request-derived resolution

StoreContext may own:

neutral contracts
non-HTTP access abstractions

Do NOT move code in Analyze.

TENANT RESOLUTION AUTHORITY

Document exact resolution rules:

Edition:

Unset
Marketplace
SingleStore or other active editions

Host normalization:

HostNormalizer
forwarded-host assumptions

Tenant registry:

ControlPlaneRegistry
Hosts dictionary
TenantStatus.Active

Connection:

MarketplaceConnectionReference
tenant ConnectionReference
IDatabaseConnectionResolver

Store commerce:

DeploymentStoreCommerce
Tenant record StoreCommerce

Determine whether any business authority is embedded here or this is pure platform routing/context authority.

FAIL-CLOSED / SECURITY SEMANTICS

Verify exact behavior:

edition unset => 503 platform.edition.unconfigured
marketplace connection missing => 503 platform.connection.unconfigured
unknown host => 404 platform.resolution.failed
disabled tenant => same 404/code
suspended tenant => same 404/code
no tenant enumeration leak
no fail-open path
no untrusted tenant header as source of truth

Confirm whether any tenant-id header exists elsewhere and whether middleware ignores it.

SKIP-PREFIX AUDIT

Current SkipPrefixes includes:

/health
/ready
/__platform-error
/__platform-conflict

Analyze:

why each bypass exists
whether bypass is bounded/safe
whether auth/business endpoints can accidentally bypass tenant resolution
prefix matching edge cases
whether health/dev probes are the only valid exclusions

Do NOT alter prefixes yet.

REQUESTSERVICES / SERVICE LOCATOR DEBT

Current middleware uses:
httpContext.RequestServices.GetRequiredService<IStoreCommerceContextAssigner>()

Classify:

acceptable middleware request-scope bridge
service-locator violation requiring constructor/scoped redesign
blocked due middleware lifetime constraints

Architecture lock disfavors service locator.
Analyze exact DI/lifetime reason and recommend target pattern.

FILE COHESION / SPLIT DECISION

TenantResolutionMiddleware.cs currently includes:

HttpCommerceContextAccessor
TenantResolutionMiddleware

Determine whether canonical target should split into:

MultiTenancy/

HttpCommerceContextAccessor.cs
TenantResolutionMiddleware.cs

or whether current co-location is justified.

Apply exact path↔namespace lock.

PATH ↔ NAMESPACE

Current likely:
path = Host/MultiTenancy/
namespace = Tooba.Host

Classify exact structural debt.

Expected canonical namespace if retained in folder:
Tooba.Host.MultiTenancy

Audit all Program/tests/usings impact.

DEPENDENCY BOUNDARY

Audit all dependencies used by both types.

Classify each as:

Host platform
BuildingBlocks neutral
StoreContext.Contracts
foreign module contract
forbidden layer

Required ZERO:

module Application
module Infrastructure
module Domain
foreign DbContext
cross-module persistence
business repository
business command
DATABASE CONNECTION RESOLVER OWNERSHIP

Trace IDatabaseConnectionResolver:

definition owner
implementation owner
DI registration
connection-reference semantics
whether MultiTenancy only resolves platform connection context
whether secrets/connection strings can leak

Do NOT redesign persistence.

CONTROLPLANE REGISTRY OWNERSHIP

Trace:

ControlPlaneRegistry definition
who builds it
options/fail-fast source
edition/tenant record source
active tenant list

Determine whether this is:

Host platform configuration
Configuration folder responsibility
MultiTenancy responsibility
mixed responsibility needing seam cleanup later

No cross-folder move during Analyze.

CONTEXT ASSIGNMENT SEMANTICS

Trace both:

HttpContext.Items[HttpCommerceContextAccessor.ItemKey]
_assigned field
ICommerceContextAssigner.Assign

Analyze:

request-scoped lifetime assumptions
worker/non-HTTP support
double authority risk
stale context risk
thread/concurrency safety
whether Assign can override request truth
whether Current priority _assigned ?? HttpContext.Items is intentional

Document exact recommended invariant.

ACTIVITY / OBSERVABILITY

Audit:

tooba.edition
tooba.deployment
tooba.tenant_id
traceId source
logger BeginScope
canonical presentation logging from previous Errors wave

Determine:

duplicate warning + presentation warning behavior
sensitive identifiers
custom telemetry conflicts
whether tenant IDs in logs/tags are allowed by current policy

Do NOT change observability in Analyze.

SENSITIVE DATA

Audit for:

connection reference logging
connection string logging
tenant display name
host/domain
headers
secrets
raw exception detail

Required output:
exact sensitive-data classification.

HARD-CODED RUNTIME USER-FACING TEXT

Scan MultiTenancy production code.

After Errors W1 expected:
ZERO user-facing runtime prose on error path.

Verify no remaining hard-coded Persian/English presentation text.

EXCEPTION / MESSAGE CLASSIFICATION

Verify ZERO:

ex.Message classification
Message.Contains/StartsWith
InvalidOperationException message mapping
connection resolver message heuristics
HOSTNORMALIZER DEPENDENCY

Trace HostNormalizer:

file/folder owner
namespace
whether it is global Host Configuration/Transport primitive
whether MultiTenancy uses it legitimately
path/namespace correctness

Do not move it in this task.

TEST / GUARD INVENTORY

Identify focused tests/guards for:

tenant resolution
fail-closed behavior
marketplace behavior
edition unset
connection missing
context assignment
HostNormalizer
StoreContext fail-fast
MultiTenancy structure/path

Classify stale assertions after Errors W1.

HISTORICAL SOT RECONCILIATION

Inspect prior claims touching MultiTenancy:

StoreContext extraction
Cart StoreCommerce fail-fast
Errors W1 presentation seam
Authentication/Host platform tenant assumptions
any historical tenant middleware certification claims

Classify:

CURRENT
SUPERSEDED
STALE_METADATA
NOT_CERTIFIED

Do not infer certification from earlier seam work.

DECISIVE TARGET SHAPE

Produce the fewest safe waves.

Likely possibilities:

Option A — one migration wave + cert:
W1:

split file if required
namespace exact Tooba.Host.MultiTenancy
remove RequestServices service locator via constructor/scoped seam if safely possible
preserve resolution behavior
guards/tests
W2-CERT

Option B — two migration waves + cert if context accessor and middleware need independent work.

Do NOT force extra waves.

Every recommended wave:

exact files
exact behavior preserved
exact ownership result
time estimate <=20
focused tests
protected state
HOST-ONLY SCOPE LOCK

User explicitly requested:
FINISH HOST ONLY.

Therefore:

do not open module recovery
do not start Catalog/Fulfillment/Checkout work
module files may be inspected only to understand neutral contracts
no module production migration in this Analyze
PRODUCTION CHANGE RULE

Production change:
ZERO

Docs/evidence/SoT only.

FOCUSED ANALYSIS VALIDATION

No solution-wide tests.
No production build unless needed to resolve DI/lifetime ambiguity.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-MULTITENANCY-AMC-001/

Required:

analyze.md
type-dispositions.md
consumers-di.md
ownership-boundary.md
resolution-semantics.md
context-assignment.md
security-observability.md
path-namespace.md
guard-impact.md
historical-claims.md
migration-plan.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-MULTITENANCY-AMC-001.task.md

RECOVERY / SOT

Analysis-only.

Do NOT advance implementation SHA.

Record:

currentHostCheckpoint = MultiTenancy
mode = ANALYSIS_ONLY_NOT_MIGRATED_NOT_CERTIFIED
productionFileCount = actual
productionTypeCount = actual
dispositions
pathNamespaceState
serviceLocatorState
recommendedWaveCount
recommendedNextTask
Host/Errors certification = PRESERVED
Host/Security certification = PRESERVED
Host/Admin certification = PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_MULTITENANCY_AMC_001
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Latest accepted implementation remains:
e190e213c491fd530d86c7e5680cb0607b5e98d3

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

exact MultiTenancy tree enumerated
both current production types dispositioned separately
all interfaces/DI/consumers mapped
Host vs StoreContext ownership decided
tenant resolution authority documented
fail-closed/anti-enumeration proven
skip-prefix behavior audited
RequestServices service-locator debt classified
file split/cohesion decision made
path↔namespace debt classified
connection resolver ownership mapped
ControlPlaneRegistry ownership mapped
context assignment invariants documented
observability/sensitive logging audited
hard-coded runtime text state proven
message classification state proven
focused tests/guards mapped
historical claims reconciled
fewest safe migration waves proposed
Errors/Security/Admin certs preserved
production change ZERO
implementation SHA unchanged
automatic next NONE
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-MULTITENANCY-AMC-001
Parent-Task: TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Mode-State:
Active-Host-Folder:
Production-File-Count:
Production-Type-Count:
File-Enumeration-State:
HttpCommerceContextAccessor-State:
TenantResolutionMiddleware-State:
Type-Disposition-State:
Production-Consumer-Audit-State:
DI-Lifetime-State:
Host-vs-StoreContext-Ownership-State:
Tenant-Resolution-Authority-State:
FailClosed-AntiEnumeration-State:
Skip-Prefix-State:
RequestServices-ServiceLocator-State:
File-Cohesion-State:
Path-Namespace-State:
DatabaseConnectionResolver-State:
ControlPlaneRegistry-State:
Context-Assignment-State:
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
Guard-Impact-State:
Historical-Claims-State:
Recommended-Wave-Count:
Recommended-Next-Task:
Production-Code-Change-State:
Host-Errors-Certification-State:
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

Do not start MultiTenancy W1.
Do not start another Host folder.
Do not reopen Errors/Security/Admin.

Wait for Architect/user review.

END_TOOBA_TASK