PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-OBSERVABILITY-AMC-001-W2-CERT
Parent-Task: TB-TMAR-HOST-OBSERVABILITY-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_OBSERVABILITY_CERTIFY
Title: Certify Host/Observability as privacy-safe request enrichment platform boundary
Estimated-Time-Minutes: 8
Hard-Timebox-Minutes: 12

ARCHITECT REVIEW STATE

W1:
ACCEPTED FOR CERTIFICATION ENTRY

Architect independently verified current main:

Implementation commit:
f1425fed94cc1a8354d3c9f9a013065d87cbe66c

Current Observability production truth:

exact 1 file / 1 type
namespace exact Tooba.Host.Observability
Program imports Tooba.Host.Observability
middleware registration remains exactly once after SessionAuthenticationMiddleware
ClientIp population removed
RemoteIpAddress read ZERO
QueryString logging ZERO
HttpPath remains Path.Value only
correlation provider/items/ensure fallback preserved
tenant/store/actor/request fields preserved
canonical ObservabilityLogScope preserved
custom tracing ZERO
custom metrics ZERO
no catch/swallow
foreign App/Infra/Domain/DbContext ZERO
business authority ZERO
Messaging/Health/MultiTenancy/Errors/Security/Admin certifications preserved

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-certify/SKILL.md

CERTIFY ONLY.

No production migration.
No production repair unless a material defect is found, in which case:
Status = INCOMPLETE
Production-Repair-Required-State = YES
STOP.

Do NOT start Outbox/Persistence/Configuration.
Do NOT start another Host folder.
Do NOT open module recovery.

PRIMARY CERTIFICATION SCOPE

src/backend/Host/Tooba.Host/Observability/

Expected exact tree:

Observability/

RequestObservabilityEnrichmentMiddleware.cs

Expected:

production files = 1
production top-level types = 1
namespace = Tooba.Host.Observability

Certification labels on PASS:

HOST_OBSERVABILITY_AMC_CERTIFIED
HOST_OBSERVABILITY_PLATFORM_BOUNDARY_CERTIFIED

MANDATORY CERTIFICATION AUDITS

EXACT TREE

Verify:

exact one production .cs file
exact expected filename
no subfolder
exact one top-level production type
no duplicate legacy copy
no alias/shim/TypeForwardedTo
PATH ↔ NAMESPACE

Verify exact:
namespace Tooba.Host.Observability

OWNERSHIP

Certify:
RequestObservabilityEnrichmentMiddleware
= GLOBAL_HOST_OBSERVABILITY_PLATFORM_CERTIFIED

It remains Host-owned because it enriches request logs after Host tenant/session context exists.

Do not move to BuildingBlocks.

PROGRAM REGISTRATION

Verify:

using Tooba.Host.Observability
middleware mapped exactly once
no duplicate equivalent middleware
no old namespace reference
PIPELINE ORDER

Certify exact relevant order:

forwarded headers conditional
correlation middleware
exception handler
CORS
SecurityHeadersMiddleware
TenantResolutionMiddleware
SessionAuthenticationMiddleware
RequestObservabilityEnrichmentMiddleware
endpoint mapping

Observability enrichment must remain after Tenant + Session.

CORRELATION AUTHORITY

Certify:

ICorrelationIdProvider is primary authority
HttpContext.Items fallback remains defensive
EnsureCorrelationId is last-resort
no new correlation implementation
no competing CorrelationId creation policy
COMMERCE CONTEXT

Verify:

only ICurrentCommerceContext consumed
no mutation
tenantId read only
Marketplace tenant null => no TenantId in scope
STORE ID SEMANTICS

Certify current logging-only behavior:
storeId = tenantId

Conditions:

this is only a log dimension
no business authority
Marketplace null tenant => storeId absent
not interpreted as seller/store domain identity
ACTOR ID

Verify:

only CurrentAuthenticatedSession.UserId
Guid non-empty only
format = N
no username/email/phone/claims/token/cookie/session secret
CLIENT IP PRIVACY

Certify exact W1 target:

RemoteIpAddress read ZERO
X-Forwarded-For read ZERO
ClientIp argument not populated
ObservabilityLogScope ClientIp key absent from this middleware’s scope output
no hash/mask replacement introduced

Classification:
CLIENT_IP_OMITTED_PRIVACY_SAFE_CERTIFIED

HTTP PATH

Verify:

path source = context.Request.Path.Value
QueryString not logged
RawTarget not logged
headers not logged
full URL not logged

Classification:
PATH_ONLY_NO_QUERY_CERTIFIED

REQUEST ID

Verify:

context.TraceIdentifier preserved
independent from correlation ID
safe operational identifier
CANONICAL LOG SCOPE

Verify only:
ObservabilityLogScope.CreateState
ObservabilityLogScope.Begin

No parallel structured logging schema.

LOGGER BEHAVIOR

Verify:

ILogger<RequestObservabilityEnrichmentMiddleware>
no explicit information/warning/error log line added
logger used only for BeginScope
no duplicate request completion logging
CUSTOM TRACING / METRICS

Required ZERO:

ActivitySource
Activity creation
Meter
custom traceparent
custom tracestate
custom span enrichment in Host/Observability file

This middleware enriches logs only.

EXCEPTION PROPAGATION

Verify:

no catch
no swallow
_next exception propagates
scope disposes
HARD-CODED USER-FACING TEXT

Required ZERO.

Comments/XML docs are not runtime presentation.

SENSITIVE DATA

Verify scope cannot include from this middleware:

raw IP
query string
Authorization
cookies
password
OTP
connection strings
payment payload
request body
headers
token/session secret

Allowed operational identifiers:

correlationId
requestId
tenantId
storeId logging mirror
actor Guid
method
path only
AUTHENTICATION / TENANCY BOUNDARY

Verify middleware only consumes:

CurrentAuthenticatedSession
ICurrentCommerceContext

Required ZERO authority to:

authenticate
authorize
resolve tenant
mutate commerce/session state
FOREIGN LAYERS

Required ZERO:

foreign Application
foreign Infrastructure
foreign Domain
DbContext
repository
persistence query
business command
domain decision
ACTIVE CONSUMER

Verify Program is the single production consumer.
No dead code.

W1 GUARD

Do NOT weaken:
HostObservabilityAmcW1GuardTests

DURABLE CERT GUARD

Create:
HostObservabilityAmcCertGuardTests

Lock at minimum:

exact one-file/one-type tree
exact namespace
exact Program registration count
order after SessionAuthenticationMiddleware
correlation provider/fallback
tenant/store/actor/request fields
raw ClientIp absent
QueryString absent
Path.Value retained
canonical ObservabilityLogScope only
custom tracing/metrics zero
no catch
foreign App/Infra/Domain/DbContext zero
protected cert states preserved in SoT
cert labels present
implementation SHA remains W1 SHA
FOCUSED TESTS

Run:

HostObservabilityAmcW1GuardTests
HostObservabilityAmcCertGuardTests
RequestObservabilityEnrichmentMiddlewareTests
TmarDurableGuardTests
any existing observability foundation guard required for regression confidence
HostMessagingAmcCertGuardTests only if Program/recovery coupling requires

No solution-wide tests.

BUILD

Focused:

Tooba.Host
Tooba.Host.Tests

No solution-wide build.

PROTECTED CERTIFICATIONS

Must preserve:

HOST_MESSAGING_AMC_CERTIFIED
HOST_HEALTH_AMC_CERTIFIED
HOST_MULTITENANCY_AMC_CERTIFIED
HOST_ERRORS_AMC_CERTIFIED
HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED

No production edits under prior certified folders.

PRODUCTION CHANGE RULE

Expected:
Production-Code-Change-State = ZERO
Production-Repair-Required-State = NONE

Cert may modify only:

tests/guards
docs/evidence
Recovery/SoT metadata

If production defect found:
STOP INCOMPLETE.

RECOVERY HYGIENE

Verify:

no stale historical current pointer
no duplicate JSON properties
no automatic next Host folder
current checkpoint remains Observability
implementation SHA remains W1
certification/docs stamp separate
EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-OBSERVABILITY-AMC-001-W2-CERT/

Required:

certification-summary.md
physical-tree.md
middleware-order.md
privacy-scope.md
boundary-contracts.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-OBSERVABILITY-AMC-001-W2-CERT.task.md

RECOVERY / SOT

On PASS:

Top-level:

lastAcceptedTask = TB-TMAR-HOST-OBSERVABILITY-AMC-001-W2-CERT
lastAcceptedCommit remains:
f1425fed94cc1a8354d3c9f9a013065d87cbe66c
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-OBSERVABILITY-AMC-001-W1
currentHostCheckpoint = Observability
nextTask = USER_REVIEW_HOST_OBSERVABILITY_AMC_001_W2_CERT
workflowStop = USER_REVIEW_HOST_OBSERVABILITY_AMC_001_W2_CERT
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Add/update:
hostObservabilityAmc001W2Cert

Required:

certificationState = HOST_OBSERVABILITY_AMC_CERTIFIED
boundaryState = HOST_OBSERVABILITY_PLATFORM_BOUNDARY_CERTIFIED
productionFileCount = 1
productionTypeCount = 1
pathNamespace = EXACT_Tooba.Host.Observability
middlewareDisposition = GLOBAL_HOST_OBSERVABILITY_PLATFORM_CERTIFIED
middlewareOrder = AFTER_TENANT_AND_SESSION_CERTIFIED
correlationAuthority = CANONICAL_CERTIFIED
clientIpLogging = OMITTED_PRIVACY_SAFE_CERTIFIED
httpPathLogging = PATH_ONLY_NO_QUERY_CERTIFIED
storeIdSemantics = TENANTID_LOGGING_MIRROR_CERTIFIED
actorIdSemantics = GUID_N_CERTIFIED
requestIdSemantics = TRACEIDENTIFIER_CERTIFIED
observabilityLogScope = CANONICAL_CERTIFIED
customTracing = ZERO
customMetrics = ZERO
sensitiveData = ZERO
foreignApplication = ZERO
foreignInfrastructure = ZERO
foreignDomain = ZERO
foreignDbContext = ZERO
businessAuthority = ZERO
productionRepairRequired = false
productionCodeChange = ZERO
implementationCommit = f1425fed...
hostMessagingCertification = HOST_MESSAGING_AMC_CERTIFIED_PRESERVED
hostHealthCertification = HOST_HEALTH_AMC_CERTIFIED_PRESERVED
hostMultiTenancyCertification = HOST_MULTITENANCY_AMC_CERTIFIED_PRESERVED
hostErrorsCertification = HOST_ERRORS_AMC_CERTIFIED_PRESERVED
hostSecurityCertification = HOST_SECURITY_AMC_CERTIFIED_PRESERVED
hostAdminCertification = HOST_ADMIN_FULLY_CERTIFIED_PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_OBSERVABILITY_AMC_001_W2_CERT

Certification/docs stamp separate from implementation SHA.

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

exact 1 file / 1 type
exact namespace
single Program registration
pipeline order certified
correlation authority certified
tenant/store/actor/request semantics certified
ClientIp omitted
QueryString zero
Path.Value only
canonical log scope only
custom tracing/metrics zero
exception propagation preserved
hard-coded user-facing text zero
sensitive leakage zero
foreign App/Infra/Domain/DbContext zero
business authority zero
durable cert guard PASS
prior certs preserved
production repair NONE
production change ZERO
focused builds/tests PASS
recovery hygiene exact
implementation SHA remains f1425fed...
automatic next NONE
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-OBSERVABILITY-AMC-001-W2-CERT
Parent-Task: TB-TMAR-HOST-OBSERVABILITY-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Certification-State:
Boundary-Certification-State:
Observability-Production-File-Count:
Observability-Production-Type-Count:
Exact-Tree-State:
Path-Namespace-State:
RequestObservabilityEnrichmentMiddleware-State:
Program-Registration-State:
Middleware-Order-State:
Correlation-Authority-State:
Correlation-Fallback-State:
CommerceContext-Boundary-State:
StoreId-Semantics-State:
ActorId-State:
ClientIp-Logging-State:
HttpPath-Logging-State:
QueryString-Logging-State:
RequestId-State:
ObservabilityLogScope-State:
Custom-Tracing-State:
Custom-Metrics-State:
Logger-State:
Exception-Propagation-State:
Hardcoded-User-Facing-Text-State:
Sensitive-Data-State:
Authentication-Boundary-State:
Tenancy-Boundary-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
Active-Consumer-State:
Durable-Cert-Guard-State:
Host-Messaging-Certification-State:
Host-Health-Certification-State:
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
Do not start Outbox/Persistence/Configuration AMC.
Do not modify production code in Cert.
Do not open module recovery.

Wait for Architect/user review.

END_TOOBA_TASK