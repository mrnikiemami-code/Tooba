PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-OUTBOX-AMC-001
Parent-Task: TB-TMAR-HOST-OBSERVABILITY-AMC-001-W2-CERT
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_OUTBOX_ANALYZE
Title: Analyze Host/Outbox worker orchestration, scope activation, retry/dead-letter, context reconstruction, and structural debt
Estimated-Time-Minutes: 15
Hard-Timebox-Minutes: 19

ARCHITECT REVIEW STATE

Parent Observability certification:
ACCEPTED

Architect independently verified:

HOST_OBSERVABILITY_AMC_CERTIFIED
HOST_OBSERVABILITY_PLATFORM_BOUNDARY_CERTIFIED
production change in Observability CERT = ZERO
implementation authority remains Observability W1:
f1425fed94cc1a8354d3c9f9a013065d87cbe66c
ClientIp omission is durably guarded
Messaging/Health/MultiTenancy/Errors/Security/Admin certifications preserved

NEXT ACTIVE HOST UNIT

src/backend/Host/Tooba.Host/Outbox/

Current repository snapshot shows exactly three production files:

OutboxDispatcher.cs
OutboxHostOptions.cs
OutboxWorkerSeams.cs

Current visible top-level production types:

OutboxDispatcher.cs:

OutboxDispatcher
OutboxDispatcherHostedService

OutboxHostOptions.cs:

OutboxHostOptions

OutboxWorkerSeams.cs:

ConfiguredOutboxPollTargetSource
WorkerCommerceContextFactory
WorkerStoreCommerceContextFactory

Expected current total:
3 files / 6 top-level types

Current namespace appears:
Tooba.Host

while physical path is:
/Outbox/

Likely path↔namespace violation.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-analyze/SKILL.md

ANALYSIS ONLY.

Do NOT edit production code.
Do NOT migrate.
Do NOT certify.
Do NOT start Persistence/Configuration.
Do NOT start another Host folder.
Do NOT open module recovery.

PROTECTED STATE

Must remain:

HOST_OBSERVABILITY_AMC_CERTIFIED
HOST_MESSAGING_AMC_CERTIFIED
HOST_HEALTH_AMC_CERTIFIED
HOST_MULTITENANCY_AMC_CERTIFIED
HOST_ERRORS_AMC_CERTIFIED
HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED
frontend frozen
Checkout paused unchanged

MANDATORY ANALYSIS

EXACT TREE / TYPE ENUMERATION

Enumerate:
src/backend/Host/Tooba.Host/Outbox/

Record:

exact production file count
exact filenames
all top-level types
nested types
visibility
namespace
consumers

Expected:
3 files / 6 top-level types

PER-TYPE DISPOSITION

Classify independently:

OutboxDispatcher
OutboxDispatcherHostedService
OutboxHostOptions
ConfiguredOutboxPollTargetSource
WorkerCommerceContextFactory
WorkerStoreCommerceContextFactory

Allowed dispositions:

KEEP_AS_GLOBAL_HOST_OUTBOX_PLATFORM
KEEP_AS_GLOBAL_HOST_BACKGROUND_WORKER
KEEP_AS_THIN_HOST_WORKER_CONTEXT_ADAPTER
MOVE_TO_STORECONTEXT
MOVE_TO_PERSISTENCE
MOVE_TO_CONFIGURATION
DEAD_ZERO_CONSUMER_RESIDUE
MUST_SPLIT
BLOCKED_NEEDS_ARCHITECT_DECISION

Do not classify folder as a whole first.

PATH ↔ NAMESPACE

Current likely:
namespace Tooba.Host

Target if retained:
Tooba.Host.Outbox

Map all consumers:

Program
tests
Caching/Cart expiration worker if any
Messaging
StoreContext worker seams
Persistence abstractions
FILE COHESION

Audit:

OutboxDispatcher.cs contains dispatcher + hosted service
OutboxWorkerSeams.cs contains 3 types

Determine canonical target:
one top-level type per file?

Possible target files:

OutboxDispatcher.cs
OutboxDispatcherHostedService.cs
OutboxHostOptions.cs
ConfiguredOutboxPollTargetSource.cs
WorkerCommerceContextFactory.cs
WorkerStoreCommerceContextFactory.cs

Do not split in Analyze.

PROGRAM REGISTRATION / LIFETIMES

Trace exact Program registrations:

OutboxHostOptions binding
IOutboxDispatcherStore
IOutboxPollTargetSource
WorkerCommerceContextFactory
IWorkerCommerceContextFactory
WorkerStoreCommerceContextFactory
IWorkerStoreCommerceContextFactory
OutboxDispatcher
OutboxDispatcherHostedService
publisher dependencies

Record each lifetime.

OUTBOX DISPATCHER RESPONSIBILITY

Audit OutboxDispatcher exact responsibilities:

enumerate poll targets
enumerate module outbox registrations
resolve DB connection
claim rows
deserialize events
restore correlation
create worker DI scope
assign worker commerce context
assign store commerce context
publish integration event
mark processed/retry/dead-letter
log/metrics

Determine whether this remains one cohesive platform orchestration class or is doing too much.

Do not redesign unless clearly required.

TENANT FAILURE ISOLATION

Verify:

target-level failure does not stop other targets
module claim failure does not stop other modules
message failure does not stop other claimed messages
cancellation semantics remain correct

Classify whether broad catch is intentional worker resilience or unsafe swallow.

BROAD EXCEPTION CATCH POLICY

There are multiple catch (Exception) blocks.

Audit each separately:

target poll catch
module claim catch
message processing catch
hosted-loop catch

Determine:

expected background-worker resilience
whether unknown failures are merely logged/sanitized and continued
whether this violates "unknown faults propagate" for HTTP paths or is acceptable worker-specific isolation
whether cancellation can be accidentally swallowed

Pay special attention to OperationCanceledException inside message/claim paths.

CANCELLATION SAFETY

Trace cancellation token through:

ClaimAsync
publisher
MarkProcessed
MarkDeadLetter
MarkRetry
Task.Delay

Determine whether OperationCanceledException during message processing can be converted into retry/dead-letter rather than stopping worker.

This is a critical audit point.

RETRY / DEAD-LETTER SEMANTICS

Audit:

AttemptCount threshold
MaxAttempts comparison
exponential backoff expression
overflow/cap
RetryBaseDelaySeconds
clock source SystemClock
MarkRetry next attempt
dead-letter error text

Determine whether attempt semantics are off-by-one or consistent with store claim semantics.

No changes in Analyze.

ERROR SANITIZATION

Trace:
OutboxErrorSanitizer.Sanitize(ex)

Find owner/implementation.

Verify:

no raw exception.Message persisted if sensitive
no stack trace
bounded length
stable enough for operations
no message-text classification
no secret leak
LOGGING SENSITIVITY

Audit logged fields:

TenantId
EventType
Schema
ErrorType

Classify:

allowed operational identifiers
potential sensitive values
no payload
no connection string/reference
no exception.Message
METRICS

Audit canonical ToobaTelemetry counters:

tenant_failures
retries
dead_letters
processed

Verify:

no custom Meter
naming consistency
no duplicate metrics elsewhere
no high-cardinality dimensions attached here
CORRELATION / TRACING

Audit:

MessagingCorrelation.ResolveForPublish
CorrelationIdContext.BeginScope
ToobaTelemetry.ActivitySource
ToobaTraceEnricher
tags: event_type, tenant_id, module_schema

Verify:

canonical mechanisms only
no parallel correlation
no sensitive payload
tag cardinality/sensitivity acceptable
REFLECTION METADATA MUTATION

Current code:

deserializes event
reflects Metadata property
if CorrelationId empty, sets meta with { CorrelationId = correlationId }

Audit:

whether Metadata setter always exists
failure behavior if immutable/no setter
reflection exception risk
whether serializer should own correlation restoration
whether this is acceptable platform compatibility seam or debt

Do not redesign in Analyze.

RUNTIME SCOPE ACTIVATION

Current dispatcher uses:
IServiceScopeFactory
CreateAsyncScope()
scope.ServiceProvider.GetRequiredService<...>()

Resolve:

ICommerceContextAssigner
IStoreCommerceContextAssigner
IIntegrationEventPublisher

Classify separately from generic service locator.

Questions:

Is this legitimate per-message worker scope composition?
Are scoped services required?
Would constructor injection be wrong because dispatcher is singleton?
Is a typed WorkerScopeRunner abstraction warranted or overengineering?

Do not mechanically reject scope.ServiceProvider in background worker composition.

WORKER COMMERCE CONTEXT FACTORY

Audit WorkerCommerceContextFactory:

registry edition
Marketplace connection reference
SingleStore TenantId matching
Active status
resolved host fallback
FromOutbox
FromPollTarget

Determine:

Host platform ownership vs StoreContext ownership
whether it duplicates MultiTenancy semantics
whether it is business authority
whether it should remain a thin Host worker context adapter
FROMOUTBOX TRUST MODEL

For SingleStore:
message.TenantId selects registry tenant.

Verify:

must exist
must be Active
connection reference comes from registry, not message
message cannot inject connection string/reference
deployment/edition comes from registry
host headers irrelevant

Classify anti-spoof boundary.

FROMPOLLTARGET TRUST MODEL

Audit target-derived worker context:

target edition
target deployment id
target connection reference
tenant lookup again
active status

Check whether target values could diverge from registry state between enumeration and dispatch.

WORKER STORE COMMERCE FACTORY

Audit WorkerStoreCommerceContextFactory:

Marketplace returns DeploymentStoreCommerce
SingleStore requires active tenant and returns record.StoreCommerce

Verify:

Contracts-only StoreContext boundary
no default Market/Currency/SalesChannel
no duplicate business derivation
fail-closed
POLL TARGET SOURCE

Audit ConfiguredOutboxPollTargetSource:

Marketplace one target
SingleStore Active tenants only
Unset/other => empty
connection reference from registry
deployment ID

Compare with:

MultiTenancy active-tenant semantics
Health all-configured semantics

Determine if correct intentional difference.

OUTBOX OPTIONS

Audit OutboxHostOptions:

Enabled default true
PollIntervalSeconds=2
BatchSize=20
RetryBaseDelaySeconds=2
MaxAttempts=5
LockSeconds=30

Determine:

Host deployment options ownership
startup validation exists or absent
invalid/zero/negative values behavior
runtime clamping currently only poll delay and retry delay partially
BatchSize/MaxAttempts/LockSeconds may pass invalid values

Classify whether options validator is required before certification.

HOSTED SERVICE

Audit OutboxDispatcherHostedService:

disabled path
delay behavior
exception handling
cancellation
readiness independence
logging

Determine whether BackgroundService belongs same file or split.

PERSISTENCE BOUNDARY

Trace:

IOutboxDispatcherStore
OutboxMessage
IOutboxModuleRegistration
IIntegrationEventSerializer
IDatabaseConnectionResolver
OutboxPollTarget
OutboxErrorSanitizer

Classify which are Tooba.Persistence neutral platform seams.

Required:
no module DbContext / no direct module table access except through registered schema/table abstraction.

MODULE OUTBOX REGISTRATION BOUNDARY

Audit IEnumerable<IOutboxModuleRegistration>.

Verify:

modules expose only schema/table metadata through neutral contract
Host does not reference module Application/Infrastructure directly here
no switch by module name
no business routing logic
SQL IDENTIFIER SAFETY

Because dispatcher passes:
module.Schema
module.TableName

to store methods, determine:

store validates/quotes identifiers
registrations are trusted static metadata
injection impossible through tenant/message payload
no direct concatenation in Host
MESSAGING BOUNDARY

Verify:

Outbox dispatcher resolves IIntegrationEventPublisher only
no direct MassTransit reference
Messaging owns transport publication
Messaging CERT preserved
STORECONTEXT BOUNDARY

Verify:

only StoreContext.Contracts.Current
IStoreCommerceContextAssigner
IWorkerStoreCommerceContextFactory
StoreCommerceContext

No StoreContext Application/Infra/Domain.

HARD-CODED RUNTIME TEXT

Audit all runtime exception/log messages.

Important:
WorkerCommerceContextFactory.FromPollTarget currently contains Persian exception prose.

Classify:

internal/operator runtime text
user-facing impossible
localization exemption or production hygiene debt
consistency with stable machine error policy

Do not automatically localize internal worker exceptions.

EXCEPTION MESSAGE CLASSIFICATION

Required ZERO:

ex.Message branching
Contains/StartsWith message classification
error semantics inferred from prose
FOREIGN LAYERS / BUSINESS AUTHORITY

Required ZERO:

module Application
module Infrastructure
module Domain
module DbContext
business commands
business policy
business mutation

If any exists, identify exact line/owner.

TEST / GUARD INVENTORY

Find focused tests for:

target enumeration Marketplace/SingleStore
active/inactive tenants
worker context reconstruction
store-commerce reconstruction
claim failure isolation
target failure isolation
message retry/dead-letter
cancellation
backoff calculation
options invalid values
disabled hosted service
correlation restore
scope assignment
no direct MassTransit
sensitive logging
path/namespace/file cohesion

Identify missing coverage.

HISTORICAL CLAIMS

Inspect Outbox foundation docs / existing SoT / old worker cleanup.

Classify:

foundation certified
old Host worker cleanup historical
current Outbox folder certification state

Do not infer folder cert from prior Outbox foundation.

DECISIVE TARGET PLAN

Recommend fewest safe waves <=20 min.

Possible:
A. W1 structural namespace/file split + bounded correctness repairs + validator if needed; W2-CERT
B. W1 structure, W2 behavioral repair, W3-CERT if cancellation/options issues are material
C. direct CERT only if no production debt (unlikely due namespace/cohesion)

Each wave must specify exact files, behavior, tests, estimate.

HOST-ONLY SCOPE LOCK

No module recovery.
No Persistence/Configuration AMC.
Module files may be inspected only for contract truth.

PRODUCTION CHANGE RULE

Analyze production change:
ZERO

Docs/evidence/SoT only.

FOCUSED VALIDATION

No solution-wide tests.
No production build unless necessary to resolve consumer ambiguity.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-OUTBOX-AMC-001/

Required:

analyze.md
type-dispositions.md
dispatcher-semantics.md
cancellation-retry-deadletter.md
worker-scope.md
worker-context-trust.md
persistence-messaging-boundaries.md
options-validation.md
observability-sensitive-data.md
path-namespace-cohesion.md
tests-guards.md
historical-claims.md
migration-plan.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-OUTBOX-AMC-001.task.md

RECOVERY / SOT

Analysis-only.
Do NOT advance implementation SHA.

Record:

currentHostCheckpoint = Outbox
mode = ANALYSIS_ONLY_NOT_MIGRATED_NOT_CERTIFIED
productionFileCount = actual
productionTypeCount = actual
pathNamespaceState
fileCohesionState
broadExceptionPolicyState
cancellationSafetyState
retryDeadLetterState
workerScopeActivationState
workerContextTrustState
storeCommerceBoundaryState
optionsValidationState
recommendedWaveCount
recommendedNextTask
Observability certification = PRESERVED
Messaging certification = PRESERVED
Health certification = PRESERVED
MultiTenancy certification = PRESERVED
Errors certification = PRESERVED
Security certification = PRESERVED
Admin certification = PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_OUTBOX_AMC_001
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Latest accepted implementation remains:
f1425fed94cc1a8354d3c9f9a013065d87cbe66c

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

exact Outbox tree/types enumerated
six types dispositioned
path/namespace debt classified
file cohesion decision made
DI lifetimes/consumers mapped
dispatcher responsibility audited
target/module/message failure isolation audited
broad exception policy classified
cancellation safety proven or blocker identified
retry/dead-letter attempt semantics audited
error sanitizer audited
logging/metrics/correlation safety audited
metadata reflection seam classified
runtime scope activation classified
worker commerce/store context trust boundaries audited
poll target semantics compared to MultiTenancy/Health
options validation completeness audited
hosted service behavior audited
Persistence/Messaging/StoreContext boundaries proven
SQL identifier safety audited
hard-coded runtime text classified
exception-message classification zero
foreign module/business authority zero or exact blocker
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
Task-ID: TB-TMAR-HOST-OUTBOX-AMC-001
Parent-Task: TB-TMAR-HOST-OBSERVABILITY-AMC-001-W2-CERT
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
OutboxDispatcher-State:
OutboxDispatcherHostedService-State:
OutboxHostOptions-State:
ConfiguredOutboxPollTargetSource-State:
WorkerCommerceContextFactory-State:
WorkerStoreCommerceContextFactory-State:
Type-Disposition-State:
Path-Namespace-State:
File-Cohesion-State:
Program-DI-Lifetime-State:
Dispatcher-Responsibility-State:
Target-Failure-Isolation-State:
Module-Failure-Isolation-State:
Message-Failure-Isolation-State:
Broad-Exception-Policy-State:
Cancellation-Safety-State:
Retry-DeadLetter-State:
Attempt-Semantics-State:
Error-Sanitizer-State:
Logging-Sensitivity-State:
Metrics-State:
Correlation-Tracing-State:
Metadata-Reflection-State:
Worker-Scope-Activation-State:
Worker-Commerce-Context-Trust-State:
PollTarget-Trust-State:
Worker-StoreCommerce-State:
PollTarget-Source-State:
Options-Validation-State:
HostedService-State:
Persistence-Boundary-State:
Module-Registration-Boundary-State:
Sql-Identifier-Safety-State:
Messaging-Boundary-State:
StoreContext-Boundary-State:
Hardcoded-Runtime-Text-State:
Exception-Message-Classification-State:
Sensitive-Data-State:
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
Host-Observability-Certification-State:
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

Do not start Outbox W1.
Do not start Persistence/Configuration.
Do not start another Host folder.
Do not open module recovery.

Wait for Architect/user review.

END_TOOBA_TASK