PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-SELLER-AMC-001-R5
Parent-Task: TB-TMAR-HOST-SELLER-AMC-001-R4
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Seller AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: SELLER_R5_FINAL_DEV_CONTEXT_CLOSURE
Title: Evacuate final seller dev-context/bootstrap residue and close Host/Seller to ZERO

MANDATORY SKILLS

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

ACCEPTED BASELINE

latest accepted implementation: TB-TMAR-HOST-SELLER-AMC-001-R4
commit: a5f353c02329a1667b74a09e7ba4bf6eb1a12b38
Host/Seller files = exactly 2:
SellerPanelEndpoints.cs
SellerDevActorBootstrap.cs
Host-owned Seller routes = exactly 1:
GET /v1/seller/dev-contexts
Recovery current on Seller R4
Host/Security/Seller canonical and protected

ARCHITECTURE DECISION
This is the FINAL Host/Seller implementation closure.
Do NOT create a separate R6 implementation task if this task can certify the final state.

The remaining behavior is Development-only seller authorization/demo context.
Its durable owner is AccessControl Development capability, not Host/Seller.

Canonical target:

GET /v1/seller/dev-contexts -> Tooba.AccessControl.Endpoints seller/development surface
seller development authorization/demo orchestration -> AccessControl-owned development implementation
foreign Party access -> Party.Contracts only
foreign Identity access -> Identity.Contracts only
authorization tuple write -> existing neutral/AccessControl-owned canonical seam
no foreign DbContext / foreign Application / foreign Domain / foreign Infrastructure

IMPORTANT:

DO NOT move either remaining file into Host/Development.
Host/Development is a closed accepted folder; no sink-folder regression.
DO NOT create another Host seller/development sink.
responsibility must leave Host.

EXISTING USEFUL CONTRACT
Tooba.Party.Contracts.IPartyDevelopmentSeedGateway already exists.
Prefer extending/reusing this Party-owned development seam narrowly if needed rather than reading PartyDbContext or Party.Domain from AccessControl.

If membership lookup/creation is still required, add only the smallest Party.Contracts development capability necessary and implement it inside Party.Infrastructure.
Do not expose Party persistence or Domain types.

IDENTITY
Use existing Identity.Contracts authentication/identity seams only.
No Tooba.Identity.Infrastructure consumption outside Identity.

ACCESSCONTROL DEVELOPMENT OWNERSHIP
Use a capability-first path such as:

AccessControl.Application/Development/Seller/*
AccessControl.Infrastructure/Development/Seller/*
AccessControl.Endpoints/Seller/Development/*
Exact placement should follow the existing AccessControl structure standard.

Endpoint must not reference Infrastructure.
If snapshot state/provider is needed, define an AccessControl Application-owned port/model and implement it in AccessControl Infrastructure Development.

PRESERVE DEVELOPMENT BEHAVIOR
Preserve:

exact route: GET /v1/seller/dev-contexts
route unavailable outside Development -> 404 seller.dev.unavailable
not-ready -> 503 seller.dev.not-ready
response actors[] fields: actorUserId, actorLabel, sellerPartyId, sellerLabel, contextKind
contextKind values: seller-owner, seller-owner-alt, scoped-employee
Actor A/B demo identity semantics
seller A/B demo labels
membership semantics
member tuple semantics
scoped employee publication semantics
fail-closed behavior when authorization engine is unavailable
no schema/migration change
frontend unchanged

Do not preserve bad layering merely for code similarity.

HOST FINAL STATE — MANDATORY
On PASS:
src/backend/Host/Tooba.Host/Seller/ MUST NOT EXIST.

Delete:

SellerPanelEndpoints.cs
SellerDevActorBootstrap.cs

Also remove:

Host registration/mapping/bootstrap call sites that exist only for these files
stale Host Seller source-size/write baselines
stale route ownership assumptions

Do NOT delete or move Host/Security/Seller; it is a separate canonical global Host security adapter boundary and remains allowed.

FINAL HOST/SELLER CERTIFICATION IN THIS SAME TASK
Certify:

Host/Seller production file count = ZERO
Host/Seller route count = ZERO
Host/Seller directory absent
no Seller business route ownership remains in Host
no Seller development bootstrap business authority remains in Host
no sink-folder regression into Host/Development or another Host folder
Host/Security/Seller remains only thin security adapters and has ZERO foreign Application/Domain/Infrastructure/Persistence

ACCESSCONTROL CERTIFICATION PRESERVATION
AccessControl was already structure-certified.
Because this task adds a real Development capability needed to close Host/Seller:

update manifest/root allowlists only as genuinely required
exact path↔namespace
preserve COMPLETE_REFERENCE_PATTERN
no broad structural refactor
no unrelated AccessControl work

CQRS / ENDPOINT RULES
For the development GET route:

use ISender if it represents an Application query in the canonical AccessControl endpoint pattern
if the route is purely a development snapshot read over an Application port and the existing certified AccessControl pattern explicitly allows that, document the reason; do not invent a parallel architecture
no DbContext in Endpoints
validator classification explicit
canonical ApiResponseFactory/SemanticError where applicable

GUARDS
Create/update final Seller closure guard proving:

Host/Tooba.Host/Seller absent.
Host Seller route count ZERO.
/v1/seller/dev-contexts owned by AccessControl.Endpoints.
duplicate route ownership ZERO.
Host/Development unchanged from its accepted allowlist.
no seller bootstrap class moved anywhere under Host.
AccessControl development implementation consumes Party.Contracts / Identity.Contracts only.
foreign Party/Identity DbContext/Application/Domain/Infrastructure ZERO.
Host/Security/Seller R1A-R4 boundary remains canonical.
all R1A/R2/R3/R4 route ownership remains intact.
no sink-folder regression.
final Host/Seller closure is durable.

FOCUSED VALIDATION ONLY
Build only touched:

AccessControl.Application
AccessControl.Infrastructure
AccessControl.Endpoints
Party.Contracts / Party.Infrastructure only if contract seam extended
Host
focused test projects

Run only:

final Host Seller closure guard
relevant AccessControl development/seller tests
relevant Party development seam tests if touched
HostSellerAmcR1/R2/R3/R4 guards or their final superseding aggregate
Host module endpoint ownership guard
TmarDurableGuardTests
AccessControl structure/manifest guards directly affected

No solution-wide tests.
No unrelated module suites.

RECOVERY / SOT — FINAL SELLER CLOSURE IN SAME TASK
On PASS:

lastAcceptedTask = TB-TMAR-HOST-SELLER-AMC-001-R5
lastAcceptedCommit = actual R5 implementation commit
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
currentHostEvacuation.currentTask = TB-TMAR-HOST-SELLER-AMC-001-R5
latestAcceptedImplementationWave = TB-TMAR-HOST-SELLER-AMC-001-R5
currentHostCheckpoint = Seller
Seller folder state = CLOSED / HOST_ZERO
fullSellerFolderCertification = PASS
workflowStop = USER_REVIEW_HOST_SELLER_AMC_001_R5_FINAL_CLOSURE
nextTask = USER_REVIEW_HOST_SELLER_AMC_001_R5_FINAL_CLOSURE
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
nextHostFolderStarted = false
nextHostFolder = NONE_USER_DECISION_REQUIRED

Add hostSellerAmcR5:

dev-context route migrated = 1
Host Seller routes 1 -> 0
Host/Seller files 2 -> 0
Host/Seller directory = ABSENT
sellerDevBootstrapHostState = ZERO
sellerDevContextOwner = ACCESSCONTROL
HostDevelopmentSinkRegression = ZERO
fullSellerFolderCertification = PASS
certificationState = CLOSED_HOST_ZERO
automaticNextImplementationTask = NONE

Reconcile CURRENT/AUTHORITATIVE sections:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

No placeholder commit markers.
Do not auto-start another Host folder.
Do not create R6 merely to repeat certification if all final closure criteria are proven here.

EVIDENCE
Create:
docs/evidence/TB-TMAR-HOST-SELLER-AMC-001-R5/

migration.md
dev-context-ownership.md
host-zero.md
validation.md
certification.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-SELLER-AMC-001-R5.task.md

SUCCESS
PASS only if:

dev-context route AccessControl-owned
Host/Seller directory ABSENT
Host Seller routes ZERO
Host seller bootstrap authority ZERO
no Host/Development sink regression
cross-module boundaries Contracts-only
AccessControl certification preserved
R1A/R2/R3/R4 behavior/ownership preserved
Recovery fully synchronized
full Seller closure certified in this task
automaticNextImplementationTask = NONE
frontend unchanged
user work preserved

GIT
Work from latest main.
No reset/clean/rebase/force-push.
Commit and push main only on PASS.

CANONICAL RESULT
Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-SELLER-AMC-001-R5
Parent-Task: TB-TMAR-HOST-SELLER-AMC-001-R4
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
R4-State:
Dev-Context-Route-Ownership-State:
Seller-Dev-Bootstrap-Ownership-State:
AccessControl-Development-State:
Party-Development-Contract-State:
Identity-Boundary-State:
Foreign-DbContext-State:
Foreign-Application-Domain-Infrastructure-State:
Host-Seller-Route-Count-Before:
Host-Seller-Route-Count-After:
Host-Seller-File-Count-Before:
Host-Seller-File-Count-After:
Host-Seller-Directory-State:
Host-Development-Sink-Regression-State:
Duplicate-Route-State:
Behavior-Parity-State:
Host-Security-Seller-State:
AccessControl-Structure-Certification-State:
Schema-Change-State:
Frontend-State:
Final-Seller-Folder-Certification-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
Recovery-Placeholder-State:
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
Do not create/start Seller-R6.
Do not start another Host folder.
Wait for Architect/user review.

END_TOOBA_TASK
