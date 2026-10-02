PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-HEALTH-AMC-001-W2-CERT
Parent-Task: TB-TMAR-HOST-HEALTH-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_HEALTH_CERTIFY
Title: Independently certify Host/Health as a safe global liveness/readiness platform boundary
Estimated-Time-Minutes: 11
Hard-Timebox-Minutes: 15

ARCHITECT REVIEW STATE

W1:
ACCEPTED FOR CERTIFICATION ENTRY

Architect independently verified current main:

Implementation commit:
ba8db8c6bcb22f0ad4c386073d3b612e3d318e00

W1 docs/SoT stamp:
17a866d56ccd139b1e912371fb8d07c9159f9019

Current pushed tip:
73b3a45f4ff9e6693c79ccea87c92439afa35149

Current production truth:

exact Health files = 2
exact Health types = 2 (+ nested Evaluation record)
namespace exact = Tooba.Host.Health
IServiceProvider in Health = ZERO
RequestServices in Health = ZERO
service locator for IBusControl = ZERO
explicit IEnumerable<IBusControl> DI
exact four health routes preserved
connection-reference disclosure removed
messaging-schema disclosure removed
raw Results.Json intentionally retained
configured-not-connectivity readiness semantics preserved
all configured tenant connection-reference policy preserved
AccessControl dependency remains Contracts-only
Host/MultiTenancy certification preserved
Host/Errors certification preserved
Host/Security certification preserved
Host/Admin certification preserved

ARCHITECT RECOVERY NOTE

Repository recovery documentation was manually cleaned by Architect after Health Analyze to remove stale historical "current" checkpoint markers.

Current authoritative resume rule is:

authoritative current checkpoint section
docs/architecture/tmar-current-state.json

Do NOT reintroduce:

USER_REVIEW_AFTER_RECOVERY_SOT_SYNC_001 as current
"no Host folder is active" as current
stale "Current Issued Task" / "Last Implementation Task" labels

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-certify/SKILL.md

CERTIFY ONLY.

No production migration.
No production repair.
No Messaging AMC.
No Configuration AMC.
No Persistence AMC.
No next Host folder.
No module recovery.

If a material production defect is found:
Status = INCOMPLETE
Production-Repair-Required-State = YES
STOP.

PRIMARY CERTIFICATION SCOPE

src/backend/Host/Tooba.Host/Health/

Expected exact tree:

Health/

HostHealthEndpoints.cs
HostReadinessEvaluator.cs

Expected namespace:
Tooba.Host.Health

Expected ownership:

HostHealthEndpoints = KEEP_AS_GLOBAL_HOST_HEALTH_PLATFORM
HostReadinessEvaluator = KEEP_AS_GLOBAL_HOST_HEALTH_PLATFORM

Certification label on PASS:
HOST_HEALTH_AMC_CERTIFIED

Boundary label:
HOST_HEALTH_PLATFORM_BOUNDARY_CERTIFIED

MANDATORY CERTIFICATION AUDITS

EXACT TREE

Re-enumerate from disk.

Verify:

exact production .cs count = 2
exact filenames
exact top-level production type count = 2
nested Evaluation record allowed
no unexpected file/subfolder
no duplicate old namespace copy
no alias/shim/TypeForwardedTo
PATH ↔ NAMESPACE

Both files:
namespace Tooba.Host.Health

State must be EXACT.

ROUTE INVENTORY

Verify exactly:

GET /health/live
GET /health
GET /health/ready
GET /ready

Verify:

no duplicate route elsewhere
HostHealthEndpoints.Map registered once in Program
liveness aliases keep CORS behavior
readiness aliases share same evaluator
health/ready routes remain tenant-resolution bypasses by established MultiTenancy skip rules
session/auth bypass remains intentional operational platform behavior
LIVENESS CERTIFICATION

/health/live and /health must:

return 200
return status=ok
not open DB
not evaluate messaging
not evaluate authorization readiness
not resolve tenant
not expose configuration
READINESS CHECK ORDER

Certify exact order:

edition configured
configured PostgreSQL reference presence
authorization readiness
messaging readiness

No hidden business check.

READINESS TRUTH

Explicit certification wording:
CONFIGURED_NOT_CONNECTIVITY

Verify:

no DbContext open
no SQL query
no TCP/database connectivity probe
PostgreSQL check validates configured references only

Do not mislabel as database connectivity health.

TENANT REFERENCE POLICY

Verify current W1-preserved semantics:
ALL_CONFIGURED_PRESERVED

For SingleStore:
all configured tenant connection references are included.

Do NOT change to Active-only during Cert.

Record that this is deployment-configuration readiness, not request-routing readiness.

ACCESSCONTROL READINESS BOUNDARY

Verify:
IAuthorizationReadinessProbe comes only from AccessControl.Contracts.

Required ZERO:

AccessControl.Application
AccessControl.Infrastructure
AccessControl.Domain

Health owns no authorization business logic.

EXPLICIT MASS TRANSIT DI

Verify readiness endpoint receives:
IEnumerable<IBusControl>
or current equivalent explicit DI collection.

Required ZERO:

IServiceProvider
RequestServices
GetService<IBusControl>
GetRequiredService<IBusControl>
injected generic IServiceProvider
BUS CARDINALITY

When messaging enabled:

zero bus => not-ready / bus-unavailable
exactly one => CheckHealth
multiple buses => deterministic not-ready / bus-unavailable
no arbitrary First()/selection

When messaging disabled:

no bus registration required
readiness may still succeed
MESSAGING HEALTH SEMANTICS

Verify:

Unhealthy => not-ready / messaging=unhealthy
non-Unhealthy status preserves existing safe status string
messaging-transport=postgresql-sql on enabled successful path
messaging=disabled + transport=n/a when disabled

No publish/consume/business messaging logic in Health.

PUBLIC DISCLOSURE SAFETY

Public readiness JSON must expose ZERO:

actual ConnectionReference values
connection strings
messaging schema name
credentials
tokens
secrets
tenant status reason
exception.Message
stack trace
SQL/file paths

Required:
missing connection check value exactly generic:
missing-reference

Required absent:
messaging-schema

RESPONSE CONTRACT

Normal operational readiness:

raw Results.Json is intentional and certified
ready => 200, status=ready
not-ready => 503, status=not-ready
checks object retained

Do NOT require ApiResponseFactory for health protocol.

MACHINE STATUS TEXT

Operational machine values are allowed and not localized:

ok
ready
not-ready
configured
unconfigured
missing-reference
disabled
bus-unavailable
unhealthy
postgresql-sql
n/a
MassTransit health enum status strings

Hard-coded USER-FACING runtime prose:
ZERO.

UNKNOWN EXCEPTION POLICY

Verify no broad catch was introduced.

Unknown failures from:

authorization probe
bus health
unexpected runtime/configuration faults

must continue to canonical global exception handling.

No exception.Message classification.

OBSERVABILITY

Verify Health production has ZERO custom:

ActivitySource
Meter
correlation ID
logging scope
custom trace propagation

No sensitive logs.

CONFIGURATION OWNERSHIP

Health only reads:

ControlPlaneRegistry
ToobaPlatformOptions
MessagingHostOptions

Verify:

no mutation
no options authority
no duplicate validator implementation
no Configuration folder ownership claim
MASS TRANSIT BOUNDARY

Direct MassTransit dependency is allowed here ONLY for Host transport readiness:

IBusControl
CheckHealth
BusHealthStatus

Required ZERO:

message publishing
consumers
retry policy ownership
SQL transport registration
Npgsql direct dependency
CONTRACT / MICROSERVICE BOUNDARY

Across Health files required ZERO:

foreign Application
foreign Infrastructure
foreign Domain
foreign DbContext
repository
cross-module persistence
business commands
business workflows
module state mutation

Allowed:

AccessControl.Contracts readiness seam
Host configuration models
Host messaging options
MassTransit transport health
ASP.NET platform primitives
COHESION

Verify:

HostHealthEndpoints only route/presentation composition
HostReadinessEvaluator only readiness evaluation
nested Evaluation record cohesive
no split/merge needed
no god file
ACTIVE CONSUMER / DEAD CODE

Verify:

HostHealthEndpoints.Map active once
HostReadinessEvaluator active from endpoints
no dead Health type
no duplicate evaluator elsewhere
MULTITENANCY BYPASS COMPATIBILITY

Reverify:
MultiTenancy SkipPrefixes still includes:
/health
/ready

No Health W1 change broke certified MultiTenancy behavior.

Do not modify MultiTenancy production code.

AUTHENTICATION BYPASS COMPATIBILITY

Reverify SessionAuthenticationMiddleware operational skip behavior for health/readiness.

Do not modify Authentication production code.

DURABLE CERTIFICATION GUARD

Create:
HostHealthAmcCertGuardTests

It must lock at minimum:

exact 2-file tree
exact namespace
exact 4 routes
Program maps health once
IServiceProvider ZERO
service locator ZERO
explicit bus collection DI
zero/one/multiple bus semantics
generic missing-reference only
no connection reference interpolation
messaging-schema absent
raw Results.Json retained
configured-not-connectivity semantics
all-configured tenant policy retained
AccessControl Contracts-only
hard-coded user-facing prose ZERO
message classification ZERO
foreign layers/DbContext ZERO
protected prior cert states preserved in SoT

Do NOT weaken HostHealthAmcW1GuardTests.

PROTECTED CERTIFICATIONS

Must preserve:

HOST_MULTITENANCY_AMC_CERTIFIED
HOST_ERRORS_AMC_CERTIFIED
HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED

No production changes under those folders.

RECOVERY HYGIENE

Verify current authoritative recovery remains coherent.

On PASS:

do not reintroduce stale historical "current checkpoint" labels
no duplicate JSON properties
no stale current pointer
no automatic next folder
no historical task line treated as current
PRODUCTION CHANGE RULE

Expected:
Production-Code-Change-State = ZERO
Production-Repair-Required-State = NONE

Cert may modify only:

tests/guards
docs/evidence
Recovery/SoT metadata

If production repair is needed:
STOP INCOMPLETE.

FOCUSED VALIDATION

Build:

Tooba.Host
Tooba.Host.Tests

Run focused:

HostHealthAmcW1GuardTests
HostHealthAmcCertGuardTests
HostReadinessEvaluatorW1Tests
HostHealthEndpointTests
HostReadinessBoundaryGuardTests
HostMultiTenancyAmcCertGuardTests
HostErrorsAmcCertGuardTests
TmarDurableGuard current pointer/recovery assertions

No solution-wide tests.

One deterministic cert/docs/guard correction + one rerun maximum.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-HEALTH-AMC-001-W2-CERT/

Required:

certification-summary.md
physical-tree-routes.md
readiness-contract.md
messaging-di.md
disclosure-security.md
boundary-contracts.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-HEALTH-AMC-001-W2-CERT.task.md

RECOVERY / SOT

On PASS:

Top-level:

lastAcceptedTask = TB-TMAR-HOST-HEALTH-AMC-001-W2-CERT
lastAcceptedCommit remains W1 implementation:
ba8db8c6bcb22f0ad4c386073d3b612e3d318e00
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-HEALTH-AMC-001-W1
currentHostCheckpoint = Health
nextTask = USER_REVIEW_HOST_HEALTH_AMC_001_W2_CERT
workflowStop = USER_REVIEW_HOST_HEALTH_AMC_001_W2_CERT
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Add/update:
hostHealthAmc001W2Cert

Required:

certificationState = HOST_HEALTH_AMC_CERTIFIED
boundaryState = HOST_HEALTH_PLATFORM_BOUNDARY_CERTIFIED
productionFileCount = 2
productionTypeCount = 2
routeCount = 4
pathNamespace = EXACT_Tooba.Host.Health
livenessState = PROCESS_ONLY_CERTIFIED
readinessTruth = CONFIGURED_NOT_CONNECTIVITY_CERTIFIED
tenantReferencePolicy = ALL_CONFIGURED_CERTIFIED
serviceLocator = ZERO
iServiceProvider = ZERO
messagingBusInjection = EXPLICIT_DI_COLLECTION_CERTIFIED
connectionReferenceDisclosure = ZERO
messagingSchemaDisclosure = ZERO
rawResultsJson = OPERATIONAL_PROTOCOL_CERTIFIED
hardcodedUserFacingText = ZERO
exceptionMessageClassification = ZERO
foreignApplication = ZERO
foreignInfrastructure = ZERO
foreignDomain = ZERO
foreignDbContext = ZERO
businessAuthority = ZERO
productionRepairRequired = false
implementationCommit = ba8db8c6...
hostMultiTenancyCertification = HOST_MULTITENANCY_AMC_CERTIFIED_PRESERVED
hostErrorsCertification = HOST_ERRORS_AMC_CERTIFIED_PRESERVED
hostSecurityCertification = HOST_SECURITY_AMC_CERTIFIED_PRESERVED
hostAdminCertification = HOST_ADMIN_FULLY_CERTIFIED_PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_HEALTH_AMC_001_W2_CERT

Certification/docs stamp recorded separately from implementation SHA.

GIT

Work from latest origin/main.

No reset.
No clean.
No rebase.
No force push.

Preserve unrelated user work.

Because Architect recovery-cleanup commits were integrated after W1:
do NOT overwrite or revert those documentation cleanups.

Push only on PASS.

SUCCESS CRITERIA

PASS only if:

exact 2-file Health tree
exact namespace
exact four routes
liveness process-only
readiness configured-not-connectivity
all-configured tenant policy exact
AccessControl Contracts-only
IServiceProvider ZERO
service locator ZERO
explicit bus collection DI
zero/multiple bus deterministic fail
messaging disabled without bus PASS
connection reference disclosure ZERO
messaging schema disclosure ZERO
raw Results.Json certified operational exception
machine labels accepted
hard-coded user-facing prose ZERO
message classification ZERO
unknown exception broad catch ZERO
sensitive detail leakage ZERO
custom observability ZERO
foreign App/Infra/Domain/DbContext ZERO
business authority ZERO
MultiTenancy/Errors/Security/Admin certs preserved
durable cert guard PASS
production repair NONE
production change ZERO
focused builds/tests PASS
Recovery current pointer exact
stale historical current markers not reintroduced
implementation SHA remains ba8db8c6...
automatic next NONE
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-HEALTH-AMC-001-W2-CERT
Parent-Task: TB-TMAR-HOST-HEALTH-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Certification-State:
Boundary-Certification-State:
Health-Production-File-Count:
Health-Production-Type-Count:
Route-Count:
Exact-Tree-State:
Path-Namespace-State:
Liveness-State:
Readiness-Truth-State:
Tenant-Reference-Policy-State:
AccessControl-Readiness-Boundary-State:
IServiceProvider-State:
ServiceLocator-State:
Messaging-Bus-DI-State:
Bus-Cardinality-State:
Messaging-Readiness-State:
ConnectionReference-Disclosure-State:
MessagingSchema-Disclosure-State:
Raw-ResultsJson-State:
Machine-Status-Text-State:
Hardcoded-User-Facing-Text-State:
Unknown-Exception-State:
Exception-Message-Classification-State:
Sensitive-Data-State:
Observability-State:
Configuration-Ownership-State:
MassTransit-Boundary-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
Cohesion-State:
Active-Consumer-State:
MultiTenancy-Bypass-Compatibility-State:
Authentication-Bypass-Compatibility-State:
Durable-Cert-Guard-State:
Host-MultiTenancy-Certification-State:
Host-Errors-Certification-State:
Host-Security-Certification-State:
Host-Admin-Certification-State:
Production-Repair-Required-State:
Production-Code-Change-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
Recovery-Hygiene-State:
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
Do not start Messaging AMC.
Do not modify production code in Cert.
Do not open module recovery.

Wait for Architect/user review.

END_TOOBA_TASK