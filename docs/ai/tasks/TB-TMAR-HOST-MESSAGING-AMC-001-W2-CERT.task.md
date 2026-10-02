PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-MESSAGING-AMC-001-W2-CERT
Parent-Task: TB-TMAR-HOST-MESSAGING-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_MESSAGING_CERTIFY
Title: Independently certify Host/Messaging as a thin global transport-composition boundary
Estimated-Time-Minutes: 11
Hard-Timebox-Minutes: 15

ARCHITECT REVIEW STATE

W1:
ACCEPTED FOR CERTIFICATION ENTRY

Architect independently verified current main:

Implementation commit:
f29a881370b9a8813035715ef6973145ce3f1723

Current Messaging production truth:

exact 7 production files
exact 7 top-level production types
namespace exact Tooba.Host.Messaging
one top-level type per file
MessagingHostOptions / MessagingOptionsValidator split complete
redundant Production-only duplicate ConnectionReference validation removed with preserved enabled-path semantics
Testing-only InProcessIntegrationEventPublisher remains explicit
IServiceProvider/reflection confined to Testing-only double
framework composition callback GetRequiredService remains only in MessagingRegistration
fail-closed publisher selection preserved
retry policy exact
MassTransit/PostgreSQL topology preserved
Health imports only repaired
Health/MultiTenancy/Errors/Security/Admin certifications preserved
foreign module App/Infra/Domain/DbContext zero
latest accepted implementation now W1 commit above

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-certify/SKILL.md

CERTIFY ONLY.

No production migration.
No production redesign.
No Transport AMC.
No Persistence AMC.
No Outbox AMC.
No next Host folder.
No module recovery.

If any material production defect is found:
Status = INCOMPLETE
Production-Repair-Required-State = YES
STOP.

PRIMARY CERTIFICATION SCOPE

src/backend/Host/Tooba.Host/Messaging/

Expected exact tree:

Messaging/

InProcessIntegrationEventPublisher.cs
MassTransitIntegrationEventPublisher.cs
MessagingDisabledPublisher.cs
MessagingHostOptions.cs
MessagingOptionsValidator.cs
MessagingRegistration.cs
MessagingRetryConfigurator.cs

Expected top-level type count:
7

Expected namespace:
Tooba.Host.Messaging

Certification labels on PASS:

HOST_MESSAGING_AMC_CERTIFIED
HOST_MESSAGING_PLATFORM_BOUNDARY_CERTIFIED

MANDATORY CERTIFICATION AUDITS

EXACT TREE / NAMESPACE / COHESION

Verify:

exact .cs file count = 7
exact expected filenames
exact top-level type count = 7
exactly one top-level type per file
namespace exact Tooba.Host.Messaging
no old Tooba.Host duplicates
no alias/shim/TypeForwardedTo
no unexpected subfolders/files
TYPE OWNERSHIP

Certify dispositions:

InProcessIntegrationEventPublisher
= EXPLICIT_TESTING_ONLY_HOST_DOUBLE_CERTIFIED

MassTransitIntegrationEventPublisher
= THIN_HOST_TRANSPORT_ADAPTER_CERTIFIED

MessagingDisabledPublisher
= GLOBAL_HOST_MESSAGING_PLATFORM_CERTIFIED

MessagingHostOptions
= GLOBAL_HOST_MESSAGING_PLATFORM_CERTIFIED

MessagingOptionsValidator
= GLOBAL_HOST_MESSAGING_PLATFORM_CERTIFIED

MessagingRegistration
= GLOBAL_HOST_MESSAGING_COMPOSITION_CERTIFIED

MessagingRetryConfigurator
= GLOBAL_HOST_MESSAGING_TRANSPORT_POLICY_CERTIFIED

PROGRAM REGISTRATION

Verify Program:

uses Tooba.Host.Messaging
AddOptions<MessagingHostOptions>()
binds Tooba:Messaging
ValidateOnStart()
IValidateOptions<MessagingHostOptions> singleton registration
AddToobaIntegrationPublisher(...)
no duplicate messaging registration path
publisher lifetimes preserved
FAIL-CLOSED TRANSPORT SELECTION

Certify exact decision tree:

A. UseInProcessTestDouble=true

only Testing accepted
non-Testing throws
no silent fallback
registers InProcessIntegrationEventPublisher

B. Enabled=true

MassTransit SQL Transport registration
MassTransitIntegrationEventPublisher registered

C. otherwise

MessagingDisabledPublisher registered
disabled publisher throws on publish
no event is silently dropped
TESTING-ONLY SERVICE-LOCATION EXCEPTION

Explicitly certify the narrow exception:

Allowed only in:
InProcessIntegrationEventPublisher

Allowed constructs only there:

IServiceProvider
GetServices(Type)
MakeGenericType
MethodInfo reflection Invoke

Required proof:

the type is internal
registration only happens after explicit Testing environment check
non-Testing path cannot select it
no other Messaging file uses runtime IServiceProvider
this does NOT establish a general architecture exemption

Record:
TESTING_ONLY_SERVICE_LOCATION_EXCEPTION_CERTIFIED

REFLECTION DISPATCH SEMANTICS

Certify/document:

handler interface constructed from runtime event type
all registered handlers invoked
DI registration order
zero handlers currently treated as successful no-op + metric increment
cancellation token forwarded
synchronous reflection throw behavior understood
test double does NOT prove transport/inbox/retry parity

Do NOT redesign.

COMPOSITION CALLBACK DI

Certify as allowed:

sp.GetRequiredService inside AddSingleton callback
context.GetRequiredService inside MassTransit/UsingPostgres callback

Classification:
FRAMEWORK_COMPOSITION_CALLBACK_ALLOWED

Do not treat as application service locator.

OPTIONS / VALIDATOR

Verify MessagingHostOptions exact fields/defaults:

Enabled
Transport
ConnectionReference
Schema
UseInProcessTestDouble
CanonicalTransport = PostgreSql

Verify validator:

Enabled + test double conflict rejected
disabled returns success
PostgreSql/PostgreSQL accepted
other transport rejected
ConnectionReference required when enabled
reserved/invalid schemas rejected
no duplicate Production branch
startup/operator prose only
no user-facing business presentation role
MASSTRANSIT TOPOLOGY

Certify:

SqlTransportOptions configured from resolver + MessagingHostOptions
PostgreSQL SQL Transport canonical
AddPostgresMigrationHostedService
CreateDatabase=false
CreateInfrastructure=true
MassTransitHostOptions WaitUntilStarted=true
StopTimeout=30 seconds
singleton NpgsqlDataSource
AddMassTransit
ToobaIntegrationTransportConsumer
endpoint tooba-integration
UsingPostgres
AutoStart=true
retry configured
ConfigureEndpoints

RabbitMQ runtime registration:
ZERO

RETRY POLICY

Certify exact:

Immediate(2)
intervals 5 / 15 / 30 seconds
no infinite retry
Messaging owns global consumer transport retry policy only
no module-specific business retry authority
MASSTRANSIT PUBLISHER BOUNDARY

Certify:

implements IIntegrationEventPublisher
IBus only
IIntegrationEventSerializer only
builds transport envelope
publishes via bus
no handler invocation
no business decision
no module dependency
no DB write
no repository
TRANSPORT ENVELOPE

Verify exact mapped fields:

EventType
Version
EventId
OccurredAt
TenantId
Edition
DeploymentId
CorrelationId
PayloadJson

No field silently dropped.

HEADER PROPAGATION

Verify headers:

tooba.event-type
tooba.tenant-id
tooba.edition
tooba.deployment-id
tooba.event-id
canonical correlation header
traceparent when present
tracestate when present

No payload in headers.
No connection refs.
No secrets.

OBSERVABILITY / CORRELATION

Certify canonical mechanisms only:

ToobaTelemetry
MessagingCorrelation
CorrelationIdContext
ToobaTraceEnricher

No parallel ActivitySource.
No parallel Meter instance.
No custom correlation framework.

Record metric semantics as accepted:

in-process = tooba.outbox.published
MassTransit = tooba.messaging.published

Do not rename in cert.

DISABLED PUBLISHER

Certify:

throws fail-closed
never silently succeeds
no fallback to in-process
runtime prose is internal/operator text
external canonical exception presentation remains generic for unknown exception
no message-text classification
HARD-CODED TEXT / LOCALIZATION

Allowed:

startup validation messages
operator/internal misconfiguration exception prose
telemetry names
endpoint name
protocol/transport literals
header names

Required user-facing runtime presentation text:
ZERO

MESSAGE CLASSIFICATION

Required ZERO:

ex.Message branching
exception.Message branching
Contains/StartsWith message mapping
exception text → semantic code inference
SENSITIVE DATA

Verify no logs/traces/public presentation expose:

connection string
connection reference
payload body
secret
auth token
SQL
credential

Metadata tags/headers may include:

tenant id
edition
deployment id
event id
correlation id
event type
PERSISTENCE/SERIALIZER BOUNDARY

Verify Messaging consumes neutral platform abstractions:

IIntegrationEventSerializer
IDatabaseConnectionResolver
ConnectionReference

No business persistence authority.

No module DbContext.
No cross-module SQL.
No repository.

TRANSPORT BOUNDARY

Verify Messaging → Tooba.Host.Transport dependency is limited to:

transport envelope
transport consumer registration
SQL transport mapper

No circular production dependency from Transport back into Messaging implementation types that creates ownership inversion.

Do not open Transport AMC.

OUTBOX BOUNDARY

Verify:

Outbox owns poll/delivery orchestration
Messaging owns publish adapter only
publisher exceptions propagate to Outbox
no duplicate polling
no duplicate delivery lifecycle

Do not modify Outbox.

HEALTH BOUNDARY

Verify Health:

depends only on MessagingHostOptions + IBusControl adjacent platform concerns
imports Tooba.Host.Messaging
behavior unchanged
HOST_HEALTH_AMC_CERTIFIED preserved
FOREIGN LAYER / BUSINESS AUTHORITY

Across all Messaging files required ZERO:

foreign module Application
foreign module Infrastructure
foreign module Domain
foreign module DbContext
business command
business policy
domain mutation
module workflow authority
ACTIVE CONSUMERS

Verify every Messaging type is live:

registration
publisher DI
options
validator
retry callback
test double path
disabled path

No dead production type.

TEST / GUARD CERTIFICATION

Create:
HostMessagingAmcCertGuardTests

Lock at minimum:

exact 7-file tree
exact namespace
one type per file
exact dispositions
Program registrations
fail-closed selection
Testing-only service-location confinement
framework callback DI allowed
exact options fields/defaults
exact validator semantics
exact retry policy
PostgreSQL SQL Transport canonical
endpoint name exact
no RabbitMQ registration
envelope/header mapping
canonical observability/correlation
no message classification
sensitive leakage zero
foreign App/Infra/Domain/DbContext zero
Health/Outbox boundary expectations
protected prior certifications in SoT

Do NOT weaken W1 guard.

FOCUSED TESTS

Run focused:

HostMessagingAmcW1GuardTests
HostMessagingAmcCertGuardTests
MassTransitFoundationTests
MassTransitPostgresTests
Outbox-focused publisher/dispatcher tests
HealthReadiness focused tests after Messaging namespace
options validator focused tests
Testing-only double gate tests
disabled publisher fail-closed test
TmarDurableGuard current pointer/recovery assertions
HostHealthAmcCertGuardTests

No solution-wide tests.

BUILD

Focused:

Tooba.Host
Tooba.Host.Tests

No solution-wide build.

PROTECTED CERTIFICATIONS

Must preserve:

HOST_HEALTH_AMC_CERTIFIED
HOST_MULTITENANCY_AMC_CERTIFIED
HOST_ERRORS_AMC_CERTIFIED
HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED

No production edits under those folders.

RECOVERY HYGIENE

Verify:

no stale historical "current" checkpoint reintroduced
no duplicate JSON property
no stale current pointer
no automatic next Host folder
implementation vs docs stamp lineage correct
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

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-MESSAGING-AMC-001-W2-CERT/

Required:

certification-summary.md
physical-tree.md
transport-selection.md
testing-double.md
options-retry.md
envelope-observability.md
boundary-contracts.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-MESSAGING-AMC-001-W2-CERT.task.md

RECOVERY / SOT

On PASS:

Top-level:

lastAcceptedTask = TB-TMAR-HOST-MESSAGING-AMC-001-W2-CERT
lastAcceptedCommit remains W1 implementation:
f29a881370b9a8813035715ef6973145ce3f1723
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-MESSAGING-AMC-001-W1
currentHostCheckpoint = Messaging
nextTask = USER_REVIEW_HOST_MESSAGING_AMC_001_W2_CERT
workflowStop = USER_REVIEW_HOST_MESSAGING_AMC_001_W2_CERT
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Add/update:
hostMessagingAmc001W2Cert

Required:

certificationState = HOST_MESSAGING_AMC_CERTIFIED
boundaryState = HOST_MESSAGING_PLATFORM_BOUNDARY_CERTIFIED
productionFileCount = 7
productionTypeCount = 7
pathNamespace = EXACT_Tooba.Host.Messaging
fileCohesion = ONE_TOP_LEVEL_TYPE_PER_FILE_CERTIFIED
inProcessPublisher = EXPLICIT_TESTING_ONLY_HOST_DOUBLE_CERTIFIED
inProcessServiceLocator = TESTING_ONLY_EXCEPTION_CERTIFIED
reflectionDispatch = TESTING_ONLY_CERTIFIED
compositionCallbackServiceProvider = FRAMEWORK_COMPOSITION_CALLBACK_ALLOWED
massTransitPublisher = THIN_HOST_TRANSPORT_ADAPTER_CERTIFIED
disabledPublisher = FAIL_CLOSED_CERTIFIED
options = HOST_DEPLOYMENT_OPTIONS_CERTIFIED
validator = STARTUP_OPERATOR_VALIDATION_CERTIFIED
transportSelection = FAIL_CLOSED_CERTIFIED
retryPolicy = IMMEDIATE2_PLUS_5_15_30_CERTIFIED
sqlTransport = POSTGRESQL_SQL_TRANSPORT_CERTIFIED
rabbitMq = ZERO
observabilityCorrelation = CANONICAL_CERTIFIED
messageClassification = ZERO
sensitiveData = ZERO
foreignApplication = ZERO
foreignInfrastructure = ZERO
foreignDomain = ZERO
foreignDbContext = ZERO
businessAuthority = ZERO
productionRepairRequired = false
implementationCommit = f29a8813...
hostHealthCertification = HOST_HEALTH_AMC_CERTIFIED_PRESERVED
hostMultiTenancyCertification = HOST_MULTITENANCY_AMC_CERTIFIED_PRESERVED
hostErrorsCertification = HOST_ERRORS_AMC_CERTIFIED_PRESERVED
hostSecurityCertification = HOST_SECURITY_AMC_CERTIFIED_PRESERVED
hostAdminCertification = HOST_ADMIN_FULLY_CERTIFIED_PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_MESSAGING_AMC_001_W2_CERT

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

exact 7-file tree
exact 7 top-level types
exact namespace
one type per file
all seven ownership dispositions certified
Program registration exact
fail-closed transport selection certified
Testing-only IServiceProvider/reflection exception strictly confined and gated
framework composition callbacks classified allowed
options/validator exact
retry exact
PostgreSQL SQL Transport exact
RabbitMQ registration zero
envelope/header mapping exact
canonical observability/correlation exact
disabled publisher fail-closed
user-facing runtime text zero
message classification zero
sensitive leakage zero
persistence/business authority zero
foreign App/Infra/Domain/DbContext zero
every type active
Health/Outbox boundaries preserved
Health/MultiTenancy/Errors/Security/Admin certs preserved
production repair NONE
production change ZERO
focused builds/tests PASS
durable cert guard PASS
recovery hygiene exact
implementation SHA remains f29a8813...
automatic next NONE
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-MESSAGING-AMC-001-W2-CERT
Parent-Task: TB-TMAR-HOST-MESSAGING-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Certification-State:
Boundary-Certification-State:
Messaging-Production-File-Count:
Messaging-Production-Type-Count:
Exact-Tree-State:
Path-Namespace-State:
File-Cohesion-State:
InProcessIntegrationEventPublisher-State:
InProcess-ServiceLocator-State:
Reflection-Dispatch-State:
Testing-Only-Gate-State:
Composition-Callback-ServiceProvider-State:
MassTransitIntegrationEventPublisher-State:
MessagingDisabledPublisher-State:
MessagingHostOptions-State:
MessagingOptionsValidator-State:
MessagingRegistration-State:
MessagingRetryConfigurator-State:
Transport-Selection-State:
Options-Validation-State:
Retry-Policy-State:
SqlTransport-State:
RabbitMq-State:
MassTransit-Topology-State:
Transport-Envelope-State:
Header-Propagation-State:
Observability-Correlation-State:
Metric-Semantics-State:
Hardcoded-User-Facing-Text-State:
Exception-Message-Classification-State:
Sensitive-Data-State:
Persistence-Serializer-Boundary-State:
Transport-Boundary-State:
Outbox-Boundary-State:
Health-Boundary-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
Active-Consumer-State:
Durable-Cert-Guard-State:
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
Do not start Transport/Persistence/Outbox AMC.
Do not modify production code in Cert.
Do not open module recovery.

Wait for Architect/user review.

END_TOOBA_TASK