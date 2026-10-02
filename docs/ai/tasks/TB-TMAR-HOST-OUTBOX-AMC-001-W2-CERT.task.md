PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-OUTBOX-AMC-001-W2-CERT
Parent-Task: TB-TMAR-HOST-OUTBOX-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_OUTBOX_CERTIFY
Title: Certify Host/Outbox worker orchestration, cancellation safety, fail-fast options, and platform boundaries
Estimated-Time-Minutes: 10
Hard-Timebox-Minutes: 14

ARCHITECT REVIEW STATE

W1:
ACCEPTED FOR CERTIFICATION ENTRY

Architect independently verified current main:

Implementation commit:
382ef10af3a5eb49f519e49cb399809b19844bbc

Current Outbox production truth:

exact 7 production files
exact 7 top-level production types
exact namespace Tooba.Host.Outbox
one top-level type per file
Program imports Tooba.Host.Outbox
Outbox options use AddOptions + Bind + ValidateOnStart
OutboxHostOptionsValidator validates all five positive numeric settings
target-level requested OperationCanceledException rethrows
claim-level requested OperationCanceledException rethrows
message-level requested OperationCanceledException rethrows
requested cancellation cannot enter retry/dead-letter branch
non-cancellation target/module/message isolation preserved
retry/dead-letter formula preserved
per-message IServiceScopeFactory + scoped GetRequiredService preserved
Worker commerce context trusts registry authority
Worker StoreCommerce uses StoreContext.Contracts only
Messaging boundary remains IIntegrationEventPublisher only
Persistence boundary remains neutral seams only
Persian FromPollTarget operator exception prose removed
foreign App/Infra/Domain/DbContext zero
business authority zero
Observability/Messaging/Health/MultiTenancy/Errors/Security/Admin certs preserved

IMPORTANT RECOVERY NOTE

Current tmar-current-state.json was reformatted during W1 (semantic content preserved). Do NOT perform another whole-file cosmetic rewrite in CERT. Make minimal semantic edits only. Preserve valid JSON and existing data.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-certify/SKILL.md

CERTIFY ONLY.

No production migration.
No production redesign.
No Persistence AMC.
No Configuration AMC.
No next Host folder.
No module recovery.

If a material production defect is found:
Status = INCOMPLETE
Production-Repair-Required-State = YES
STOP.

PRIMARY CERTIFICATION SCOPE

src/backend/Host/Tooba.Host/Outbox/

Expected exact tree:

Outbox/

ConfiguredOutboxPollTargetSource.cs
OutboxDispatcher.cs
OutboxDispatcherHostedService.cs
OutboxHostOptions.cs
OutboxHostOptionsValidator.cs
WorkerCommerceContextFactory.cs
WorkerStoreCommerceContextFactory.cs

Expected:

production files = 7
production top-level types = 7
namespace = Tooba.Host.Outbox
one top-level type per file

Certification labels on PASS:

HOST_OUTBOX_AMC_CERTIFIED
HOST_OUTBOX_PLATFORM_BOUNDARY_CERTIFIED

MANDATORY CERTIFICATION AUDITS

EXACT TREE / TYPE / NAMESPACE

Verify:

exact 7 production .cs files
exact filenames
exact 7 top-level types
one top-level type per file
exact namespace Tooba.Host.Outbox
no unexpected subfolder/file
no legacy duplicate
no alias/shim/TypeForwardedTo
TYPE OWNERSHIP

Certify:

OutboxDispatcher
= GLOBAL_HOST_OUTBOX_PLATFORM_CERTIFIED

OutboxDispatcherHostedService
= GLOBAL_HOST_BACKGROUND_WORKER_CERTIFIED

OutboxHostOptions
= GLOBAL_HOST_OUTBOX_OPTIONS_CERTIFIED

OutboxHostOptionsValidator
= GLOBAL_HOST_OUTBOX_OPTIONS_VALIDATOR_CERTIFIED

ConfiguredOutboxPollTargetSource
= THIN_HOST_WORKER_CONTEXT_ADAPTER_CERTIFIED

WorkerCommerceContextFactory
= THIN_HOST_WORKER_CONTEXT_ADAPTER_CERTIFIED

WorkerStoreCommerceContextFactory
= THIN_HOST_WORKER_CONTEXT_ADAPTER_CERTIFIED

PROGRAM REGISTRATION

Verify exact current registration/lifetimes:

using Tooba.Host.Outbox
AddOptions<OutboxHostOptions>()
Bind Tooba:Outbox
ValidateOnStart()
singleton IValidateOptions<OutboxHostOptions>, OutboxHostOptionsValidator
singleton IOutboxDispatcherStore
singleton IOutboxPollTargetSource
singleton WorkerCommerceContextFactory
singleton IWorkerCommerceContextFactory adapter
singleton WorkerStoreCommerceContextFactory
singleton IWorkerStoreCommerceContextFactory adapter
singleton OutboxDispatcher
hosted OutboxDispatcherHostedService

No duplicate registration.

OPTIONS VALIDATOR

Certify fail-fast validation:

Required > 0:

PollIntervalSeconds
BatchSize
RetryBaseDelaySeconds
MaxAttempts
LockSeconds

Defaults preserved:

Enabled = true
PollIntervalSeconds = 2
BatchSize = 20
RetryBaseDelaySeconds = 2
MaxAttempts = 5
LockSeconds = 30

Operator validation prose allowed.
User-facing presentation role = ZERO.

TARGET CANCELLATION

Certify in DispatchOnceAsync:

OperationCanceledException when task token requested:

rethrows
does not increment tenant failure metric
does not log target failure
does not continue next target
CLAIM CANCELLATION

Certify module ClaimAsync path:

requested OCE:

rethrows
does not increment failure metric
does not log claim failure
does not continue next module
MESSAGE CANCELLATION

Certify per-message path:

requested OCE:

rethrows before sanitizer
does not call MarkRetryAsync
does not call MarkDeadLetterAsync
does not increment retry/dead-letter metrics
does not convert shutdown into durable message failure state

Certification label:
REQUESTED_CANCELLATION_PROPAGATES_NO_RETRY_DEADLETTER_CERTIFIED

HOSTED SERVICE CANCELLATION

Verify:

DispatchOnceAsync requested OCE breaks worker loop
Task.Delay requested OCE breaks worker loop
no warning log for normal requested shutdown
NON-CANCELLATION FAILURE ISOLATION

Certify:

target failure isolates target
claim failure isolates module
message processing failure goes to retry/dead-letter
next work continues as designed
worker loop catches unexpected non-cancellation failure and continues

Broad catches are background-worker resilience, not HTTP exception suppression.

RETRY / DEAD-LETTER

Certify exact current semantics:

AttemptCount threshold unchanged
MaxAttempts threshold unchanged
retry base delay unchanged
exponential expression unchanged
shift cap 8
SystemClock/NodaTime preserved
MarkRetry / MarkDeadLetter store contracts preserved

No jitter/new policy.

ERROR SANITIZATION

Verify:

OutboxErrorSanitizer.Sanitize(ex) used for durable last-error text
raw exception.Message not persisted directly
no stack trace persisted
no message-text classification
PER-MESSAGE WORKER SCOPE

Certify:

IServiceScopeFactory is injected into singleton dispatcher
CreateAsyncScope per message
scope resolves exactly:
ICommerceContextAssigner
IStoreCommerceContextAssigner
IIntegrationEventPublisher

Classification:
LEGITIMATE_PER_MESSAGE_WORKER_SCOPE_COMPOSITION_CERTIFIED

This is NOT a general service-locator exemption.

WORKER COMMERCE TRUST

Certify:

registry is authority for edition/deployment
Marketplace connection reference comes from registry
SingleStore message TenantId must exist in registry
SingleStore tenant must be Active
DB reference comes from registry record
HTTP Host/header ignored
message cannot inject connection reference

Classification:
ANTI_SPOOF_REGISTRY_AUTHORITY_CERTIFIED

POLL TARGET TRUST

Certify:

Marketplace one deployment target
SingleStore only Active tenants
unconfigured/other edition returns empty
refs/deployment derived from registry
target-to-worker relookup remains fail-closed where applicable
WORKER STORE COMMERCE

Certify:

Marketplace returns DeploymentStoreCommerce
SingleStore active tenant returns record.StoreCommerce
no Market/Currency/SalesChannel defaults
StoreContext.Contracts.Current only
no StoreContext Application/Infrastructure/Domain
METADATA CORRELATION COMPATIBILITY

Certify current reflection seam:

deserialize integration event
inspect Metadata property
if durable correlation absent, replace metadata with copied correlation value
no message-text parsing
no business decision

Classification:
PLATFORM_COMPATIBILITY_SEAM_CERTIFIED

Do NOT redesign in CERT.

PERSISTENCE BOUNDARY

Verify only neutral platform seams:

IOutboxDispatcherStore
IOutboxModuleRegistration
IIntegrationEventSerializer
IDatabaseConnectionResolver
OutboxMessage
OutboxPollTarget
OutboxErrorSanitizer

Required ZERO:

module DbContext
module repository
direct module SQL
module Application/Infra/Domain
MODULE REGISTRATION BOUNDARY

Verify:

IEnumerable<IOutboxModuleRegistration>
metadata-driven schema/table iteration
no module-name switch
no business routing
no foreign Application handler invocation
SQL IDENTIFIER SAFETY

Verify Host passes trusted registration metadata only.
Persistence store remains responsible for identifier quoting/validation.

No user/message-provided schema/table construction.

MESSAGING BOUNDARY

Verify Outbox uses:
IIntegrationEventPublisher only

Required ZERO:

MassTransit
IBus
transport envelope ownership
broker-specific publish logic

Messaging CERT preserved.

OBSERVABILITY / CORRELATION

Certify canonical only:

ToobaTelemetry
MessagingCorrelation
CorrelationIdContext
ToobaTraceEnricher

Counters:

tooba.outbox.tenant_failures
tooba.outbox.retries
tooba.outbox.dead_letters
tooba.outbox.processed

No parallel Meter/ActivitySource.

LOGGING SENSITIVITY

Allowed structured operational identifiers:

TenantId
EventType
Schema
ErrorType

Required ZERO:

event payload
connection string
connection reference
exception.Message
stack trace
secrets/tokens
HARD-CODED RUNTIME TEXT

Allowed:

internal/operator worker messages
startup validator messages

Required:

no Persian user-facing runtime prose
no user-facing business text
no localization ceremony for internal worker exceptions
EXCEPTION MESSAGE CLASSIFICATION

Required ZERO:

ex.Message branching
exception.Message branching
Message.Contains
Message.StartsWith
text → semantic code/status mapping
FOREIGN LAYERS / BUSINESS AUTHORITY

Required ZERO:

foreign module Application
foreign module Infrastructure
foreign module Domain
foreign module DbContext
business commands
business policy
domain mutation
ACTIVE CONSUMERS

Verify all seven types are active:

Dispatcher Program registration
HostedService Program registration
Options binding
Validator registration
PollTargetSource interface registration
WorkerCommerce factory concrete/interface use
WorkerStoreCommerce interface registration/use

No dead type.

W1 GUARD

Do NOT weaken:
HostOutboxAmcW1GuardTests

DURABLE CERT GUARD

Create:
HostOutboxAmcCertGuardTests

Lock at minimum:

exact 7-file/7-type tree
one type per file
exact namespace
Program registrations/lifetimes
options ValidateOnStart and five positive checks
requested OCE escape at target/claim/message
cancellation cannot enter retry/dead-letter
non-cancellation isolation remains
retry/dead-letter expression remains
per-message worker scope pattern
no MassTransit in Outbox
StoreContext Contracts-only
Persistence neutral seams only
registry authority trust semantics
no message classification
no sensitive data
foreign App/Infra/Domain/DbContext zero
protected cert labels in SoT
implementation SHA remains W1 SHA
FOCUSED TESTS

Run:

HostOutboxAmcW1GuardTests
HostOutboxAmcCertGuardTests
OutboxAmcW1BehaviorTests
OutboxFoundationTests
OutboxPostgresTests if focused/available
MassTransitPostgresTests only if namespace coupling requires
TmarDurableGuardTests

Required behavioral proof:

validator valid defaults
each <=0 option fails
target requested OCE propagates
claim requested OCE propagates
message requested OCE propagates
requested OCE => MarkRetryCalls 0
requested OCE => MarkDeadLetterCalls 0
non-cancellation retry preserved
max-attempt dead-letter preserved
module isolation preserved
target isolation preserved

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

No production edits under certified folders.

PRODUCTION CHANGE RULE

Expected:
Production-Code-Change-State = ZERO
Production-Repair-Required-State = NONE

Cert may change only:

tests/guards
docs/evidence
Recovery/SoT metadata

If production defect found:
STOP INCOMPLETE.

RECOVERY HYGIENE

Verify:

valid JSON
no duplicate properties
no stale historical current pointer
no automatic next folder
currentHostCheckpoint remains Outbox
implementation SHA remains W1
cert/docs stamp separate
avoid whole-file cosmetic JSON rewrite
EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-OUTBOX-AMC-001-W2-CERT/

Required:

certification-summary.md
physical-tree.md
cancellation-safety.md
options-validation.md
worker-scope-trust.md
retry-deadletter.md
boundary-observability.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-OUTBOX-AMC-001-W2-CERT.task.md

RECOVERY / SOT

On PASS:

Top-level:

lastAcceptedTask = TB-TMAR-HOST-OUTBOX-AMC-001-W2-CERT
lastAcceptedCommit remains W1 implementation:
382ef10af3a5eb49f519e49cb399809b19844bbc
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-OUTBOX-AMC-001-W1
currentHostCheckpoint = Outbox
nextTask = USER_REVIEW_HOST_OUTBOX_AMC_001_W2_CERT
workflowStop = USER_REVIEW_HOST_OUTBOX_AMC_001_W2_CERT
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Add/update:
hostOutboxAmc001W2Cert

Required:

certificationState = HOST_OUTBOX_AMC_CERTIFIED
boundaryState = HOST_OUTBOX_PLATFORM_BOUNDARY_CERTIFIED
productionFileCount = 7
productionTypeCount = 7
pathNamespace = EXACT_Tooba.Host.Outbox
fileCohesion = ONE_TOP_LEVEL_TYPE_PER_FILE_CERTIFIED
dispatcher = GLOBAL_HOST_OUTBOX_PLATFORM_CERTIFIED
hostedService = GLOBAL_HOST_BACKGROUND_WORKER_CERTIFIED
options = GLOBAL_HOST_OUTBOX_OPTIONS_CERTIFIED
optionsValidator = FAIL_FAST_CERTIFIED
pollTargetSource = THIN_HOST_WORKER_CONTEXT_ADAPTER_CERTIFIED
workerCommerceFactory = THIN_HOST_WORKER_CONTEXT_ADAPTER_CERTIFIED
workerStoreCommerceFactory = THIN_HOST_WORKER_CONTEXT_ADAPTER_CERTIFIED
workerScopeActivation = LEGITIMATE_PER_MESSAGE_WORKER_SCOPE_CERTIFIED
cancellationSafety = REQUESTED_OCE_PROPAGATES_NO_RETRY_DEADLETTER_CERTIFIED
broadExceptionPolicy = NON_CANCELLATION_WORKER_ISOLATION_CERTIFIED
retryDeadLetter = CERTIFIED
workerContextTrust = ANTI_SPOOF_REGISTRY_AUTHORITY_CERTIFIED
storeContextBoundary = CONTRACTS_ONLY_CERTIFIED
messagingBoundary = IIntegrationEventPublisher_ONLY_CERTIFIED
persistenceBoundary = NEUTRAL_PLATFORM_SEAMS_CERTIFIED
observabilityCorrelation = CANONICAL_CERTIFIED
messageClassification = ZERO
sensitiveData = ZERO
foreignApplication = ZERO
foreignInfrastructure = ZERO
foreignDomain = ZERO
foreignDbContext = ZERO
businessAuthority = ZERO
productionRepairRequired = false
productionCodeChange = ZERO
implementationCommit = 382ef10a...
hostObservabilityCertification = HOST_OBSERVABILITY_AMC_CERTIFIED_PRESERVED
hostMessagingCertification = HOST_MESSAGING_AMC_CERTIFIED_PRESERVED
hostHealthCertification = HOST_HEALTH_AMC_CERTIFIED_PRESERVED
hostMultiTenancyCertification = HOST_MULTITENANCY_AMC_CERTIFIED_PRESERVED
hostErrorsCertification = HOST_ERRORS_AMC_CERTIFIED_PRESERVED
hostSecurityCertification = HOST_SECURITY_AMC_CERTIFIED_PRESERVED
hostAdminCertification = HOST_ADMIN_FULLY_CERTIFIED_PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_OUTBOX_AMC_001_W2_CERT

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

exact 7-file/7-type Outbox tree
one type per file
exact namespace
all ownership dispositions certified
Program registration/lifetimes exact
fail-fast options exact
requested cancellation propagation certified at target/claim/message
cancellation never retry/dead-letter
non-cancellation isolation certified
retry/dead-letter exact
error sanitization exact
per-message worker scope certified
registry anti-spoof trust certified
StoreContext Contracts-only
Messaging IIntegrationEventPublisher-only
Persistence neutral seams-only
observability canonical
no sensitive leakage
no message classification
foreign App/Infra/Domain/DbContext zero
business authority zero
all seven types live
durable cert guard PASS
prior certs preserved
production repair NONE
production change ZERO
focused builds/tests PASS
recovery hygiene exact
implementation SHA remains 382ef10a...
automatic next NONE
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-OUTBOX-AMC-001-W2-CERT
Parent-Task: TB-TMAR-HOST-OUTBOX-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Certification-State:
Boundary-Certification-State:
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
HostedService-Cancellation-State:
Broad-Exception-Policy-State:
Target-Failure-Isolation-State:
Module-Failure-Isolation-State:
Message-Failure-Isolation-State:
Retry-DeadLetter-State:
Attempt-Semantics-State:
Error-Sanitizer-State:
Worker-Scope-Activation-State:
Worker-Commerce-Context-Trust-State:
PollTarget-Trust-State:
Worker-StoreCommerce-State:
Metadata-Reflection-State:
Persistence-Boundary-State:
Module-Registration-Boundary-State:
Sql-Identifier-Safety-State:
Messaging-Boundary-State:
StoreContext-Boundary-State:
Observability-Correlation-State:
Metrics-State:
Logging-Sensitivity-State:
Hardcoded-User-Facing-Text-State:
Exception-Message-Classification-State:
Sensitive-Data-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
Active-Consumer-State:
Durable-Cert-Guard-State:
Host-Observability-Certification-State:
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

Do not start Persistence/Configuration.
Do not start another Host folder.
Do not modify production code in Cert.
Do not open module recovery.

Wait for Architect/user review.

END_TOOBA_TASK
