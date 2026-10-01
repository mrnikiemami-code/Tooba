PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-TRANSPORT-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Transport AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_TRANSPORT_CERTIFICATION
Title: Certify Host/Transport as generic Host transport infrastructure (KEEP, not HOST_ZERO)

ARCHITECT DECISION
Host/Transport is NOT a HOST_ZERO target.
Target disposition:
KEEP_AS_GENERIC_HOST_TRANSPORT_INFRASTRUCTURE

Current retained files:

Transport/SqlTransportOptionsMapper.cs
Transport/ToobaIntegrationTransportConsumer.cs
Transport/ToobaIntegrationTransportMessage.cs

These are process/platform transport responsibilities:

MassTransit SQL transport option mapping
MassTransit integration-message consume adapter / handler dispatch
generic integration transport envelope

CURRENT BLOCKER
All three files are physically under Host/Tooba.Host/Transport but currently declare:
namespace Tooba.Host;

Required canonical namespace:
Tooba.Host.Transport

MANDATORY AUDIT

PATH / NAMESPACE
Repoint all 3 Transport files to exact namespace Tooba.Host.Transport.
Repoint all consumers/usings.
No aliases/shims.
No duplicate compatibility namespace.
Exact path-derived namespace guard required.
HOST TRANSPORT OWNERSHIP
Allowed:
MassTransit transport wiring/adapters
SQL transport option mapping
generic integration transport envelope
correlation / observability plumbing
generic dispatch to neutral IIntegrationEventHandler<>
commerce/tenant context reconstruction from durable envelope

Forbidden:

module-specific business policy
module Application/Domain/Infrastructure/Persistence references
module DbContext usage
module endpoint/composer/business-handler ownership
product/order/payment/etc semantic decisions
message-text classification
payload logging
FOREIGN MODULE LAYERS
Host/Transport must have ZERO references to any module:
.Application
.Domain
.Infrastructure
.Persistence

BuildingBlocks/platform-neutral contracts are allowed.

Explicitly audit:

Tooba.Persistence
OutboxMessage
WorkerCommerceContextFactory
IIntegrationEventSerializer
IIntegrationEventHandler<>
ICommerceContextAssigner
ICurrentTenant

Classify each dependency as:

GENERIC_PLATFORM_ALLOWED
or
FOREIGN_LAYER_BLOCKER

If Tooba.Persistence.OutboxMessage is generic shared platform persistence, document why it is allowed.
If it is module/business-owned, repair through a neutral transport/outbox contract instead.
Do not guess.

ENVELOPE SAFETY
ToobaIntegrationTransportMessage must remain generic transport metadata only:
EventType
Version
EventId
OccurredAt
TenantId
Edition
DeploymentId
CorrelationId
PayloadJson

No module-specific fields.
No $type / CLR assembly-qualified type coupling.
No logging of PayloadJson.
No secrets/connection strings in logs.

CONSUMER SAFETY
ToobaIntegrationTransportConsumer:
may deserialize generic integration event envelope
may reconstruct commerce context
may resolve neutral integration handlers
may invoke IIntegrationEventHandler<>
must not contain module-specific branching or business policy
must not switch on concrete module event types
must not access module DbContexts
unknown handler absence must preserve existing behavior
current tenant integrity check may remain if generic platform invariant
SQL TRANSPORT MAPPER
SqlTransportOptionsMapper:
generic MassTransit/Npgsql transport mapping only
no business schema knowledge
no logging of connection string/password
schema input is infrastructure schema input, not module schema authority
ROOT / ALLOWLIST CERTIFICATION
Create durable guard for exact Host/Transport allowlist:
SqlTransportOptionsMapper.cs
ToobaIntegrationTransportConsumer.cs
ToobaIntegrationTransportMessage.cs

No fourth file without future Architect authorization.

Guard must prove:

folder exists
exactly 3 production .cs files
exact namespace Tooba.Host.Transport
ZERO foreign module Application/Domain/Infrastructure/Persistence
no module-specific DbContext
no PayloadJson logging
no connection-string/password logging
no message-text classification
no new business authority
PROGRAM / COMPOSITION
Repoint Host Program/composition registrations/usings to Tooba.Host.Transport.
Do not move transport authority elsewhere.
Do not start any other Host folder.

RECOVERY / SOT
On PASS reconcile all authoritative surfaces:

lastAcceptedTask = TB-TMAR-HOST-TRANSPORT-AMC-001
lastAcceptedCommit = actual implementation commit
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-TRANSPORT-AMC-001
currentHostEvacuation.activeModule = Transport
currentHostCheckpoint = Transport
currentTask = TB-TMAR-HOST-TRANSPORT-AMC-001
activeModuleState = TRANSPORT_KEEP_GENERIC_HOST_INFRASTRUCTURE_USER_REVIEW_REQUIRED
workflowStop = USER_REVIEW_HOST_TRANSPORT_AMC_001_KEEP_GENERIC_HOST_INFRASTRUCTURE
nextTask = same user-review marker
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
Wallet / ProductQnA / Preferences / Reviews / Security remain accepted historical lineage

Reconcile:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

No placeholders.
No conflict markers.

SCOPE LIMIT
Do NOT:

attempt HOST_ZERO
move MassTransit transport into a business module
redesign messaging architecture
modify module event contracts unless a real foreign-layer blocker requires a neutral seam
touch frontend
change schema/migrations
start another Host folder
run solution-wide refactors
create aliases/shims

FOCUSED VALIDATION ONLY
Build:

Host
directly affected BuildingBlocks/shared platform projects if touched
directly affected tests

Run:

new HostTransportAmcGuardTests
existing messaging/transport architecture guards
TmarDurableGuardTests
focused Host composition tests if directly affected

No solution-wide test run.

SUCCESS
PASS only if all are true:

Host/Transport remains PRESENT
disposition = KEEP_AS_GENERIC_HOST_TRANSPORT_INFRASTRUCTURE
exactly 3 retained production files
path↔namespace = EXACT (Tooba.Host.Transport)
foreign module Application/Domain/Infrastructure/Persistence = ZERO
module DbContext usage = ZERO
business authority = ZERO
module-specific event branching = ZERO
payload logging = ZERO
connection-string/password logging = ZERO
message-text classification = ZERO
generic integration dispatch preserved
behavior parity preserved
schema unchanged
frontend unchanged
Recovery fully points to Transport AMC
staleCurrentPointerState = ZERO
automaticNextImplementationTask = NONE

EVIDENCE
Create:
docs/evidence/TB-TMAR-HOST-TRANSPORT-AMC-001/

analyze.md
dependency-classification.md
namespace-and-allowlist.md
transport-safety.md
recovery.md
validation.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-TRANSPORT-AMC-001.task.md

GIT
Work from latest main.
No reset/clean/rebase/force-push.
Preserve all user work.
Commit and push main only on PASS.

CANONICAL RESULT
Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-TRANSPORT-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Transport-Disposition-State:
Host-Transport-File-Count:
Path-Namespace-State:
Foreign-Module-Layer-State:
Shared-Persistence-Dependency-State:
Module-DbContext-State:
Business-Authority-State:
Module-Specific-Event-Branching-State:
Payload-Logging-State:
Connection-Secret-Logging-State:
Message-Text-Classification-State:
Integration-Dispatch-State:
Transport-Envelope-State:
Sql-Transport-Mapper-State:
Behavior-Parity-State:
Schema-Change-State:
Frontend-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
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
Do not start another Host folder.
Wait for Architect/user review.

END_TOOBA_TASK
