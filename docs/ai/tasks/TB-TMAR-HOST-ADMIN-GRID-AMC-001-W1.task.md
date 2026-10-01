PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-GRID-AMC-001-W1
Parent-Task: TB-TMAR-HOST-ADMIN-GRID-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Admin Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_GRID_DELETE_DEAD_RESIDUE
Title: Delete dead zero-consumer Host/Admin/Grid helper and close folder HOST_ZERO
Estimated-Time-Minutes: 9
Hard-Timebox-Minutes: 12

ARCHITECT REVIEW STATE

Parent Analyze:
ACCEPTED

Architect independently verified:

Host/Admin/Grid contains exactly 1 production file:
AdminGridQueryEndpoint.cs
type is internal static
production consumers = 0
Program references = 0
Panel references = 0
only remaining references are test/allowlist/history
current disposition = DEAD_ZERO_CONSUMER_RESIDUE
parent Panel certification remains authoritative
last accepted implementation SHA remains:
39ea2e66d188cab7719ac99dd624c22778d7fb18

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

MIGRATE ONLY.

Do NOT certify another area.
Do NOT open Admin/Access.
Do NOT touch Panel/Development/Party sellers behavior.
Do NOT start another Host folder.

ACTIVE RECOVERY UNIT

src/backend/Host/Tooba.Host/Admin/Grid/

CURRENT FILE

src/backend/Host/Tooba.Host/Admin/Grid/AdminGridQueryEndpoint.cs

DISPOSITION — LOCKED

DELETE_DEAD_ADMIN_GRID

Reason:
DEAD_ZERO_CONSUMER_RESIDUE

No migration destination.
No replacement helper.
No new shared Admin/Grid/BFF abstraction.

BuildingBlocks.Grid remains the neutral shared grid primitive owner.

REQUIRED CHANGES

DELETE DEAD FILE

Delete:

src/backend/Host/Tooba.Host/Admin/Grid/AdminGridQueryEndpoint.cs

After deletion:

Admin/Grid directory must be absent if empty
no stale physical copy
no shim
no alias
no type forwarder
no compatibility wrapper
UPDATE EXACT HOST/ADMIN STRUCTURE GUARDS

Current recursive Host/Admin production count:
19

After deletion:
18

Update exact-membership guards accordingly.

At minimum inspect and reconcile:

HostAdminCanon001..009 guard expectations if they encode count/membership
HostAdminCanon008GuardTests
HostAdminCanon009GuardTests
HostAdminCanonicalCertificationGuardTests
HostAdminPanelAmcCertGuardTests

Required:

remove Grid/AdminGridQueryEndpoint.cs from exact allowlists
remove Admin/Grid folder from exact folder expectations where appropriate
update exact recursive count 19 -> 18
prefer assert ABSENT for Admin/Grid where certification protection benefits

Do NOT weaken exact checks into >=, floor, loose contains, or optional membership.

PRESERVE CERTIFIED PANEL

Must remain unchanged in behavior and ownership:

Host/Admin/Panel:

AdminPanelComposer.cs
AdminPanelEndpoints.cs
AdminPanelModels.cs

Dashboard:

GET /v1/admin/dashboard
Host-owned
IAdminPanelAccess
Result
ApiResponseFactory
HOST_PRESENTATION_COMPOSITION_CQRS_EXCEPTION

Panel certification:
HOST_ADMIN_PANEL_AMC_CERTIFIED / PANEL_KEEP_CERTIFIED

PRESERVE ADMIN/DEVELOPMENT CERTIFICATION

Must remain:

AdminDevActorBootstrap.cs
AdminDevContextEndpoints.cs
GET /v1/admin/dev-context
admin.dev.unavailable
404
ApiResponseFactory
HOST_DEVELOPMENT_PRESENTATION_CQRS_EXCEPTION

No behavior change.

PRESERVE PARTY SELLERS

Must remain:

GET /v1/admin/sellers -> Party.Endpoints
POST /v1/admin/sellers/query -> Party.Endpoints
CQRS/validators unchanged
no Host seller route reintroduction
ADMIN/ACCESS — STRICTLY OUT OF SCOPE

Do NOT edit:
src/backend/Host/Tooba.Host/Admin/Access/

Do NOT fix its current hard-coded messages in this task.
Do NOT certify it.
Do NOT change auth semantics.

NO BEHAVIOR CHANGE

Because deleted helper has zero production consumers:

Runtime behavior change must be NONE.

No route changes.
No API shape changes.
No schema changes.
No frontend changes.

PATH / NAMESPACE / STRUCTURE

Final Host/Admin top-level child folders expected:

Access
Development
Panel

Admin/Grid:
ABSENT

Host/Admin root flat .cs:
ZERO

Host/Admin recursive .cs:
18 exact

RECOVERY / SOT

On PASS record:

lastAcceptedTask = TB-TMAR-HOST-ADMIN-GRID-AMC-001-W1
lastAcceptedCommit = actual W1 implementation SHA
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
currentHostCheckpoint = Admin/Grid
Host/Admin/Grid = ABSENT
AdminGridQueryEndpoint = REMOVED
productionConsumerCountBeforeRemoval = 0
runtimeBehaviorChange = NONE
Host/Admin recursive file count = 18
parentPanelCertification = PRESERVED
Admin/Development certification = PRESERVED
Party sellers certification = PRESERVED
Admin/Access = NOT_OPENED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_ADMIN_GRID_AMC_001_W1
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

If docs/stamp separate:
lastAcceptedCommit MUST remain actual implementation SHA.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-ADMIN-GRID-AMC-001-W1/

Required:

deletion.md
structure.md
protection.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-ADMIN-GRID-AMC-001-W1.task.md

FOCUSED VALIDATION ONLY

Build:

Host
directly affected Host tests

Run focused:

HostAdmin Canon structure guards affected by count/membership
HostAdminPanelAmcCertGuardTests
any direct Admin/Grid residue guard
TmarDurableGuardTests only for Recovery truth

No solution-wide tests.

No open-ended test loop.

If one deterministic local guard update is needed:
ONE bounded correction + ONE rerun only.

Otherwise STOP INCOMPLETE.

SUCCESS CRITERIA

PASS only if:

AdminGridQueryEndpoint.cs deleted
Admin/Grid directory absent
production consumer count remains proven 0 pre-removal
stale type/reference in production = ZERO
Host/Admin recursive count = 18 exact
exact allowlists updated, not weakened
Panel certification preserved
Development certification preserved
Party sellers certification preserved
Admin/Access untouched
runtime behavior change = NONE
schema = NONE
frontend = UNCHANGED
focused validation passes
Recovery points to W1 implementation SHA
automaticNextImplementationTask = NONE
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-GRID-AMC-001-W1
Parent-Task: TB-TMAR-HOST-ADMIN-GRID-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Migration-State:
Active-Host-Folder:
Admin-Grid-Directory-State:
AdminGridQueryEndpoint-State:
Production-Consumer-PreRemoval-State:
Production-Consumer-Count:
Stale-Production-Reference-State:
Host-Admin-Recursive-File-Count:
Host-Admin-Exact-Allowlist-State:
Panel-Certification-State:
Admin-Development-Certification-State:
Party-Sellers-Certification-State:
Admin-Access-State:
Runtime-Behavior-Change-State:
Schema-Change-State:
Frontend-State:
Focused-Build-State:
Focused-Test-State:
Guard-State:
Recovery-State:
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

Do not start Admin/Access.
Do not start another Host folder.

Wait for Architect/user review.

END_TOOBA_TASK