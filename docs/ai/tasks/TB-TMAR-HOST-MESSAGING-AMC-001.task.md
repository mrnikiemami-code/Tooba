PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-MESSAGING-AMC-001
Parent-Task: TB-TMAR-HOST-HEALTH-AMC-001-W2-CERT
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_MESSAGING_ANALYZE
Title: Analyze Host/Messaging transport composition, publisher ownership, test-double service-location, options, retry, and path/namespace debt
Estimated-Time-Minutes: 13
Hard-Timebox-Minutes: 18

ARCHITECT REVIEW STATE

Parent Health certification:
ACCEPTED

Architect independently verified:

HOST_HEALTH_AMC_CERTIFIED
HOST_HEALTH_PLATFORM_BOUNDARY_CERTIFIED
exact 2-file Health tree
exact namespace Tooba.Host.Health
IServiceProvider/service locator ZERO in Health
explicit bus DI collection certified
disclosure ZERO
production change in Health CERT = ZERO
latest accepted implementation remains Health W1:
ba8db8c6bcb22f0ad4c386073d3b612e3d318e00
MultiTenancy/Errors/Security/Admin certifications preserved
stale historical Recovery markers remain removed

NEXT ACTIVE HOST UNIT

src/backend/Host/Tooba.Host/Messaging/

Current repository snapshot shows exactly six production files:

InProcessIntegrationEventPublisher.cs
MassTransitIntegrationEventPublisher.cs
MessagingDisabledPublisher.cs
MessagingHostOptions.cs
MessagingRegistration.cs
MessagingRetryConfigurator.cs

Do NOT assume; re-enumerate from disk.

Current visible top-level production types appear to be seven because
MessagingHostOptions.cs contains both:

MessagingHostOptions
MessagingOptionsValidator

Likely current namespace for all files:
Tooba.Host

while physical path is:
/Messaging/

This is a likely path↔namespace violation.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-analyze/SKILL.md

ANALYSIS ONLY.

Do NOT edit production code.
Do NOT migrate.
Do NOT certify.
Do NOT start another Host folder.
Do NOT reopen Health/MultiTenancy/Errors/Security/Admin.

PROTECTED STATE

Must remain:

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
src/backend/Host/Tooba.Host/Messaging/

Record:

exact recursive production .cs count
exact filenames
all top-level production types
nested types
namespaces
visibility
one-file/multi-type cohesion

Expected current:
6 files
7 top-level types

PER-TYPE DISPOSITION

Classify each independently:

InProcessIntegrationEventPublisher
MassTransitIntegrationEventPublisher
MessagingDisabledPublisher
MessagingHostOptions
MessagingOptionsValidator
MessagingRegistration
MessagingRetryConfigurator

Allowed dispositions:

KEEP_AS_GLOBAL_HOST_MESSAGING_PLATFORM
KEEP_AS_THIN_HOST_TRANSPORT_ADAPTER
KEEP_AS_EXPLICIT_TEST_ONLY_HOST_DOUBLE
MOVE_TO_TRANSPORT
MOVE_TO_BUILDINGBLOCKS
MOVE_TO_PERSISTENCE
DEAD_ZERO_CONSUMER_RESIDUE
MUST_SPLIT
BLOCKED_NEEDS_ARCHITECT_DECISION

Do not classify whole folder first.

ACTIVE CONSUMER / DI INVENTORY

Trace exact production usage and registration for each type.

Include:

Program.cs
Outbox publisher consumption
transport consumer path
MassTransit registration
Testing path
disabled path
options validation
retry registration

For each:

lifetime
active environments
active consumers
test-only consumers
zero-consumer status if applicable
OWNERSHIP MODEL

Determine canonical responsibility split among:

Host/Messaging:

process-level messaging composition
transport selection
publisher adapter registration
retry topology
explicit Testing double
disabled fail-closed publisher
host messaging options

Host/Transport:

transport envelope
transport consumer
receive-side dispatch

BuildingBlocks:

integration event contracts
serializer abstraction
correlation/tracing abstractions

Persistence:

shared SQL/outbox infrastructure primitives

Determine whether any current file is in the wrong owner.

No moves in Analyze.

PATH ↔ NAMESPACE

All Messaging files currently appear to declare:
namespace Tooba.Host

Classify exact structural debt.

If retained in Messaging:
target namespace expected:
Tooba.Host.Messaging

Map impact on:

Program
Health
Transport
tests
Outbox
any module consumers
INPROCESS TEST DOUBLE — SERVICE LOCATION

Current InProcessIntegrationEventPublisher holds:
IServiceProvider _services

and dynamically resolves:
_services.GetServices(handlerType)

Analyze carefully.

Explicitly classify:

runtime service locator debt
acceptable explicit Testing-only dispatch double
reflection-based test-only compatibility seam
candidate for scoped handler registry abstraction
candidate for removal if zero real test need

Do not mechanically reject because it is IServiceProvider.
Prove:

it can only be registered in Testing
production can never reach it
tests rely on it or not
whether a cleaner explicit typed dispatch seam exists without overengineering
REFLECTION-BASED HANDLER INVOCATION

Current InProcessIntegrationEventPublisher:

MakeGenericType
GetServices(Type)
GetMethod
MethodInfo.Invoke

Assess:

test-only acceptability
semantic parity with production bus path
exception wrapping risk (TargetInvocationException)
cancellation behavior
handler ordering
multiple handlers
zero handlers
whether unexpected exceptions preserve original semantics

No redesign in Analyze.

MASS TRANSIT PUBLISHER OWNERSHIP

Audit MassTransitIntegrationEventPublisher.

Verify:

only adapts IIntegrationEventPublisher → MassTransit IBus
no business logic
serializer neutral boundary
envelope construction ownership
metadata/correlation semantics
header propagation
traceparent/tracestate propagation
no persistence authority
no direct module dependency

Classify whether Tooba.Host.Transport dependency is legitimate or should be inverted.

TRANSPORT ENVELOPE BOUNDARY

Trace:

ToobaIntegrationTransportMessage
ToobaIntegrationTransportConsumer
SqlTransportOptionsMapper

Determine:

exact file owners
namespace
whether Messaging→Transport is acceptable same-Host platform dependency
whether Transport→Messaging exists
whether circular responsibility exists

Do NOT open Transport AMC.

PERSISTENCE DEPENDENCY

MassTransitIntegrationEventPublisher imports:
Tooba.Persistence

MessagingRegistration uses:

Npgsql
IDatabaseConnectionResolver
NpgsqlDataSource

Classify:

neutral platform persistence dependency
Host Persistence responsibility
transport-specific infrastructure
any improper business persistence authority

Required:
no module DbContext/business repository.

SQL TRANSPORT REGISTRATION

Audit MessagingRegistration:

SqlTransportOptions
AddPostgresMigrationHostedService
MassTransitHostOptions
NpgsqlDataSource registration
AddMassTransit
UsingPostgres
ConfigureEndpoints
consumer endpoint name
AutoStart
retry config

Determine if Host/Messaging is the correct composition root for these.

COMPOSITION CALLBACK SERVICE PROVIDER USAGE

MessagingRegistration uses DI callbacks such as:
sp.GetRequiredService<...>()
and MassTransit callback context.GetRequiredService<...>().

Classify separately from runtime service locator.

Expected question:
Is service-provider use inside DI registration/composition callbacks acceptable framework composition, or architecture debt?

Do not conflate with application/runtime service locator.

MESSAGING OPTIONS

Audit MessagingHostOptions fields:

Enabled
Transport
ConnectionReference
Schema
UseInProcessTestDouble
CanonicalTransport

Classify:

Host-owned deployment options
secret/non-secret fields
mutable config object semantics
startup validation completeness
naming/path ownership
MESSAGING OPTIONS VALIDATOR

Audit MessagingOptionsValidator.

Verify:

Enabled + test double conflict
transport allowed values
required ConnectionReference
schema restrictions
Production behavior
duplicated/redundant checks
hard-coded startup validation prose
whether messages are developer/operator startup errors, not user-facing runtime presentation

Identify dead/redundant branch:
Production ConnectionReference check may duplicate prior generic enabled check.
Classify but do not change yet.

FAIL-CLOSED TRANSPORT SELECTION

Trace AddToobaIntegrationPublisher exact decision tree:

UseInProcessTestDouble true + Testing => in-process
UseInProcessTestDouble true + non-Testing => throw
Enabled => MassTransit
disabled => MessagingDisabledPublisher

Verify:

no silent fallback
no environment ambiguity
no production test double
no in-process production bypass
disabled publisher does not drop messages
DISABLED PUBLISHER

Audit MessagingDisabledPublisher:

throws InvalidOperationException
message is hard-coded English runtime prose

Classify carefully:

developer/operator misconfiguration exception
business/user-facing runtime error
expected contract fault
whether stable machine error/code is needed
whether exception could surface through user request path and leak operator text via canonical SafeErrorMapper

Do not assume acceptable or unacceptable without tracing consumers.

HARD-CODED RUNTIME TEXT

Audit all string literals.

Classify each as:

startup validation/operator configuration
protocol name
endpoint name
telemetry name
machine header
internal exception prose
user-facing runtime prose

Important:
MessagingDisabledPublisher and reflection errors execute at runtime.
Determine whether canonical presentation can expose them or safely maps unknown exception generically.

EXCEPTION / MESSAGE CLASSIFICATION

Verify ZERO:

ex.Message branching
Message.Contains
StartsWith message matching
transport error text mapping
InvalidOperationException message classification
RETRY POLICY OWNERSHIP

MessagingRetryConfigurator current:

Immediate(2)
intervals 5s, 15s, 30s

Audit:

consumer retry only
no infinite retry
no hidden redelivery elsewhere
whether retry policy is global platform transport policy
whether poison message / dead-letter semantics exist
whether this duplicates module-specific retry authority

Do not redesign retry policy unless ownership is clearly wrong.

OBSERVABILITY / CORRELATION

Audit both publishers.

Canonical mechanisms:

ToobaTelemetry
MessagingCorrelation
CorrelationIdContext
ToobaTraceEnricher

Verify:

no parallel ActivitySource/Meter
metric names unique/intentional
correlation precedence
current Activity reuse vs owned Activity
header propagation
event/tenant/deployment tags
no sensitive payload logging

Note potential metric semantic difference:

in-process counter = tooba.outbox.published
MassTransit counter = tooba.messaging.published

Determine whether intentional or inconsistent.

HEADER / METADATA SAFETY

Audit publisher headers:

event type
tenant id
edition
deployment id
event id
correlation id
traceparent
tracestate

Classify sensitivity.
Ensure:

no payload in headers
no connection refs
no secrets
SERIALIZATION BOUNDARY

Trace IIntegrationEventSerializer:

owner
implementation
type map
failure behavior
payload Json creation
whether Host/Messaging owns serialization policy or only consumes neutral abstraction

No serializer redesign.

EVENT ENVELOPE / VERSIONING

Inspect transport message fields and consumer behavior.

Verify:

EventType
Version
EventId
OccurredAt
TenantId
Edition
DeploymentId
CorrelationId
PayloadJson

Analyze microservice readiness:

stable contract
version handling
unknown event behavior
duplicate/idempotency responsibility
inbox ownership

Do NOT start Inbox/Transport work.

TEST DOUBLE SEMANTIC PARITY

Compare in-process path vs MassTransit path:

serializer used or bypassed
transport envelope used or bypassed
tracing/correlation
handler dispatch
retry
consumer/inbox path
idempotency
transaction boundary

Determine what tests using in-process double can and cannot prove.

MESSAGING ↔ OUTBOX BOUNDARY

Trace IIntegrationEventPublisher consumers, especially OutboxDispatcher.

Determine:

Outbox owns polling/delivery orchestration
Messaging owns transport publication
no duplicate retry authority
publish success semantics
publisher exceptions and Outbox retry behavior

Do NOT open Outbox AMC.

MESSAGING ↔ HEALTH BOUNDARY

Health now directly uses IBusControl readiness only.

Verify W1 Health did not create forbidden dependency into Messaging internals.

Health may read MessagingHostOptions currently.
Classify whether acceptable adjacent Host platform dependency.

Do not modify Health.

MULTIPLE TYPE FILE

MessagingHostOptions.cs currently contains:

MessagingHostOptions
MessagingOptionsValidator

Determine whether canonical target should split:

MessagingHostOptions.cs
MessagingOptionsValidator.cs

Apply cohesion/path/namespace standard.
No forced split unless justified.

FOREIGN LAYER / BUSINESS AUTHORITY

Required ZERO:

module Application
module Infrastructure
module Domain
module DbContext
business repository
business command
domain decision
cross-module business orchestration

Inspect all using/imports and runtime calls.

DIRECT NPGSQL / MASSTRANSIT POLICY

These are infrastructure libraries directly in Host/Messaging.

Determine whether this is legitimate Host platform transport composition or should live under Host/Transport/Persistence.

Decision must optimize future microservice extraction, not folder aesthetics.

SECURITY / SECRET HANDLING

Audit:

connection strings
connection reference names
schema names
payload JSON
headers
exception messages
logs
traces

Verify no secrets are logged/exposed.

TEST / GUARD INVENTORY

Map focused tests for:

options validator
production test-double rejection
disabled publisher
in-process test double
MassTransit publisher envelope/header/correlation
retry policy
registration topology
SQL transport
no RabbitMQ
path/namespace
direct module deps
no service locator where prohibited

Classify missing coverage.

HISTORICAL SOT / DOC CLAIMS

Inspect:

messaging foundation docs
MassTransit PostgreSQL SQL Transport docs
observability foundation
Outbox foundation
current SoT
previous messaging certification claims

Classify:

CURRENT
HISTORICAL
STALE_METADATA
NOT_CERTIFIED

Do not infer current certification from foundation docs.

DECISIVE TARGET PLAN

Recommend fewest safe waves.

Possible examples:

Option A:
W1 structure + namespace + cohesion + obvious hygiene
W2 runtime/test-double or config repair if materially needed
W3-CERT

Option B:
W1 all bounded Messaging repairs
W2-CERT

Prefer B if safe and <=20 min.

Each proposed wave:

exact files
exact ownership
behavior preserved/changed
focused tests
estimate <=20 min
HOST-ONLY SCOPE LOCK

User explicitly requested:
FINISH HOST ONLY.

No module recovery.
No Catalog/Fulfillment/Checkout work.
Module files may only be inspected for dependency truth.

PRODUCTION CHANGE RULE

Analyze production change:
ZERO

Docs/evidence/SoT only.

FOCUSED VALIDATION

No solution-wide tests.
No production build unless required to resolve registration ambiguity.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-MESSAGING-AMC-001/

Required:

analyze.md
type-dispositions.md
consumers-di.md
ownership-boundary.md
service-location.md
options-validation.md
transport-topology.md
observability-correlation.md
outbox-health-boundaries.md
path-namespace-cohesion.md
tests-guards.md
historical-claims.md
migration-plan.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-MESSAGING-AMC-001.task.md

RECOVERY / SOT

Analysis-only.

Do NOT advance implementation SHA.

Record:

currentHostCheckpoint = Messaging
mode = ANALYSIS_ONLY_NOT_MIGRATED_NOT_CERTIFIED
productionFileCount = actual
productionTypeCount = actual
pathNamespaceState
inProcessServiceLocatorState
compositionCallbackServiceProviderState
testDoubleState
disabledPublisherState
optionsValidatorState
transportOwnershipState
retryOwnershipState
observabilityState
recommendedWaveCount
recommendedNextTask
Health certification = PRESERVED
MultiTenancy certification = PRESERVED
Errors certification = PRESERVED
Security certification = PRESERVED
Admin certification = PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_MESSAGING_AMC_001
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Latest accepted implementation remains:
ba8db8c6bcb22f0ad4c386073d3b612e3d318e00

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

exact Messaging tree enumerated
all seven expected top-level types dispositioned separately
all DI/consumers/lifetimes mapped
Host/Messaging vs Transport/BuildingBlocks/Persistence ownership decided
path↔namespace debt classified
in-process IServiceProvider/reflection path classified
composition callback DI usage classified separately
MassTransit publisher boundary proven
transport envelope boundary mapped
SQL transport composition mapped
options/validator audited
disabled fail-closed publisher audited
hard-coded runtime text classified
message classification proven zero
retry ownership mapped
observability/correlation audited
header sensitivity audited
serializer/versioning boundary mapped
test-double parity limitations documented
Outbox boundary mapped
Health boundary mapped
foreign module/business authority zero or exact blocker named
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
Task-ID: TB-TMAR-HOST-MESSAGING-AMC-001
Parent-Task: TB-TMAR-HOST-HEALTH-AMC-001-W2-CERT
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
InProcessIntegrationEventPublisher-State:
MassTransitIntegrationEventPublisher-State:
MessagingDisabledPublisher-State:
MessagingHostOptions-State:
MessagingOptionsValidator-State:
MessagingRegistration-State:
MessagingRetryConfigurator-State:
Type-Disposition-State:
Production-Consumer-Audit-State:
DI-Lifetime-State:
Ownership-Boundary-State:
Path-Namespace-State:
InProcess-ServiceLocator-State:
Reflection-Dispatch-State:
Composition-Callback-ServiceProvider-State:
MassTransit-Publisher-Boundary-State:
Transport-Envelope-Boundary-State:
Persistence-Dependency-State:
SqlTransport-Registration-State:
Options-Validation-State:
FailClosed-Transport-Selection-State:
Disabled-Publisher-State:
Hardcoded-Runtime-Text-State:
Exception-Message-Classification-State:
Retry-Policy-State:
Observability-Correlation-State:
Metric-Semantics-State:
Header-Sensitivity-State:
Serialization-Boundary-State:
Event-Versioning-State:
TestDouble-Parity-State:
Outbox-Boundary-State:
Health-Boundary-State:
File-Cohesion-State:
Direct-Npgsql-MassTransit-State:
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

Do not start Messaging W1.
Do not start another Host folder.
Do not open module recovery.

Wait for Architect/user review.

END_TOOBA_TASK