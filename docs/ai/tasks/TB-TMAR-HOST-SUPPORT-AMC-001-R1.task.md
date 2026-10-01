PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-SUPPORT-AMC-001-R1
Parent-Task: TB-TMAR-HOST-SUPPORT-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Support AMC Repair
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_SUPPORT_REPAIR
Title: Remove AccessControl Application/Domain leakage from Support Host development binder and reconcile Recovery

BASELINE
Accepted parent implementation:
023d26c1e844658245fbcf4bc666dbfe9e410c55

Parent docs/stamp:
6dbc17c2b4ec13b6ee4f633304bf357ece9b5d7c

Parent accepted state to PRESERVE:

src/backend/Host/Tooba.Host/Support/ = ABSENT
Host/Support = HOST_ZERO
Support HTTP remains module-owned
migrate + seed owner:
Support.Infrastructure.Development.SupportDevelopmentSeedBootstrap
Host binder:
Host/Composition/SupportDevelopmentSeedHost.cs
Guest actor uses:
Order.Contracts.Fulfillment.StorefrontGuestActor
Host binder contains NO SupportDbContext
Host binder contains NO Database.MigrateAsync
schema unchanged
frontend unchanged

ARCHITECT R1 BLOCKER
Host/Composition/SupportDevelopmentSeedHost.cs currently directly imports/uses foreign AccessControl internal layers:

Tooba.AccessControl.Application.Development.Seller
Tooba.AccessControl.Application.Models
Tooba.AccessControl.Domain

and directly consumes items such as:

ISellerDevContextStore
IAccessControlDirectory
AccessOwnerScope
AccessOwnerScopeKind

This violates the Host boundary lock:
Host composition may coordinate platform/development bootstrap, but must not depend on foreign module Application/Domain/Infrastructure/Persistence authority.

R1 OBJECTIVE
Remove ALL direct AccessControl Application/Domain leakage from the Host Support development binder while preserving current development seed behavior.

MANDATORY ARCHITECTURE DECISION

Host Support binder remains a thin composition adapter only.
AccessControl-specific development bootstrap responsibility must be exposed through an AccessControl-owned neutral boundary.
Prefer an existing AccessControl.Contracts / neutral BuildingBlocks seam if one already exists.
If no adequate seam exists, add the SMALLEST AccessControl.Contracts development/bootstrap contract required for this exact flow, with implementation inside AccessControl-owned Application/Infrastructure as appropriate.
Do NOT move AccessControl business/development logic into Host.
Do NOT create aliases/shims.
Do NOT widen scope beyond the Support development seed flow.

REQUIRED PRESERVED BEHAVIOR
The resulting flow must still:

create a scoped provider
resolve current ControlPlane registry
require active store-alpha
assign the CommerceContext
ensure Admin dev actor
ensure seller dev context
ensure AccessControl bootstrap / seller capability tuple prerequisites
call SupportDevelopmentSeedBootstrap.ApplyAsync(...)
pass:
StorefrontGuestActor.ActorId
seller party id
seller actor user id
admin actor user id
preserve cancellation/order/idempotency semantics

HOST BOUNDARY — REQUIRED END STATE
SupportDevelopmentSeedHost.cs must have ZERO references to:

Tooba.AccessControl.Application
Tooba.AccessControl.Domain
Tooba.AccessControl.Infrastructure
Tooba.AccessControl.Persistence

Host may depend only on:

AccessControl.Contracts, if needed
neutral BuildingBlocks/platform seams
Host-owned composition/development helpers
Support.Infrastructure.Development bootstrap
Order.Contracts StorefrontGuestActor

The Host binder must NOT:

construct AccessOwnerScope
reference AccessOwnerScopeKind
call AccessControl Application directory interfaces
own AccessControl bootstrap policy

ACCESSCONTROL OWNER REQUIREMENT
The AccessControl side must own any internal translation from the neutral contract to:

seller dev context storage
bootstrap state
access owner scope
capability tuple synchronization

If a new contract is required:

place it in a capability-first path under Tooba.AccessControl.Contracts
exact path ↔ namespace
no Host references from AccessControl
no Application/Domain types leak through the contract
no opaque object/dynamic dictionaries
use explicit stable DTO/record values only if unavoidable
keep API minimal for this one development/bootstrap operation

SUPPORT HOST GUARD REPAIR
Strengthen HostSupportAmcGuardTests.

It must prove at minimum:

Host/Support remains ABSENT
Support module bootstrap remains owner of migrate+seed
Host binder contains no SupportDbContext
Host binder contains no Database.MigrateAsync
Host binder contains no Order.Application
Host binder contains no AccessControl.Application
Host binder contains no AccessControl.Domain
Host binder contains no AccessControl.Infrastructure
Host binder contains no AccessControl.Persistence
Guest actor still comes from Order.Contracts.StorefrontGuestActor
Host binder still delegates to SupportDevelopmentSeedBootstrap.ApplyAsync

Also add a durable boundary guard proving any new AccessControl contract:

is in Contracts
does not expose Application/Domain/Infrastructure types
has exact namespace/path
does not introduce Host dependency into AccessControl

NO SINK-FOLDER REGRESSION
Do not move this debt into another Host folder.
Do not create a new Host AccessControl helper folder to hide the same leakage.
The foreign authority must be rehomed behind the module-owned neutral seam.

RECOVERY / SOT — MANDATORY DoD
Recovery synchronization is REQUIRED for PASS.

Update all authoritative surfaces:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

Required final state:

lastAcceptedTask = TB-TMAR-HOST-SUPPORT-AMC-001-R1
lastAcceptedCommit = <actual R1 implementation commit SHA>
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-SUPPORT-AMC-001-R1
currentHostEvacuation.currentTask = TB-TMAR-HOST-SUPPORT-AMC-001-R1
currentHostEvacuation.activeModule = Support
currentHostEvacuation.currentHostCheckpoint = Support
active state = SUPPORT_CLOSED_HOST_ZERO_R1_USER_REVIEW_REQUIRED
workflowStop = USER_REVIEW_HOST_SUPPORT_AMC_001_R1_CLOSED_HOST_ZERO
nextTask = USER_REVIEW_HOST_SUPPORT_AMC_001_R1_CLOSED_HOST_ZERO
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO

Historical lineage must remain intact:

parent Support AMC
Transport AMC
Wallet
ProductQnA
Preferences
Reviews
Security
and prior accepted closures.

If docs/stamp is a separate commit:

lastAcceptedCommit MUST still point to the R1 IMPLEMENTATION commit, not the docs stamp.

SCOPE LIMIT
Do NOT:

reopen Host/Support folder
change Support HTTP ownership
move Support seed back into Host
redesign AccessControl generally
restructure unrelated AccessControl capabilities
touch frontend
change DB schema/migrations
run solution-wide refactors
start another Host folder
change behavior beyond the required seam repair
touch unrelated user file accesscontrol-first-slice-map.md

FOCUSED VALIDATION ONLY

Build:

AccessControl.Contracts
whichever AccessControl owner project implements the seam
Support.Infrastructure if affected
Host
affected tests

Run focused:

HostSupportAmcGuardTests
any new AccessControl boundary/development seam tests
focused Support development seed tests if present
TmarDurableGuardTests

No solution-wide test run.

EVIDENCE
Create:
docs/evidence/TB-TMAR-HOST-SUPPORT-AMC-001-R1/

Required:

blocker.md
accesscontrol-seam.md
host-boundary.md
behavior-parity.md
recovery.md
validation.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-SUPPORT-AMC-001-R1.task.md

SUCCESS CRITERIA
PASS only if all are true:

Host/Support remains ABSENT
Support HOST_ZERO preserved
Support migrate/seed remains module-owned
SupportDevelopmentSeedHost has ZERO AccessControl Application/Domain/Infrastructure/Persistence references
Host binder uses only Contracts/neutral seams for AccessControl prerequisite work
no AccessControl internal types leak through Contracts
Guest actor still from Order.Contracts
SupportDbContext in Host = ZERO
Database.MigrateAsync in Host Support binder = ZERO
behavior parity preserved
schema change = NONE
frontend = UNCHANGED
durable guard covers AccessControl foreign-layer leakage
Recovery fully reconciled to Support R1
lastAcceptedCommit points to actual R1 implementation SHA
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO

GIT
Work from latest origin/main.
No reset.
No clean.
No rebase.
No force push.
Preserve unrelated user work.
Commit/push main only on PASS.

CANONICAL RESULT
Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-SUPPORT-AMC-001-R1
Parent-Task: TB-TMAR-HOST-SUPPORT-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Host-Support-State:
Support-Host-Zero-State:
Support-Seed-Ownership-State:
Host-AccessControl-Application-State:
Host-AccessControl-Domain-State:
Host-AccessControl-Infrastructure-State:
Host-AccessControl-Persistence-State:
AccessControl-Neutral-Seam-State:
AccessControl-Internal-Type-Leakage-State:
Guest-Actor-State:
Support-DbContext-In-Host-State:
Host-Database-Migrate-State:
Sink-Folder-Regression-State:
Behavior-Parity-State:
Schema-Change-State:
Frontend-State:
Guard-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
Last-Accepted-Commit-State:
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