PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-OBSERVABILITY-AMC-001-W1
Parent-Task: TB-TMAR-HOST-OBSERVABILITY-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_OBSERVABILITY_NAMESPACE_PRIVACY_HYGIENE
Title: Align Host/Observability namespace and remove raw ClientIp from canonical request log scope
Estimated-Time-Minutes: 9
Hard-Timebox-Minutes: 13

ARCHITECT REVIEW STATE

Parent Analyze:
ACCEPTED

Architect independently verified:

Host/Observability exact production tree = 1 file / 1 type
RequestObservabilityEnrichmentMiddleware is a legitimate global Host observability platform boundary
current namespace Tooba.Host violates path-derived namespace
middleware order after TenantResolution + SessionAuthentication is intentional
correlation authority is canonical ICorrelationIdProvider / BuildingBlocks correlation middleware
storeId=tenantId is logging-only and naturally null in Marketplace when tenant is null
actorId is Guid-only and contains no username/email/phone/token
raw HttpPath excludes query string and is permitted by the canonical observability foundation
raw ClientIp is optional in ObservabilityLogScope and the key contract explicitly says only if trusted-proxy safe
current middleware always passes RemoteIpAddress, so ClientIp is the only material sensitive-logging debt
there is no robust Host-local proof at this point that every emitted RemoteIpAddress satisfies the key's trusted-proxy-safe condition
safest bounded W1 is to OMIT ClientIp from this enrichment scope rather than invent hashing/masking/proxy state
Messaging/Health/MultiTenancy/Errors/Security/Admin certifications remain protected
latest accepted implementation remains:
f29a881370b9a8813035715ef6973145ce3f1723

ARCHITECT DECISIONS FOR W1

KEEP RequestObservabilityEnrichmentMiddleware in Host/Observability.
Namespace target = Tooba.Host.Observability.
Do NOT move middleware into BuildingBlocks.
Do NOT create a new observability options type merely to control IP logging.
Do NOT hash/mask/anonymize IP in this wave.
Remove ClientIp population from this middleware entirely.
Leave the canonical optional ClientIp key/type in BuildingBlocks untouched.
Preserve HttpPath (path only, no query string).
Preserve storeId=tenantId as logging-only current semantics.
Preserve correlation fallback and middleware order.
No logging/tracing/metrics redesign.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

MIGRATE ONLY.

Do NOT certify Observability in W1.
Do NOT start Outbox/Persistence/Configuration.
Do NOT start another Host folder.
Do NOT open module recovery.
Do NOT reopen prior certified Host folders.

PRIMARY ACTIVE FOLDER

src/backend/Host/Tooba.Host/Observability/

EXPECTED TARGET TREE

Exactly one production file:

Observability/

RequestObservabilityEnrichmentMiddleware.cs

Exactly one production type.

TARGET NAMESPACE

Tooba.Host.Observability

REQUIRED W1 CHANGES

PATH ↔ NAMESPACE

Change:
namespace Tooba.Host;

to:
namespace Tooba.Host.Observability;

Update Program/tests with:
using Tooba.Host.Observability;

No alias.
No shim.
No TypeForwardedTo.
No duplicate legacy copy.

CLIENT IP PRIVACY REPAIR

Remove:
context.Connection.RemoteIpAddress?.ToString()

from Host/Observability enrichment.

Do not pass raw ClientIp into:
ObservabilityLogScope.CreateState

Acceptable target:

omit clientIp argument entirely, or
pass null explicitly

Required final state in this Host middleware:

RemoteIpAddress read = ZERO
ClientIp scope population = ZERO
X-Forwarded-For read = ZERO
custom IP hashing = ZERO
custom IP masking = ZERO

Reason:
ClientIp is optional and canonical key contract says only if trusted-proxy safe. Host currently cannot durably prove that condition at this boundary for every request, so privacy-safe omission is preferred.

Do NOT modify BuildingBlocks ObservabilityLogScope or key definitions.

HTTP PATH

Preserve:
context.Request.Path.Value

Do NOT log:

QueryString
RawTarget
full URL
headers

No redaction redesign in W1.

CORRELATION AUTHORITY

Preserve exact resolution order:

correlationIdProvider.GetCorrelationId()
?? context.Items[CorrelationIdMiddleware.HttpContextItemKey] as string
?? correlationIdProvider.EnsureCorrelationId()

No new correlation ID implementation.
No custom ActivitySource.
No new trace propagation.

TENANT / STORE

Preserve:

tenantId = commerce.Current?.Tenant?.TenantId.Value
storeId = tenantId

This remains logging-only.

Do NOT infer Marketplace seller/store identity.
Do NOT create StoreId business authority.

ACTOR ID

Preserve:
session.UserId Guid non-empty -> "N" format

No:

username
email
phone
claims dump
token
cookie
session ID
REQUEST ID

Preserve:
context.TraceIdentifier

OBSERVABILITY LOG SCOPE

Preserve canonical:
ObservabilityLogScope.CreateState
ObservabilityLogScope.Begin

No parallel dictionary/schema.
No custom scope helper.

LOGGER

Preserve:
ILogger<RequestObservabilityEnrichmentMiddleware>

Use only for BeginScope.
Do not add explicit request logging.

MIDDLEWARE ORDER

Program order must remain:

forwarded headers conditional
correlation
exception handler
CORS
SecurityHeaders
TenantResolution
SessionAuthentication
RequestObservabilityEnrichment
endpoint maps

Only import/namespace update allowed in Program.

EXCEPTION PROPAGATION

No catch.
No swallow.
_next(context) exceptions continue naturally.
Scope must dispose via using.

HARD-CODED USER-FACING TEXT

Required ZERO.

Comments/XML docs are not runtime presentation.

CUSTOM OBSERVABILITY

Required ZERO new:

ActivitySource
Meter
custom TraceId
custom CorrelationId
custom structured log schema
FOREIGN LAYERS

Required ZERO:

module Application
module Infrastructure
module Domain
DbContext
repository
business commands
persistence access
AUTH/TENANCY AUTHORITY

Middleware may consume:

CurrentAuthenticatedSession
ICurrentCommerceContext

It must not:

authenticate
authorize
resolve tenant
mutate commerce context
W1 GUARD

Add:
HostObservabilityAmcW1GuardTests

Lock at minimum:

exact one-file tree
exact one type
exact namespace Tooba.Host.Observability
Program imports namespace
Program maps middleware exactly once
middleware remains after SessionAuthenticationMiddleware
correlation provider/fallback exact
ObservabilityLogScope canonical use
RemoteIpAddress absent
ClientIp argument absent/null
QueryString absent
HttpPath remains Path.Value
actor id Guid N
tenant/store semantics retained
no custom tracing/metrics
no catch
foreign App/Infra/Domain/DbContext zero
protected certifications in SoT
FOCUSED TESTS

Add/update focused tests for:

canonical scope receives correlation/request/tenant/store/actor/method/path
ClientIp key absent from resulting enrichment scope
query string not included
Marketplace null tenant => tenant/store absent
SingleStore tenant => tenant/store same current value
anonymous session => actor absent
authenticated Guid => actor N format
exception propagation remains

Prefer unit/focused middleware tests.

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

No production edits under those folders.

RECOVERY HYGIENE

Do not reintroduce stale historical current markers.
Do not advance another Host folder.
Automatic next remains NONE.

HOST-ONLY SCOPE

No module production changes.
No Outbox/Persistence/Configuration work.
No frontend.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-OBSERVABILITY-AMC-001-W1/

Required:

namespace.md
client-ip-privacy.md
context-parity.md
middleware-order.md
boundary-security.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-OBSERVABILITY-AMC-001-W1.task.md

RECOVERY / SOT

On PASS:

lastAcceptedTask = TB-TMAR-HOST-OBSERVABILITY-AMC-001-W1
lastAcceptedCommit = actual W1 implementation SHA
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-OBSERVABILITY-AMC-001-W1
currentHostCheckpoint = Observability

Record:
observabilityProductionFileCount = 1
observabilityProductionTypeCount = 1
pathNamespace = EXACT_Tooba.Host.Observability
middlewareDisposition = KEEP_AS_GLOBAL_HOST_OBSERVABILITY_PLATFORM
middlewareOrder = AFTER_TENANT_AND_SESSION_PRESERVED
correlationAuthority = CANONICAL_PRESERVED
clientIpLogging = OMITTED_PRIVACY_SAFE
httpPathLogging = PATH_ONLY_NO_QUERY_PRESERVED
storeIdSemantics = TENANTID_LOGGING_MIRROR_PRESERVED
actorIdSemantics = GUID_N_PRESERVED
customTracing = ZERO
customMetrics = ZERO
foreignApplication = ZERO
foreignInfrastructure = ZERO
foreignDomain = ZERO
foreignDbContext = ZERO
businessAuthority = ZERO
certificationState = NOT_CERTIFIED_W2_REQUIRED

hostMessagingCertification = HOST_MESSAGING_AMC_CERTIFIED_PRESERVED
hostHealthCertification = HOST_HEALTH_AMC_CERTIFIED_PRESERVED
hostMultiTenancyCertification = HOST_MULTITENANCY_AMC_CERTIFIED_PRESERVED
hostErrorsCertification = HOST_ERRORS_AMC_CERTIFIED_PRESERVED
hostSecurityCertification = HOST_SECURITY_AMC_CERTIFIED_PRESERVED
hostAdminCertification = HOST_ADMIN_FULLY_CERTIFIED_PRESERVED

automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_OBSERVABILITY_AMC_001_W1
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Docs stamp separate from implementation SHA.

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

exact one-file/one-type shape preserved
namespace exact Tooba.Host.Observability
Program import repaired
middleware registration order unchanged
raw ClientIp logging removed
no replacement IP collection mechanism added
query string remains unlogged
HttpPath path-only preserved
correlation authority preserved
tenant/store/actor/request fields preserved
canonical ObservabilityLogScope preserved
custom tracing/metrics zero
no catch/swallow
foreign App/Infra/Domain/DbContext zero
business authority zero
protected certs preserved
focused builds/tests PASS
W1 guard PASS
certification remains pending W2
implementation SHA updated to actual W1
automatic next NONE
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-OBSERVABILITY-AMC-001-W1
Parent-Task: TB-TMAR-HOST-OBSERVABILITY-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Migration-State:
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
Sensitive-Logging-State:
Authentication-Boundary-State:
Tenancy-Boundary-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
W1-Guard-State:
Host-Messaging-Certification-State:
Host-Health-Certification-State:
Host-MultiTenancy-Certification-State:
Host-Errors-Certification-State:
Host-Security-Certification-State:
Host-Admin-Certification-State:
Schema-Change-State:
Frontend-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
Recovery-Hygiene-State:
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

Wait for Architect/user review.

END_TOOBA_TASK