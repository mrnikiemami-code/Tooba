PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-MESSAGING-AMC-001-W1
Parent-Task: TB-TMAR-HOST-MESSAGING-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_MESSAGING_STRUCTURE_NAMESPACE_COHESION
Title: Align Host/Messaging namespace, split options validator, and lock Testing-only service-location exception without transport redesign
Estimated-Time-Minutes: 14
Hard-Timebox-Minutes: 18

ARCHITECT REVIEW STATE

Parent Analyze:
ACCEPTED

Architect independently verified:

Host/Messaging exact production tree = 6 files
top-level production type count = 7
all seven types are live or intentionally active
no dead Messaging production type identified
Host/Messaging is the correct process-level composition root
MassTransit publisher is a thin Host transport adapter
disabled publisher is fail-closed
options + validator are Host deployment/config platform responsibility
retry configurator is Host transport policy
path↔namespace is a real violation
MessagingHostOptions.cs has two top-level types and should split
IServiceProvider in InProcessIntegrationEventPublisher is reachable only through the explicit Testing-only double
DI callback GetRequiredService inside registration/MassTransit composition is framework composition and is NOT the same as runtime service locator
Health/Outbox boundaries are currently valid
Health/MultiTenancy/Errors/Security/Admin certifications remain protected
implementation authority remains:
ba8db8c6bcb22f0ad4c386073d3b612e3d318e00

ARCHITECT DECISIONS FOR W1

Do NOT redesign transport topology.
Do NOT move Messaging files into Transport/Persistence/BuildingBlocks.
Do NOT replace the Testing-only in-process double with a new registry abstraction in this Host wave.
The in-process IServiceProvider/reflection path is an explicit TESTING-ONLY exception, not a production runtime exception.
Certification later must lock that this exception cannot be selected outside Testing.
Framework DI composition callbacks may continue using GetRequiredService.
Split MessagingOptionsValidator into its own file.
Fix namespace/path exactly.
Remove the redundant Production-specific ConnectionReference validation branch if and only if exact semantic equivalence is proven; otherwise leave it and document as redundant. Do not expand scope.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

MIGRATE ONLY.

Do NOT certify Messaging in W1.
Do NOT open Transport AMC.
Do NOT open Persistence AMC.
Do NOT open Outbox AMC.
Do NOT start another Host folder.
Do NOT open module recovery.
Do NOT reopen Health/MultiTenancy/Errors/Security/Admin.

PRIMARY ACTIVE FOLDER

src/backend/Host/Tooba.Host/Messaging/

CURRENT FILES

InProcessIntegrationEventPublisher.cs
MassTransitIntegrationEventPublisher.cs
MessagingDisabledPublisher.cs
MessagingHostOptions.cs
MessagingRegistration.cs
MessagingRetryConfigurator.cs

CURRENT TOP-LEVEL TYPES

InProcessIntegrationEventPublisher
MassTransitIntegrationEventPublisher
MessagingDisabledPublisher
MessagingHostOptions
MessagingOptionsValidator
MessagingRegistration
MessagingRetryConfigurator

TARGET PHYSICAL SHAPE

After W1 exactly seven production files:

Messaging/

InProcessIntegrationEventPublisher.cs
MassTransitIntegrationEventPublisher.cs
MessagingDisabledPublisher.cs
MessagingHostOptions.cs
MessagingOptionsValidator.cs
MessagingRegistration.cs
MessagingRetryConfigurator.cs

Exact top-level production type count:
7

One top-level type per file.

TARGET NAMESPACE

All seven types:
Tooba.Host.Messaging

No alias.
No shim.
No TypeForwardedTo.
No duplicate legacy namespace copy.

REQUIRED W1 CHANGES

PATH ↔ NAMESPACE

Change all Messaging production files from:

namespace Tooba.Host;

to:

namespace Tooba.Host.Messaging;

Update consumers/usings only as required.

Expected consumers include:

Program
Health
tests
any Outbox/Transport-adjacent Host files that reference Messaging types
OPTIONS / VALIDATOR SPLIT

Keep only:
MessagingHostOptions

in:
MessagingHostOptions.cs

Move:
MessagingOptionsValidator

to:
MessagingOptionsValidator.cs

Both:
namespace Tooba.Host.Messaging

Preserve behavior exactly.

PROGRAM USINGS / REGISTRATION

Update Program with:
using Tooba.Host.Messaging;

Preserve:

AddOptions<MessagingHostOptions>()
section Tooba:Messaging
ValidateOnStart()
singleton IValidateOptions<MessagingHostOptions>
AddToobaIntegrationPublisher(...)
all current publisher registration lifetimes

No lifetime change.

HEALTH COMPATIBILITY

Host/Health currently consumes:
MessagingHostOptions

Update using to:
Tooba.Host.Messaging

No Health production behavior changes.

Health certifications must remain:

HOST_HEALTH_AMC_CERTIFIED
HOST_HEALTH_PLATFORM_BOUNDARY_CERTIFIED

Do not edit Health behavior.

FAIL-CLOSED TRANSPORT SELECTION

Preserve exact selection:

A. UseInProcessTestDouble = true

only Testing allowed
non-Testing throws immediately
registers InProcessIntegrationEventPublisher

B. messaging Enabled = true

registers MassTransitIntegrationEventPublisher
MassTransit SQL Transport configured

C. otherwise

registers MessagingDisabledPublisher

No silent fallback.
No silent event drop.

TESTING-ONLY IN-PROCESS DOUBLE

Preserve InProcessIntegrationEventPublisher as:
KEEP_AS_EXPLICIT_TEST_ONLY_HOST_DOUBLE

Its use of:

IServiceProvider
GetServices(Type)
MakeGenericType
reflection Invoke

is allowed ONLY because:

registration is explicitly Testing-gated
non-Testing selection throws before registration
it is not a production publisher path

W1 MUST add durable guard proving:

UseInProcessTestDouble requires Testing
no production/environment-neutral registration path can select it
the type remains internal
the service-provider/reflection usage exists only in this specific file/family

Do NOT normalize this into a general service locator exception.

COMPOSITION CALLBACK DI

Preserve framework composition callback usage in MessagingRegistration:

sp.GetRequiredService inside AddSingleton callback
MassTransit context.GetRequiredService inside UsingPostgres setup

Classify as:
FRAMEWORK_COMPOSITION_CALLBACK_ALLOWED

Do NOT replace with custom factories merely to remove these calls.

MASSTRANSIT PUBLISHER

Preserve:

IIntegrationEventPublisher adapter
IBus injection
IIntegrationEventSerializer injection
transport envelope fields
headers
traceparent/tracestate
correlation logic
ToobaTelemetry
no business logic

No header/value rename.

IN-PROCESS PUBLISHER OBSERVABILITY

Preserve:

correlation resolution
CorrelationIdContext scope
ToobaTelemetry activity
event/tenant/edition tags
published counter
handler dispatch behavior

No metric rename in W1.

DISABLED PUBLISHER

Preserve fail-closed throw behavior.

Do NOT replace with no-op.
Do NOT swallow.

Current exception prose is operator/internal runtime text.
Because it is an unknown InvalidOperationException, canonical external presentation must remain generic/safe.

No message-text classification.

MESSAGING OPTIONS

Preserve fields/defaults:

Enabled
Transport
ConnectionReference
Schema
UseInProcessTestDouble
CanonicalTransport = PostgreSql

No configuration key rename.
No schema/transport behavior change.

OPTIONS VALIDATOR

Preserve:

Enabled + test-double conflict rejection
disabled => success
only PostgreSql/PostgreSQL accepted
RabbitMQ/AMQP rejected
ConnectionReference required when Enabled
dedicated schema requirement
testing gate elsewhere in AddToobaIntegrationPublisher

Optional bounded cleanup:
The later Production-specific empty ConnectionReference check is redundant because the earlier Enabled check already rejects empty ConnectionReference.

You MAY remove only that duplicate branch if:

focused tests prove exact behavior remains
no environment-specific error assertion depends on its message

Otherwise preserve and document.

No other validator redesign.

RETRY CONFIGURATOR

Preserve exactly:

Immediate(2)
intervals 5s / 15s / 30s

No infinite retry.
No policy change.
No poison/dead-letter redesign.

SQL TRANSPORT REGISTRATION

Preserve:

SqlTransportOptions
IDatabaseConnectionResolver
schema mapping
AddPostgresMigrationHostedService
CreateDatabase=false
CreateInfrastructure=true
MassTransitHostOptions WaitUntilStarted
StopTimeout 30 seconds
singleton NpgsqlDataSource
AddMassTransit
AddConsumer<ToobaIntegrationTransportConsumer>
endpoint name tooba-integration
UsingPostgres
AutoStart=true
UseMessageRetry(MessagingRetryConfigurator.ApplyConsumerRetry)
ConfigureEndpoints

No Transport/Persistence move in W1.

DIRECT NPGSQL / MASSTRANSIT

Allowed in Host/Messaging as platform transport composition.

Required ZERO:

module DbContext
business repository
module Application
module Infrastructure
module Domain
TRANSPORT BOUNDARY

Preserve dependency on:
Tooba.Host.Transport

Messaging may create/publish the transport envelope.

Do NOT move:

ToobaIntegrationTransportMessage
ToobaIntegrationTransportConsumer
SqlTransportOptionsMapper

Transport AMC is not opened.

PERSISTENCE/SERIALIZER BOUNDARY

Preserve use of neutral:

IIntegrationEventSerializer
IDatabaseConnectionResolver
ConnectionReference

No Persistence folder redesign.
No serializer move.

OUTBOX BOUNDARY

Messaging publisher remains publication adapter only.

Do not modify Outbox production code.

Outbox continues to own:

polling
orchestration
retry/delivery lifecycle
HEALTH BOUNDARY

Do not modify Health semantics.

Health may consume:

MessagingHostOptions
IBusControl

No Health dependency on publisher internals.

OBSERVABILITY / CORRELATION

Required canonical only:

ToobaTelemetry
MessagingCorrelation
CorrelationIdContext
ToobaTraceEnricher

Do not introduce:

new ActivitySource
new Meter
parallel correlation IDs
custom tracing foundation
HEADER SENSITIVITY

Preserve metadata headers only:

event type
tenant id
edition
deployment id
event id
correlation id
traceparent
tracestate

No payload in headers.
No connection refs.
No secrets.

HARD-CODED TEXT

Classify and preserve only allowed internal/operator values:

endpoint/protocol names
startup validation messages
internal fail-closed exception prose
telemetry names/headers

User-facing business/runtime text:
ZERO

Do not introduce Persian/English user-facing presentation strings.

EXCEPTION MESSAGE CLASSIFICATION

Required ZERO:

ex.Message branching
exception.Message branching
Message.Contains
Message.StartsWith
message-based fault mapping
FILE COHESION

After W1:

one top-level type per file
no mixed options+validator file
no new generic MessagingHelpers file
no God file
DEPENDENCY BOUNDARY

Across Host/Messaging required ZERO:

foreign module Application
foreign module Infrastructure
foreign module Domain
foreign DbContext
cross-module persistence
business command
module business workflow authority
W1 GUARD

Add:
HostMessagingAmcW1GuardTests

Lock at minimum:

exact seven-file tree
exact seven top-level types
exact namespace Tooba.Host.Messaging
one top-level type per file
Program using/registration updated
Health uses Messaging namespace successfully
InProcess IServiceProvider/reflection limited to the explicit Testing-only file
test-double non-Testing rejection remains
framework composition callback GetRequiredService is not blanket-forbidden
fail-closed transport selection
disabled publisher remains throwing
exact retry policy
exact endpoint name
SQL Transport/PostgreSQL remains canonical
RabbitMQ absent from registration
foreign App/Infra/Domain/DbContext zero
message-text classification zero
protected certifications present in SoT
FOCUSED TESTS

Identify and run focused existing tests for:

MessagingOptionsValidator
Messaging registration
Testing-only in-process double
non-Testing test-double rejection
disabled publisher
MassTransit publisher envelope/header/correlation
retry configurator
transport registration/no RabbitMQ
Outbox publisher interaction
Health readiness compile/behavior after namespace change

Add focused tests where missing for:

exact Testing-only gate
options validator after split
disabled fail-closed publisher
namespace/path guard

No solution-wide tests.

BUILD

Focused:

Tooba.Host
Tooba.Host.Tests

If Transport/Persistence test projects are required only for compile proof, run focused project only.

No solution-wide build.

PROTECTED CERTIFICATIONS

Must preserve:

HOST_HEALTH_AMC_CERTIFIED
HOST_MULTITENANCY_AMC_CERTIFIED
HOST_ERRORS_AMC_CERTIFIED
HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED

No production edits under those folders except namespace import repair in Health if strictly required.

RECOVERY HYGIENE

Do not reintroduce stale historical current markers.

Authoritative resume rule remains:

top authoritative checkpoint section
tmar-current-state.json
HOST-ONLY SCOPE LOCK

User explicitly requested:
FINISH HOST ONLY.

No module production migration.
No Catalog/Fulfillment/Checkout work.

PRODUCTION FILE COUNT

Before:
6 files / 7 top-level types

After:
7 files / 7 top-level types

This is structural cohesion repair only.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-MESSAGING-AMC-001-W1/

Required:

structure-namespace.md
options-validator-split.md
test-double-gate.md
transport-selection-parity.md
boundary-observability.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-MESSAGING-AMC-001-W1.task.md

RECOVERY / SOT

On PASS:

lastAcceptedTask = TB-TMAR-HOST-MESSAGING-AMC-001-W1
lastAcceptedCommit = actual W1 implementation SHA
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-MESSAGING-AMC-001-W1
currentHostCheckpoint = Messaging

Record:
messagingProductionFileCount = 7
messagingProductionTypeCount = 7
pathNamespace = EXACT_Tooba.Host.Messaging
fileCohesion = ONE_TOP_LEVEL_TYPE_PER_FILE
inProcessPublisher = KEEP_EXPLICIT_TESTING_ONLY_DOUBLE
inProcessServiceLocator = TESTING_ONLY_EXCEPTION_GUARDED
compositionCallbackServiceProvider = FRAMEWORK_COMPOSITION_ALLOWED
massTransitPublisher = THIN_HOST_TRANSPORT_ADAPTER
disabledPublisher = FAIL_CLOSED_PRESERVED
transportSelection = FAIL_CLOSED_CERT_PENDING
retryPolicy = PRESERVED_EXACT
transportOwnership = HOST_MESSAGING_COMPOSITION_PRESERVED
foreignApplication = ZERO
foreignInfrastructure = ZERO
foreignDomain = ZERO
foreignDbContext = ZERO
businessAuthority = ZERO
certificationState = NOT_CERTIFIED_W2_REQUIRED

hostHealthCertification = HOST_HEALTH_AMC_CERTIFIED_PRESERVED
hostMultiTenancyCertification = HOST_MULTITENANCY_AMC_CERTIFIED_PRESERVED
hostErrorsCertification = HOST_ERRORS_AMC_CERTIFIED_PRESERVED
hostSecurityCertification = HOST_SECURITY_AMC_CERTIFIED_PRESERVED
hostAdminCertification = HOST_ADMIN_FULLY_CERTIFIED_PRESERVED

automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_MESSAGING_AMC_001_W1
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

Push main only on PASS.

SUCCESS CRITERIA

PASS only if:

exact seven-file Messaging tree
exact seven top-level types
exact namespace Tooba.Host.Messaging
one top-level type per file
Program/Health imports repaired
transport selection behavior preserved
in-process double remains Testing-only
non-Testing test-double selection still fails closed
IServiceProvider/reflection remains confined to explicit test-double file only
framework composition callback DI preserved/accepted
options semantics preserved
validator semantics preserved
exact retry policy preserved
MassTransit/PostgreSQL topology preserved
disabled publisher fail-closed preserved
no message-text classification
canonical observability/correlation preserved
sensitive data leakage zero
foreign App/Infra/Domain/DbContext zero
business authority zero
no module production edits
Health/MultiTenancy/Errors/Security/Admin certs preserved
focused builds/tests PASS
W1 guard PASS
certification remains pending W2
implementation SHA advanced to actual W1 commit
automatic next NONE
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-MESSAGING-AMC-001-W1
Parent-Task: TB-TMAR-HOST-MESSAGING-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Migration-State:
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
Transport-Selection-Parity-State:
Options-Validation-Parity-State:
Retry-Policy-Parity-State:
MassTransit-Topology-State:
Transport-Envelope-Boundary-State:
Persistence-Serializer-Boundary-State:
Outbox-Boundary-State:
Health-Boundary-State:
Observability-Correlation-State:
Metric-Semantics-State:
Header-Sensitivity-State:
Hardcoded-User-Facing-Text-State:
Exception-Message-Classification-State:
Sensitive-Data-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
W1-Guard-State:
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
Do not open Transport/Persistence/Outbox AMC.
Do not open module recovery.

Wait for Architect/user review.

END_TOOBA_TASK