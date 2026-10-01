PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W2
Parent-Task: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Admin Panel AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_PANEL_MIGRATE_W2_SELLERS_GRID
Title: Move POST /v1/admin/sellers/query from Host Panel to Party CQRS/Endpoints and close grid error path
Estimated-Time-Minutes: 18
Hard-Timebox-Minutes: 20

ARCHITECT REVIEW STATE

W1 Result:
ACCEPTED

Architect verified:

implementation commit dd90aa90033eeace6c602d88b0ed5dbda611cd0a
GET /v1/admin/sellers owner = Party.Endpoints
Host GET sellers residue = ZERO
Party Application query/handler present
ISender + Result + ApiResponseFactory present
IAdminPanelAccess neutral seam used
Host/Admin/Panel remains 3 files
POST /v1/admin/sellers/query unchanged
dashboard unchanged
dev-context unchanged
Recovery points to W1 implementation commit

Architect also verified the W1 Host/Admin exact-file guard reconciliation 15 -> 18 was NOT caused by W1:
the 18 Host/Admin production files already existed at the parent Analyze commit.
Do not alter that retained set in W2.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

MIGRATE ONLY.
Do NOT certify.
Do NOT start another Host folder.
Do NOT touch dashboard or dev-context.

ACTIVE RECOVERY UNIT

src/backend/Host/Tooba.Host/Admin/Panel/

Authorized minimum destination surface:

Tooba.Party.Application
Tooba.Party.Endpoints
Tooba.Party.Contracts only if an existing stable boundary must be reused/adjusted
Tooba.Party.Infrastructure only for the existing sellers-grid adapter/policy path directly required by this route
BuildingBlocks Grid only for inspection; do NOT broaden into a BuildingBlocks recovery/refactor unless one tiny deterministic change is strictly required to eliminate hard-coded user-facing error transport on this exact route

CURRENT SOURCE BEHAVIOR

POST /v1/admin/sellers/query

Current Host path:

AdminPanelEndpoints.QuerySellersGridAsync
-> Host/Admin/Grid/AdminGridQueryEndpoint.ExecuteAsync
-> AdminPanelAccess.RequireAuthorizedAsync
-> AdminPanelComposer.QuerySellersGridAsync
-> IAdminSellersGridPort.QueryAsync

Party grid path today:

IAdminSellersGridPort
-> AdminSellersGridAdapter
-> PartyAdminSellersGridPolicies.Normalize
-> AdminSellersGridQueryEngine.QueryAsync

CURRENT BLOCKERS ON THIS ROUTE

Host owns HTTP route
Host/Admin/Grid helper is in the request path
no ISender/CQRS request for the POST route
raw Results.Json in Host Grid helper
local PlatformHttpException mapping
PartyAdminSellersGridPolicies currently converts GridQueryValidationException using ex.Message
Grid validation messages are runtime Persian text in exception construction
future certified route must not surface hard-coded FA/EN or classify/present failures through exception.Message

ARCHITECTURE DECISION — LOCKED

Final route owner:
Tooba.Party.Endpoints

Final use-case owner:
Tooba.Party.Application

Authorization:
use neutral Tooba.BuildingBlocks.Security.IAdminPanelAccess directly at Party endpoint edge.

Grid execution:
reuse IAdminSellersGridPort as the stable Party.Contracts boundary.

No new Admin/BFF module.
No Party -> Host dependency.
No foreign Application/Infrastructure/Domain dependency.

REQUIRED MIGRATION

PARTY APPLICATION QUERY

Create one authoritative CQRS query for:

POST /v1/admin/sellers/query

Use capability-first shallow structure under the existing Admin/Sellers capability.

Required conceptual shape:

IRequest<Result<GridPageResponse<AdminSellerListItem>>>

The query may carry GridQueryRequest as input without inventing a duplicate transport model.

Handler:

delegates to IAdminSellersGridPort
returns Result<...>
preserves CancellationToken
contains no DbContext
contains no Host dependency

Do not create a command-shaped duplicate in Models.

VALIDATION CLASSIFICATION

This POST has transport/input shape and MUST be explicitly classified.

Determine using repository canonical validation conventions whether:

VALIDATOR_REQUIRED for structural transport constraints, or
structural normalization remains the existing grid policy and the MediatR request is explicitly NO_VALIDATOR_REQUIRED with durable reason.

Do NOT duplicate business/grid-field policy in FluentValidation merely to satisfy a count.

Whichever classification repository rules require, persist it durably.

No localized validator messages.
Stable machine codes only.

PARTY ENDPOINT

Add Party-owned:

POST /v1/admin/sellers/query

Required path:

endpoint
-> IAdminPanelAccess
-> ISender
-> Party query
-> Result<GridPageResponse<AdminSellerListItem>>
-> ApiResponseFactory

Preserve exact route and success body.

No:

Results.Json
local ToError
Results.BadRequest
Results.Problem
local ProblemDetails builder
ex.Message/exception.Message classification
catch-and-map for expected failures when canonical Result/SemanticError path applies
GRID VALIDATION / ERROR PATH — MUST CLOSE IN THIS WAVE

Do NOT simply move the Host raw error path into Party.

Inspect canonical Result/SemanticError/error-catalog mechanisms already in repository and implement the smallest repository-consistent conversion for grid validation failures.

Required final properties for this POST route:

invalid grid input keeps the existing stable grid.* machine error codes
status/classification semantics remain 400 where they were 400
no hard-coded Persian/English runtime title/message is exposed from Party Application/Endpoints/Infrastructure on this route
no ex.Message is used to choose, classify, or present the failure
unexpected exceptions still propagate to the global exception boundary
no duplicate ErrorDescriptor ownership
no second error/localization system

IMPORTANT:
The existing GridQueryValidationException may contain legacy hard-coded messages.
For this bounded task, do NOT launch a global BuildingBlocks rewrite.
It is sufficient to ensure the Party sellers-query path does not use those messages as client-facing contract or classification.
Use stable ErrorCode and the canonical catalog/localizer semantics.

If the only clean solution requires redesigning shared Grid globally, STOP with INCOMPLETE and report the exact blocker rather than expanding scope.

HOST PANEL REMOVAL

Remove ONLY the POST sellers grid responsibility from Host/Admin/Panel:

group.MapPost("/sellers/query", ...)
QuerySellersGridAsync endpoint method
AdminPanelComposer.QuerySellersGridAsync
IAdminSellersGridPort dependency from AdminPanelComposer if no longer required

After W2, Host/Admin/Panel still retains:

GET /v1/admin/dashboard
GET /v1/admin/dev-context
AdminDashboardSummary

Expected production file count:
3

Do NOT delete the Panel folder yet.

HOST ADMIN GRID

Do NOT open Host/Admin/Grid as a recovery unit.

After removing Panel's dependency, verify only:

Panel no longer references AdminGridQueryEndpoint
no W2 code is added to Host/Admin/Grid
no sink-folder regression

Admin/Grid cleanup is separate and later.

ROUTE OWNERSHIP

Final:
POST /v1/admin/sellers/query = Party.Endpoints only

Host Panel mapping = ZERO.

No duplicate route ownership.

MICROservice BOUNDARY

Must remain:

Party.Application -> Party.Contracts / BuildingBlocks / allowed foreign Contracts only
Party.Infrastructure -> foreign Contracts only where already required
Party.Endpoints -> Party.Application + Party.Contracts + BuildingBlocks

Forbidden:

Party -> Host
foreign .Application
foreign .Infrastructure
foreign .Domain
foreign DbContext
cross-module DbSet
cross-module EF/SQL join
shared mutable entities
HARD-CODED USER-FACING TEXT

Hard rule for every touched production file:

NO hard-coded Persian or English runtime user-facing text.

Comments/XML docs are fine.

Machine-stable codes are fine.

Explicitly audit the grid validation path for Persian text transport.

Do NOT use exception.Message as presentation.

BEHAVIOR PARITY

Preserve:

Route:
POST /v1/admin/sellers/query

Input:
same GridQueryRequest JSON contract

Success:
HTTP 200

Success body:
same GridPageResponse<AdminSellerListItem> shape

Grid semantics:

paging
search
sorting
filters
advanced filters
seller rows
ActiveOffers
OrderCount
existing max/default behavior

Authorization:
same admin access semantics through IAdminPanelAccess

Expected validation failures:
same stable grid.* error codes
same 400 classification

Cancellation:
preserved

Schema:
unchanged

Frontend:
unchanged

PATH / NAMESPACE / COHESION

Exact path-derived namespace.

No alias.
No shim.
No TypeForwardedTo.
No duplicate request shape.
No generic mixed Application *Contracts.cs.
No single-file wrapper folder.
No god-file expansion.

CLOSED-FOLDER INTEGRITY

Do not add/move production files into:

Host/Admin/Access
Host/Admin/Grid
Host/Admin/Development
Host/Development
any previously HOST_ZERO folder

No new Host folder.

TOUCHED-SURFACE CHECK

Before claiming migration readiness, re-read all touched production files and verify:

no Host sellers-query route residue
no AdminGridQueryEndpoint reference from Panel
no Party -> Host
CQRS canonical
no raw Results.Json
no hard-coded runtime FA/EN
no ex.Message presentation/classification
exact namespace
no DB boundary leakage
dashboard unchanged
dev-context unchanged

FOCUSED VALIDATION ONLY

Build:

Party.Application
Party.Endpoints
Party.Infrastructure if touched
Host
directly affected Host test project

Focused tests/guards only:

POST sellers/query route ownership
grid behavior parity
grid invalid-input stable codes/status
Host Panel residue
CQRS/validator classification
directly affected TMAR durable guard

NO solution-wide test run.

NO open-ended test/fix loop.

If one failure has one clear deterministic local cause:
ONE bounded repair and ONE affected rerun only.

If still failing or broader work needed:
STOP INCOMPLETE.

Do not weaken guards/tests/baselines.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W2/

Required:

ownership.md
route-parity.md
grid-error-path.md
cqrs-validation.md
boundary.md
localization-api.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W2.task.md

RECOVERY / SOT — MANDATORY

On PASS record:

lastAcceptedTask = TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W2
lastAcceptedCommit = actual W2 implementation SHA
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
active Host folder = Admin/Panel
POST /v1/admin/sellers/query owner = Party.Endpoints
Host POST sellers/query residue = ZERO
Host Panel -> AdminGridQueryEndpoint dependency = ZERO
GET /v1/admin/sellers remains Party.Endpoints
dashboard = KEEP_UNCHANGED
dev-context = DEFERRED_UNCHANGED
Host/Admin/Panel production files = 3
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_ADMIN_PANEL_AMC_001_W2
nextHostFolderStarted = false
staleCurrentPointerState = ZERO

If docs/stamp is separate, lastAcceptedCommit remains implementation SHA.

GIT

Work from latest origin/main.

No reset.
No clean.
No rebase.
No force push.

Preserve unrelated user work.

Commit/push main only on PASS.

SUCCESS CRITERIA

PASS only if:

POST /v1/admin/sellers/query owner = Party.Endpoints only
Host POST sellers/query mapping = ZERO
Panel -> AdminGridQueryEndpoint dependency = ZERO
Party endpoint uses IAdminPanelAccess + ISender + ApiResponseFactory
authoritative IRequest/Handler exists
Result<GridPageResponse<AdminSellerListItem>> used
validation classification explicit and durable
existing GridQueryRequest/response contract preserved
IAdminSellersGridPort remains stable boundary
grid.* error codes preserved
invalid grid failures remain HTTP 400
ex.Message presentation/classification on this route = ZERO
hard-coded runtime FA/EN in touched path = ZERO
raw Results.Json on new Party route = ZERO
foreign App/Infra/Domain = ZERO
Party -> Host = ZERO
foreign DbContext/cross-module join = ZERO
GET sellers remains Party-owned
dashboard unchanged
dev-context unchanged
Host/Admin/Panel file count = 3
no sink-folder regression
schema unchanged
frontend unchanged
focused validation passes
Recovery points to W2 implementation SHA
automaticNextImplementationTask = NONE
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W2
Parent-Task: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Migration-State:
Active-Host-Folder:
Host-Panel-Initial-File-Count:
Host-Panel-Final-File-Count:
Post-Sellers-Query-Owner-State:
Host-Post-Sellers-Residue-State:
Duplicate-Route-State:
Panel-AdminGridQueryEndpoint-Dependency-State:
Party-Query-State:
Party-Handler-State:
ISender-State:
Result-State:
ApiResponseFactory-State:
Authorization-Seam-State:
Validator-Classification-State:
Grid-Contract-Parity-State:
Grid-Error-Code-State:
Grid-Error-Http-State:
Grid-Exception-Message-Presentation-State:
Hardcoded-User-Facing-Text-State:
Cross-Module-Boundary-State:
Party-To-Host-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Cross-Module-Join-State:
Path-Namespace-State:
Cohesion-State:
Get-Sellers-State:
Dashboard-State:
Dev-Context-State:
Sink-Folder-Regression-State:
Schema-Change-State:
Frontend-State:
Behavior-Parity-State:
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

Do not start W3.
Do not move dev-context.
Do not change dashboard.
Do not start Host/Admin/Grid.
Do not start another Host folder.

Wait for Architect/user review.

END_TOOBA_TASK