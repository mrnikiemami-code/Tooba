PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-OUTBOX-AMC-001-W1
Parent-Task: TB-TMAR-HOST-OUTBOX-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_OUTBOX_STRUCTURE_CANCELLATION_OPTIONS
Title: Align Host/Outbox namespace and file cohesion, preserve worker scope model, fix cancellation safety, and add fail-fast options validation
Estimated-Time-Minutes: 15
Hard-Timebox-Minutes: 19

ARCHITECT REVIEW STATE

Parent Analyze:
ACCEPTED

Architect independently verified:

Host/Outbox current production tree = 3 files / 6 top-level types
all six current types have legitimate Host platform/worker ownership
path↔namespace violation is real (Tooba.Host vs Tooba.Host.Outbox)
OutboxDispatcher.cs contains 2 top-level types and OutboxWorkerSeams.cs contains 3; split is required
target/module/message broad catches are intentional worker isolation, BUT cancellation is currently unsafe because OperationCanceledException may be swallowed or converted to retry/dead-letter
per-message IServiceScopeFactory + scoped GetRequiredService is legitimate worker-scope composition, not generic application service location
Outbox options have no validator and invalid zero/negative values can flow to store/runtime
retry/dead-letter attempt semantics are consistent with current claim increment model
Persistence/Messaging/StoreContext boundaries are clean
no module App/Infra/Domain/DbContext/business authority
latest accepted implementation remains Observability W1:
f1425fed94cc1a8354d3c9f9a013065d87cbe66c

ARCHITECT DECISIONS FOR W1

Keep all existing six Outbox responsibilities in Host/Outbox.
Target namespace = Tooba.Host.Outbox.
Split to one top-level type per file.
Add OutboxHostOptionsValidator as the seventh type/file.
Keep per-message scope activation exactly; do NOT replace with a new WorkerScopeRunner abstraction.
Keep broad exception isolation for non-cancellation worker faults.
Add explicit cancellation escape before broad catch handling anywhere the task token is canceled.
Cancellation must NEVER become retry/dead-letter.
Add fail-fast startup validation for all positive integer Outbox options when relevant.
Remove the Persian internal operator exception prose in FromPollTarget; use stable English operator text only. No localization ceremony.
No retry formula/topology redesign.
No Persistence/Messaging/StoreContext moves.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

MIGRATE ONLY.

Do NOT certify Outbox in W1.
Do NOT start Persistence/Configuration.
Do NOT start another Host folder.
Do NOT open module recovery.
Do NOT reopen previously certified Host folders.

PRIMARY ACTIVE FOLDER

src/backend/Host/Tooba.Host/Outbox/

CURRENT TYPES

OutboxDispatcher
OutboxDispatcherHostedService
OutboxHostOptions
ConfiguredOutboxPollTargetSource
WorkerCommerceContextFactory
WorkerStoreCommerceContextFactory

TARGET TREE

Exactly seven production files:

Outbox/

OutboxDispatcher.cs
OutboxDispatcherHostedService.cs
OutboxHostOptions.cs
OutboxHostOptionsValidator.cs
ConfiguredOutboxPollTargetSource.cs
WorkerCommerceContextFactory.cs
WorkerStoreCommerceContextFactory.cs

Exactly seven top-level production types:
the six existing types + OutboxHostOptionsValidator

TARGET NAMESPACE

All seven:
Tooba.Host.Outbox

No alias.
No shim.
No TypeForwardedTo.
No duplicate old Tooba.Host type copy.

REQUIRED W1 CHANGES

NAMESPACE

Move all retained Outbox types to:
namespace Tooba.Host.Outbox;

Update consumers/usings only as required.

Expected:

Program
Host tests
any Host worker consumers
FILE COHESION

Split OutboxDispatcherHostedService out of OutboxDispatcher.cs.

Split OutboxWorkerSeams.cs into:

ConfiguredOutboxPollTargetSource.cs
WorkerCommerceContextFactory.cs
WorkerStoreCommerceContextFactory.cs

Delete old mixed file after repointing.

One top-level type per file.

OPTIONS VALIDATOR

Create:
OutboxHostOptionsValidator.cs

Implement:
IValidateOptions<OutboxHostOptions>

Fail startup validation when any of these are <= 0:

PollIntervalSeconds
BatchSize
RetryBaseDelaySeconds
MaxAttempts
LockSeconds

Enabled=false:
still validate structural numeric values unless there is a strong existing repository convention proving disabled options may be invalid; prefer consistent fail-fast configuration truth.

Validation errors are startup/operator prose, not user-facing runtime text.

PROGRAM OPTIONS REGISTRATION

Replace plain Configure<OutboxHostOptions> with canonical options registration that preserves binding and adds:

ValidateOnStart()
IValidateOptions<OutboxHostOptions> registration

Use the same Host pattern already established by Messaging/Cache.

Do not rename section:
Tooba:Outbox

CANCELLATION SAFETY — TARGET LOOP

In target-level catch around DispatchTargetAsync:

Before general catch handling, ensure:

OperationCanceledException when cancellationToken.IsCancellationRequested
must rethrow/escape.

Do not log as tenant failure.
Do not continue to next target during requested shutdown.

CANCELLATION SAFETY — MODULE CLAIM

In module claim path:

OperationCanceledException when cancellation token requested
must rethrow.

Do not:

increment TenantFailures
log claim warning
continue to another module
CANCELLATION SAFETY — MESSAGE PROCESSING

In per-message processing:

OperationCanceledException when cancellation token requested
must rethrow BEFORE:

OutboxErrorSanitizer
dead-letter decision
retry decision
DeadLetters metric
Retries metric
MarkDeadLetterAsync
MarkRetryAsync

Critical invariant:
REQUESTED CANCELLATION MUST NEVER ALTER OUTBOX MESSAGE FAILURE STATE.

HOSTED SERVICE

Preserve existing explicit OCE handling:

DispatchOnceAsync OCE with stoppingToken requested => break
Task.Delay OCE with stoppingToken requested => break

No behavior redesign.

NON-CANCELLATION FAILURE ISOLATION

Preserve:

one target failure does not kill other targets
one module claim failure does not kill other modules
one message processing failure is retry/dead-lettered and next message can proceed
loop-level unexpected worker error logged by ErrorType and continues

No blanket removal of broad catches.

RETRY / DEAD-LETTER

Preserve exact existing semantics:

attempt count decision as currently implemented
MaxAttempts threshold
RetryBaseDelaySeconds
exponential shift cap at 8
NodaTime SystemClock
MarkRetry / MarkDeadLetter contracts

No backoff redesign.
No jitter addition.
No retry count change.

ERROR SANITIZATION

Preserve:
OutboxErrorSanitizer.Sanitize(ex)

No raw exception.Message persistence.
No stack trace persistence.
No message-based classification.

PER-MESSAGE WORKER SCOPE

Preserve:

IServiceScopeFactory
CreateAsyncScope()
scope.ServiceProvider.GetRequiredService<ICommerceContextAssigner>()
scope.ServiceProvider.GetRequiredService<IStoreCommerceContextAssigner>()
scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>()

This is explicitly allowed worker-scope composition.

Do NOT replace with singleton injection of scoped services.
Do NOT create a new generic service-locator exception.

WORKER COMMERCE CONTEXT TRUST

Preserve:

Marketplace uses registry deployment connection reference
SingleStore message TenantId must resolve in registry
tenant must be Active
connection reference comes from registry record, not message
edition/deployment come from registry authority
HTTP Host/header not used
FROMPOLL TARGET OPERATOR TEXT

Replace the Persian exception:
زمینهٔ کارگر انقضای سبد از registry بازسازی نشد.

with a stable English internal/operator message such as:
Worker commerce context could not be reconstructed from registry.

Do not introduce localization resources for internal worker exceptions.
Do not classify by exception message.

STORE COMMERCE FACTORY

Preserve:

Marketplace -> DeploymentStoreCommerce
SingleStore -> active tenant record.StoreCommerce
no defaults/normalization
Contracts-only StoreContext boundary
POLL TARGET SOURCE

Preserve:

Marketplace = one deployment target
SingleStore = ACTIVE tenants only
other/unconfigured edition = empty
connection refs from registry

No Health all-configured parity change; these semantics intentionally differ.

METADATA CORRELATION COMPATIBILITY

Preserve current reflection seam:

deserialize integration event
read Metadata property
if correlation absent, set copy with durable correlation

No serializer redesign in W1.

PERSISTENCE BOUNDARY

Preserve use of:

IOutboxDispatcherStore
IOutboxModuleRegistration
IIntegrationEventSerializer
IDatabaseConnectionResolver
OutboxMessage
OutboxPollTarget
OutboxErrorSanitizer

No direct DbContext.
No module repository.
No direct module table code in Host.

SQL IDENTIFIER SAFETY

Do not change store/schema contract.
Host continues passing trusted module schema/table metadata.
Persistence store remains responsible for quoting.

MESSAGING BOUNDARY

Preserve:
IIntegrationEventPublisher only

Required ZERO in Host/Outbox:

MassTransit
IBus
transport envelope construction

Messaging CERT must remain preserved.

OBSERVABILITY

Preserve canonical:

ToobaTelemetry
MessagingCorrelation
CorrelationIdContext
ToobaTraceEnricher

Preserve counters:

tenant_failures
retries
dead_letters
processed

No new Meter/ActivitySource.

LOGGING SENSITIVITY

Preserve structured fields only:

TenantId
EventType
Schema
ErrorType

Required ZERO:

payload
connection string
connection reference
exception.Message
stack trace
secrets
HARD-CODED RUNTIME TEXT

Allowed:

internal/operator worker log messages
internal fail-closed exception prose

User-facing runtime presentation:
ZERO

MESSAGE CLASSIFICATION

Required ZERO:

ex.Message
exception.Message
Message.Contains
Message.StartsWith
prose → status/code mapping
FOREIGN LAYER / BUSINESS AUTHORITY

Required ZERO:

module Application
module Infrastructure
module Domain
module DbContext
business command
business policy
domain mutation
W1 GUARD

Add:
HostOutboxAmcW1GuardTests

Lock at minimum:

exact 7-file tree
exact 7 top-level types
one type per file
exact namespace Tooba.Host.Outbox
Program uses new namespace
options binding + ValidateOnStart + validator registration
each positive integer validator rule
requested OCE escape exists at target/module/message handling
message cancellation cannot reach sanitizer/retry/dead-letter path
per-message IServiceScopeFactory pattern allowed/preserved
no MassTransit in Outbox
StoreContext Contracts-only
Persistence neutral seams only
exact retry formula/topology preserved
no message-text classification
no foreign layers/DbContext
protected certifications preserved in SoT
FOCUSED BEHAVIOR TESTS

Add/update tests for at least:

A. options validator:

valid defaults PASS
each <=0 relevant option FAIL

B. cancellation:

target-level requested OCE propagates
claim requested OCE propagates
publisher/message requested OCE propagates
requested OCE does NOT call MarkRetry
requested OCE does NOT call MarkDeadLetter

C. non-cancellation:

normal message failure still retries
max-attempt failure still dead-letters
claim non-cancellation failure still isolates module
target non-cancellation failure still isolates target

D. trust:

inactive/missing tenant fails closed
registry connection authority preserved where focused tests already exist

No solution-wide tests.

BUILD

Focused:

Tooba.Host
Tooba.Host.Tests

No solution-wide build.

PROTECTED CERTIFICATIONS

Must preserve:

HOST_OBSERVABILITY_AMC_CERTIFIED
HOST_MESSAGING_AMC_CERTIFIED
HOST_HEALTH_AMC_CERTIFIED
HOST_MULTITENANCY_AMC_CERTIFIED
HOST_ERRORS_AMC_CERTIFIED
HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED

No production edits under those folders.

HOST-ONLY SCOPE

No module production changes.
No Persistence/Configuration AMC.
No frontend.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-OUTBOX-AMC-001-W1/

Required:

structure-namespace.md
cancellation-safety.md
options-validation.md
worker-scope-trust.md
retry-deadletter-parity.md
boundary-observability.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-OUTBOX-AMC-001-W1.task.md

RECOVERY / SOT

On PASS:

lastAcceptedTask = TB-TMAR-HOST-OUTBOX-AMC-001-W1
lastAcceptedCommit = actual W1 implementation SHA
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-OUTBOX-AMC-001-W1
currentHostCheckpoint = Outbox

Record:
outboxProductionFileCount = 7
outboxProductionTypeCount = 7
pathNamespace = EXACT_Tooba.Host.Outbox
fileCohesion = ONE_TOP_LEVEL_TYPE_PER_FILE
dispatcher = KEEP_GLOBAL_HOST_OUTBOX_PLATFORM
hostedService = KEEP_GLOBAL_HOST_BACKGROUND_WORKER
pollTargetSource = KEEP_THIN_HOST_WORKER_CONTEXT_ADAPTER
workerCommerceFactory = KEEP_THIN_HOST_WORKER_CONTEXT_ADAPTER
workerStoreCommerceFactory = KEEP_THIN_HOST_WORKER_CONTEXT_ADAPTER
workerScopeActivation = LEGITIMATE_PER_MESSAGE_SCOPE_PRESERVED
cancellationSafety = REQUESTED_OCE_PROPAGATES_NO_RETRY_DEADLETTER
broadExceptionPolicy = NON_CANCELLATION_WORKER_ISOLATION_PRESERVED
retryDeadLetter = PRESERVED
optionsValidator = FAIL_FAST_PRESENT
hardcodedRuntimeText = INTERNAL_OPERATOR_ENGLISH
foreignApplication = ZERO
foreignInfrastructure = ZERO
foreignDomain = ZERO
foreignDbContext = ZERO
businessAuthority = ZERO
certificationState = NOT_CERTIFIED_W2_REQUIRED

hostObservabilityCertification = HOST_OBSERVABILITY_AMC_CERTIFIED_PRESERVED
hostMessagingCertification = HOST_MESSAGING_AMC_CERTIFIED_PRESERVED
hostHealthCertification = HOST_HEALTH_AMC_CERTIFIED_PRESERVED
hostMultiTenancyCertification = HOST_MULTITENANCY_AMC_CERTIFIED_PRESERVED
hostErrorsCertification = HOST_ERRORS_AMC_CERTIFIED_PRESERVED
hostSecurityCertification = HOST_SECURITY_AMC_CERTIFIED_PRESERVED
hostAdminCertification = HOST_ADMIN_FULLY_CERTIFIED_PRESERVED

automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_OUTBOX_AMC_001_W1
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

exact 7-file/7-type Outbox shape
exact namespace Tooba.Host.Outbox
one top-level type per file
Program imports/registrations repaired
Outbox options validate on startup
all five positive numeric settings guarded
requested cancellation propagates from target/claim/message paths
requested cancellation never becomes retry/dead-letter
non-cancellation failure isolation preserved
retry/dead-letter semantics preserved
per-message scope activation preserved
worker context anti-spoof semantics preserved
StoreContext Contracts-only preserved
Messaging boundary remains IIntegrationEventPublisher only
Persistence boundary remains neutral seams only
canonical observability preserved
no sensitive data leakage
no message-text classification
Persian worker exception prose removed
foreign App/Infra/Domain/DbContext zero
business authority zero
focused builds/tests PASS
W1 guard PASS
protected certs preserved
certification remains pending W2
implementation SHA advances to actual W1
automatic next NONE
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-OUTBOX-AMC-001-W1
Parent-Task: TB-TMAR-HOST-OUTBOX-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Migration-State:
Outbox-Production-File-Count:
Outbox-Production-Type-Count:
Exact-Tree-State:
Path-Namespace-State:
File-Cohesion-State:
OutboxDispatcher-State:
OutboxDispatcherHostedService-State:
OutboxHostOptions-State:
OutboxHostOptionsValidator-State:
ConfiguredOutboxPollTargetSource-State:
WorkerCommerceContextFactory-State:
WorkerStoreCommerceContextFactory-State:
Program-DI-State:
Options-Validation-State:
Target-Cancellation-State:
Claim-Cancellation-State:
Message-Cancellation-State:
Cancellation-RetryDeadLetter-State:
Broad-Exception-Policy-State:
Target-Failure-Isolation-State:
Module-Failure-Isolation-State:
Message-Failure-Isolation-State:
Retry-DeadLetter-Parity-State:
Attempt-Semantics-State:
Error-Sanitizer-State:
Worker-Scope-Activation-State:
Worker-Commerce-Context-Trust-State:
Worker-StoreCommerce-State:
PollTarget-Source-State:
Metadata-Reflection-State:
Persistence-Boundary-State:
Messaging-Boundary-State:
StoreContext-Boundary-State:
Sql-Identifier-Safety-State:
Observability-Correlation-State:
Metrics-State:
Logging-Sensitivity-State:
Hardcoded-Runtime-Text-State:
Exception-Message-Classification-State:
Sensitive-Data-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
W1-Guard-State:
Host-Observability-Certification-State:
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
Do not start Persistence/Configuration.
Do not start another Host folder.
Do not open module recovery.

Wait for Architect/user review.

END_TOOBA_TASK