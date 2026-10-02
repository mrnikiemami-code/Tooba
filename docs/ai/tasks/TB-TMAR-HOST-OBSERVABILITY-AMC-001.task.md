PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-OBSERVABILITY-AMC-001
Parent-Task: TB-TMAR-HOST-MESSAGING-AMC-001-W2-CERT
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_OBSERVABILITY_ANALYZE
Title: Analyze Host/Observability request log-scope enrichment boundary and privacy-safe context propagation
Estimated-Time-Minutes: 8
Hard-Timebox-Minutes: 12

ARCHITECT REVIEW STATE

Parent Messaging certification:
ACCEPTED

Architect independently verified:

HOST_MESSAGING_AMC_CERTIFIED
HOST_MESSAGING_PLATFORM_BOUNDARY_CERTIFIED
production change in Messaging CERT = ZERO
implementation authority remains Messaging W1:
f29a881370b9a8813035715ef6973145ce3f1723
docs stamp advanced separately after cert
Health/MultiTenancy/Errors/Security/Admin certifications preserved

NEXT ACTIVE HOST UNIT

src/backend/Host/Tooba.Host/Observability/

Current repository snapshot shows exactly one production file:

RequestObservabilityEnrichmentMiddleware.cs

Current namespace appears:
Tooba.Host

while physical path is:
/Observability/

Likely path↔namespace violation:
target if retained = Tooba.Host.Observability

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-analyze/SKILL.md

ANALYSIS ONLY.

Do NOT edit production code.
Do NOT migrate.
Do NOT certify.
Do NOT start another Host folder.
Do NOT reopen Messaging/Health/MultiTenancy/Errors/Security/Admin.

PROTECTED STATE

Must remain:

HOST_MESSAGING_AMC_CERTIFIED
HOST_HEALTH_AMC_CERTIFIED
HOST_MULTITENANCY_AMC_CERTIFIED
HOST_ERRORS_AMC_CERTIFIED
HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED
frontend frozen
Checkout paused state unchanged

MANDATORY ANALYSIS

EXACT TREE / TYPE ENUMERATION

Enumerate:
src/backend/Host/Tooba.Host/Observability/

Record:

exact production file count
exact filenames
exact type count
namespace
visibility
consumers

Expected current:
1 file / 1 type

TYPE DISPOSITION

Classify:
RequestObservabilityEnrichmentMiddleware

Allowed dispositions:

KEEP_AS_GLOBAL_HOST_OBSERVABILITY_PLATFORM
KEEP_AS_THIN_HOST_OBSERVABILITY_ADAPTER
MOVE_TO_BUILDINGBLOCKS
DEAD_ZERO_CONSUMER_RESIDUE
BLOCKED_NEEDS_ARCHITECT_DECISION

Do not move in Analyze.

PATH ↔ NAMESPACE

Current likely:
path = Host/Observability/
namespace = Tooba.Host

Classify exact violation.

If retained:
target expected:
Tooba.Host.Observability

Map impact on:

Program
tests
any direct type references
PIPELINE POSITION

Verify exact middleware order in Program relative to:

correlation middleware
exception handler
CORS
security headers
tenant resolution
session authentication
observability enrichment
endpoint execution

Determine whether current position is intentional:
after tenant + session so scope can include tenant/store/actor.

CORRELATION AUTHORITY

Audit:
ICorrelationIdProvider
CorrelationIdMiddleware.HttpContextItemKey
EnsureCorrelationId()

Determine:

canonical authority
fallback behavior
whether middleware ever creates competing correlation ID
whether fallback to HttpContext.Items duplicates foundation semantics
whether this is defensive or architectural debt
COMMERCE CONTEXT

Audit:
ICurrentCommerceContext

Uses:
commerce.Current?.Tenant?.TenantId.Value

Determine:

contracts-only / neutral foundation boundary
no module dependency
marketplace behavior when Tenant null
SingleStore semantics
STORE ID SEMANTICS

Current:
storeId = tenantId

Comment says Single-Store durable store identity equals TenantId when no separate StoreId exists.

Analyze carefully:

Is this semantically valid for Marketplace?
Could tenantId represent seller/store incorrectly?
Is storeId expected to be null in Marketplace?
Is this only a logging dimension, not business authority?
Does ObservabilityLogScope contract define storeId independently?

Do not change yet.

ACTOR ID / SESSION BOUNDARY

Audit:
CurrentAuthenticatedSession
session.UserId Guid

Determine:

Host authentication platform dependency acceptable?
actorId formatting safe?
no username/email/phone/PII
no auth token/session secret
CLIENT IP PRIVACY

Current:
context.Connection.RemoteIpAddress?.ToString()

This is important.

Classify:

whether client IP is considered sensitive/personal data
whether canonical sensitive logging rules allow it
whether it should be logged raw, masked, hashed, omitted, or environment-gated
whether trusted proxy normalization is already applied before this middleware
whether current logging policy has explicit allowance

Do not change in Analyze.

HTTP PATH SAFETY

Current:
context.Request.Path.Value

Audit risk of:

identifiers in path segments
emails/phones/slugs/order IDs/user IDs
secrets/tokens in path
query string exclusion

Determine whether raw path is allowed by sensitive logging policy.

REQUEST ID

Current:
context.TraceIdentifier

Classify:

safe
duplicate with correlationId
useful independent server request id
LOG SCOPE KEYS / CONTRACT

Inspect:
ObservabilityLogScope.CreateState
ObservabilityLogScope.Begin

Document exact emitted fields.

Verify:

canonical BuildingBlocks implementation
no parallel log scope schema
no custom dictionary conventions
no sensitive fields beyond those explicitly passed
NO CUSTOM TRACING

Audit Host/Observability file for:

ActivitySource
Activity
Meter
custom traceparent/tracestate
custom correlation implementation

Expected:
ZERO custom tracing.
This middleware should enrich logs only.

LOGGER OWNERSHIP

Current:
ILogger<RequestObservabilityEnrichmentMiddleware>

Determine:

logger only used to open scope
no duplicate request completion logging
no message templates/PII
no logging side effects
EXCEPTION BEHAVIOR

await _next(context)

No catch expected.

Verify:

exceptions propagate to canonical exception middleware according to pipeline order
scope disposal still occurs
no swallow
HARD-CODED RUNTIME TEXT

Audit actual production strings.

Expected mostly none.

Classify:

comments not runtime
log keys are canonical framework keys if any
user-facing runtime prose = ZERO
FOREIGN LAYER / BUSINESS AUTHORITY

Required ZERO:

foreign Application
foreign Infrastructure
foreign Domain
DbContext
repository
business command
business decision
persistence access
AUTHENTICATION / TENANCY BOUNDARIES

Verify only consumption:

CurrentAuthenticatedSession
ICurrentCommerceContext

No authority to authenticate, authorize, or resolve tenant.

FORWARDED HEADERS / REMOTE IP

Verify Program applies forwarded headers before this middleware when configured.

Determine:

whether RemoteIpAddress is post-proxy normalized
whether absence of trusted proxies affects semantics
whether logging spoof risk exists
SENSITIVE LOGGING POLICY

Read canonical architecture rules / guards for:

raw IP
actorId
tenantId
storeId
HTTP path

Classify each:
ALLOWED
ALLOWED_WITH_CONDITION
SENSITIVE_DEBT
FORBIDDEN

CONSUMER / DEAD CODE

Verify middleware is mapped once in Program.

No duplicate observability enrichment middleware elsewhere.

TEST / GUARD INVENTORY

Find tests for:

middleware order
correlation fallback
tenant/store/actor scope fields
raw IP
HTTP path
no query string
exception propagation
scope disposal
namespace/path

Identify missing coverage.

HISTORICAL CLAIMS

Inspect Foundation observability docs and current SoT.

Classify:

foundation behavior may be certified
Host/Observability folder itself is or is not currently certified

Do not conflate foundation certification with folder certification.

DECISIVE TARGET PLAN

Prefer the fewest safe waves.

Likely outcomes:

A. one W1 + CERT:

namespace repair
any bounded sensitive logging correction if needed
guard/tests
then cert

B. direct CERT only if no production debt exists other than namespace and namespace is intentionally exempted (unlikely)

C. repair split if privacy issue needs separate bounded change.

Every wave <=20 min.

HOST-ONLY SCOPE LOCK

No module recovery.
No next Host folder.

PRODUCTION CHANGE RULE

Analyze production change:
ZERO

Docs/evidence/SoT only.

FOCUSED VALIDATION

No solution-wide tests.
No production build unless needed for consumer ambiguity.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-OBSERVABILITY-AMC-001/

Required:

analyze.md
middleware-order.md
correlation-context.md
sensitive-logging.md
path-namespace.md
boundary-contracts.md
tests-guards.md
historical-claims.md
migration-plan.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-OBSERVABILITY-AMC-001.task.md

RECOVERY / SOT

Analysis-only.

Do NOT advance implementation SHA.

Record:

currentHostCheckpoint = Observability
mode = ANALYSIS_ONLY_NOT_MIGRATED_NOT_CERTIFIED
productionFileCount = actual
productionTypeCount = actual
pathNamespaceState
middlewareOrderState
correlationAuthorityState
storeIdSemanticsState
clientIpLoggingState
httpPathLoggingState
sensitiveLoggingState
recommendedWaveCount
recommendedNextTask
Messaging certification = PRESERVED
Health certification = PRESERVED
MultiTenancy certification = PRESERVED
Errors certification = PRESERVED
Security certification = PRESERVED
Admin certification = PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_OBSERVABILITY_AMC_001
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Latest accepted implementation remains:
f29a881370b9a8813035715ef6973145ce3f1723

GIT

Latest origin/main.
No reset.
No clean.
No rebase.
No force push.

Docs-only task/evidence/SoT stamp allowed.
No production code.

Preserve unrelated user work.

SUCCESS CRITERIA

PASS only if:

exact one-file tree verified
middleware disposition decided
namespace debt classified
pipeline position verified
correlation authority verified
tenant/store/actor semantics audited
raw client IP privacy classified
raw HTTP path safety classified
canonical log-scope keys mapped
no custom tracing/metrics proven
exception propagation verified
sensitive logging policy reconciled
foreign layer/business authority zero
duplicate middleware zero
tests/guards mapped
historical claims reconciled
fewest safe waves proposed
protected certs preserved
production change ZERO
implementation SHA unchanged
automatic next NONE
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-OBSERVABILITY-AMC-001
Parent-Task: TB-TMAR-HOST-MESSAGING-AMC-001-W2-CERT
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
RequestObservabilityEnrichmentMiddleware-State:
Path-Namespace-State:
Program-Registration-State:
Middleware-Order-State:
Correlation-Authority-State:
Correlation-Fallback-State:
CommerceContext-Boundary-State:
StoreId-Semantics-State:
ActorId-State:
ClientIp-Logging-State:
HttpPath-Logging-State:
RequestId-State:
ObservabilityLogScope-State:
Custom-Tracing-State:
Custom-Metrics-State:
Logger-State:
Exception-Propagation-State:
Hardcoded-User-Facing-Text-State:
Sensitive-Logging-State:
Forwarded-Headers-State:
Authentication-Boundary-State:
Tenancy-Boundary-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
Active-Consumer-State:
Guard-Impact-State:
Historical-Claims-State:
Recommended-Wave-Count:
Recommended-Next-Task:
Production-Code-Change-State:
Host-Messaging-Certification-State:
Host-Health-Certification-State:
Host-MultiTenancy-Certification-State:
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

Do not start Observability W1.
Do not start another Host folder.
Do not open module recovery.

Wait for Architect/user review.

END_TOOBA_TASK