PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W1
Parent-Task: TB-TMAR-HOST-ADMIN-PANEL-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Admin Panel AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_PANEL_MIGRATE_W1_SELLERS_LIST
Title: Move GET /v1/admin/sellers from Host Panel to Party CQRS/Endpoints
Estimated-Time-Minutes: 16
Hard-Timebox-Minutes: 20

ARCHITECT REVIEW STATE

Parent Analyze result:
ACCEPTED

Architect verified parent evidence:

analyze.md
ownership-map.md
route-map.md
migration-plan.md

Architect also re-checked repository reality after Analyze.

IMPORTANT ARCHITECT ADJUSTMENT

The Analyze plan grouped GET /sellers and POST /sellers/query in one Wave 1.

Architect has intentionally SPLIT that plan for bounded execution.

Reason:
POST /sellers/query has an additional Grid validation/error-presentation path through:

Host/Admin/Grid/AdminGridQueryEndpoint
Party.Infrastructure.Grid.PartyAdminSellersGridPolicies
GridQueryValidationException / PlatformHttpException

That path needs its own bounded migration/hygiene wave so Cursor is not pushed into a >20 minute task or a test/fix loop.

Therefore this W1 migrates ONLY:

GET /v1/admin/sellers

Do NOT migrate POST /v1/admin/sellers/query in this task.

BASELINE

Latest accepted implementation remains:
913ce3ca6c7e89a04f6ed9ebb74d1bc8a8660179

Analyze/docs commit:
b99009e56cfbc5ad7272bcf5650c5898db65c49c

Current Host/Admin/Panel files at Analyze close:

AdminPanelComposer.cs
AdminPanelEndpoints.cs
AdminPanelModels.cs

Current GET /v1/admin/sellers behavior:
AdminPanelEndpoints.ListSellersAsync
-> AdminPanelComposer.ListSellersAsync
-> Offer.Contracts + Party.Contracts + Order.Contracts
-> IReadOnlyList<AdminSellerListItem>

Current auth:
AdminPanelAccess / IAdminPanelAccess semantics

Current success shape:
raw JSON array of AdminSellerListItem

Current error semantics:
Admin panel auth failure status/errorCode behavior

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

This is MIGRATE only.

Do NOT run certify.
Do NOT start the next Host folder.
Do NOT migrate POST /v1/admin/sellers/query.
Do NOT move dev-context.
Do NOT change dashboard ownership.

ACTIVE RECOVERY UNIT

src/backend/Host/Tooba.Host/Admin/Panel/

Minimum destination-module changes in Party are authorized only as required for this GET sellers migration.

Do NOT reopen as recovery units:

Host/Admin/Access
Host/Admin/Grid
Host/Admin/Development
Host/Development

ARCHITECTURE DECISION — LOCKED FOR W1

GET /v1/admin/sellers ownership:
MOVE_TO_PARTY

Final HTTP owner:
Tooba.Party.Endpoints

Final application use-case owner:
Tooba.Party.Application

Cross-module reads required by this Party use case:
Contracts-only

Allowed foreign boundaries:

Tooba.Offer.Contracts
Tooba.Order.Contracts
Tooba.Party.Contracts
Tooba.BuildingBlocks neutral abstractions

Forbidden:

foreign .Application
foreign .Infrastructure
foreign .Domain
foreign DbContext / DbSet
cross-module EF/SQL joins
module -> Host dependency

AUTHORIZATION DECISION

Do NOT create a new Host Party authorizer merely for this route.

Use the already-existing neutral platform seam:

Tooba.BuildingBlocks.Security.IAdminPanelAccess

Party.Endpoints may consume this neutral BuildingBlocks security contract directly at the endpoint edge.

The implementation remains Host-owned through existing DI.

Do NOT modify Host/Admin/Access unless compilation proves one deterministic registration adjustment is strictly required.
Do NOT move or redesign Access.

REQUIRED MIGRATION

PARTY APPLICATION QUERY

Create one authoritative Party Application query for the admin sellers list.

Canonical shape:

IRequest<Result<IReadOnlyList<AdminSellerListItem>>>

Handler must own the current seller-list composition behavior:

obtain seller status rows / seller IDs from the existing Offer Contracts boundary
obtain Party status projections from Party Contracts boundary
obtain order counts from Order Contracts boundary
preserve current result ordering/shape exactly
preserve CancellationToken propagation

Use capability-first shallow foldering.

Preferred responsibility shape:

Tooba.Party.Application/Admin/Sellers/Queries/

Do not create a per-use-case one-file folder.

Do not create a duplicate command/query-shaped model.

If current Party.Application project references do not include required Offer.Contracts / Order.Contracts, add ONLY those Contracts project references.

No foreign Application/Infrastructure/Domain references.

PARTY ENDPOINT

Add Party-owned Admin seller list endpoint:

GET /v1/admin/sellers

Canonical endpoint behavior:

authorize via IAdminPanelAccess
dispatch via ISender
consume the new Party query
map Result through ApiResponseFactory
preserve the shipped success JSON shape as the seller array
preserve authorization status/error-code semantics
no raw Results.Json
no local ToError
no catch-by-message
no exception.Message classification

If the canonical global exception boundary already correctly handles platform auth exceptions, do not add a local catch merely to imitate the old Host code.

Map the route through PartyEndpointModule without changing existing seller settings routes.

HOST PANEL REMOVAL

Remove ONLY GET /v1/admin/sellers ownership from Host/Admin/Panel.

Delete from Host Panel:

route mapping for GET /sellers
ListSellersAsync endpoint method
AdminPanelComposer.ListSellersAsync
constructor dependencies used ONLY by ListSellersAsync, if no longer used by dashboard or grid

Do NOT remove:

GET /v1/admin/dashboard
POST /v1/admin/sellers/query
GET /v1/admin/dev-context
AdminPanelComposer.QuerySellersGridAsync
AdminDashboardSummary

Expected Host/Admin/Panel production file count after W1:
3

The folder stays present.

DUPLICATE ROUTE ZERO

At end:
exactly one owner for GET /v1/admin/sellers

Required:
Party.Endpoints = owner
Host/Admin/Panel = zero mapping for GET /sellers

Do not temporarily leave dual route ownership in committed state.

CONTRACT / MICROservice BOUNDARY

Preserve:
AdminSellerListItem ownership in Party.Contracts.

Do NOT move it into Application.

Do NOT expose Party entities.

Do NOT use DbContext across modules.

Do NOT introduce a new BFF/Admin module.

This route must be independently extractable with Contracts-only external calls.

CQRS / MEDIATR

Required final path:

Party endpoint
-> ISender
-> Party IRequest<Result<...>>
-> Party IRequestHandler
-> Contracts-only dependencies
-> Result
-> ApiResponseFactory

MediatR version:
12.5

No generic/custom dispatcher.

VALIDATION CLASSIFICATION

Classify the new GET list query explicitly.

Expected classification:
NO_VALIDATOR_REQUIRED

Reason:
no transport/input payload beyond cancellation/context.

Persist this classification in evidence/guard where current Party architecture conventions require it.

Do not add a meaningless validator.

LOCALIZATION / HARD-CODED TEXT

Hard rule:
NO hard-coded Persian or English user-facing runtime text in any new/touched Party Application or Endpoints production code.

Stable machine codes are allowed.

Comments/XML docs may be Persian/English; runtime client-facing messages may not.

Do not introduce:

"Not Found"
Persian error titles
inline English error titles
ex.Message as client message

Reuse canonical error catalog/localization ownership.

API RESULT / ERROR MAPPING

New Party endpoint must use ApiResponseFactory.

No:

Results.Json
Results.BadRequest
Results.Problem
local ProblemDetails mapper
local ToError
message parsing

Preserve the current success contract shape.

Unknown exceptions flow to the global exception boundary.

OBSERVABILITY / CORRELATION

Do not introduce:

new ActivitySource
new Meter
manual traceparent parsing
custom correlation ID
sensitive logging

Preserve canonical pipeline behavior.

PATH / NAMESPACE / COHESION

All new files:
exact path-derived namespace.

No:

alias
shim
TypeForwardedTo
duplicate request type
root dump
one-file-per-request folder
mixed Application *Contracts.cs addition

Do not broaden into cleanup of existing Party root debt unrelated to this route.

CLOSED-FOLDER INTEGRITY

Do not add files to:

Host/Admin/Access
Host/Admin/Grid
Host/Admin/Development
Host/Development

unless one deterministic compile blocker proves an already-existing registration needs a minimal edit.

No new Host folder.
No sink-folder regression.
No resurrection of HOST_ZERO folders.

BEHAVIOR PARITY — MANDATORY

Preserve exactly:

Route:
GET /v1/admin/sellers

Success:
HTTP 200

Success body:
same JSON array shape of AdminSellerListItem

Data semantics:

same seller population
same DisplayName
same Status
same ActiveOffers count
same OrderCount
same current ordering behavior

Authorization:
same admin authorization semantics through IAdminPanelAccess

Cancellation:
preserved

Schema:
unchanged

Frontend:
unchanged

No product behavior redesign.

TOUCHED-SURFACE CHECK

Before READY_FOR_CERTIFICATION state for this wave, re-read every touched production file.

Verify:

correct ownership
exact namespace
no foreign App/Infra/Domain
no hard-coded runtime FA/EN
no raw Results.Json in the new Party route
no duplicate route
no accidental POST sellers/query change
no dashboard/dev-context change

FOCUSED VALIDATION ONLY

Build only:

Tooba.Party.Application
Tooba.Party.Endpoints
Tooba.Party.Infrastructure only if DI/project refs require it
Tooba.Host
directly affected architecture/test project

Run only focused tests/guards covering:

Party endpoint mapping / route ownership
GET admin sellers behavior parity
Host Panel route residue
CQRS/validator classification if such guard exists
durable TMAR guard directly affected

NO solution-wide tests.

NO OPEN-ENDED TEST LOOP.

If one focused validation fails for one clear deterministic local cause:

one bounded repair
rerun only that affected validation once

If it still fails or needs broader/speculative work:
STOP with INCOMPLETE.

Do not weaken any guard/test/baseline.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W1/

Required:

ownership.md
route-parity.md
cqrs.md
boundary.md
localization-api.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W1.task.md

RECOVERY / SOT — MANDATORY DoD

On PASS update the existing TMAR recovery sources honestly.

Record at minimum:

task = TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W1
active Host folder = Admin/Panel
W1 state = GET_SELLERS_MOVED_TO_PARTY
GET /v1/admin/sellers owner = Party.Endpoints
Host GET sellers residue = ZERO
POST /v1/admin/sellers/query = DEFERRED_UNCHANGED
dashboard = KEEP_UNCHANGED
dev-context = DEFERRED_UNCHANGED
Host/Admin/Panel file count = 3
cross-module boundary = CONTRACTS_ONLY
schema = NONE
frontend = UNCHANGED
nextHostFolderStarted = false
workflowStop = USER_REVIEW_HOST_ADMIN_PANEL_AMC_001_W1
automaticNextImplementationTask = NONE

lastAcceptedCommit must be the actual W1 implementation commit, not a later docs/stamp commit.

If docs/stamp is separate, record it separately.

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

GET /v1/admin/sellers is owned only by Party.Endpoints
Host mapping for GET /sellers = ZERO
Party endpoint uses ISender
authoritative Party IRequest/Handler exists
request returns Result<...>
endpoint uses ApiResponseFactory
success JSON shape preserved
authorization uses neutral IAdminPanelAccess
no Party -> Host dependency
foreign App/Infra/Domain = ZERO
cross-module DbContext/join = ZERO
AdminSellerListItem remains Party.Contracts owned
NO_VALIDATOR_REQUIRED classification is explicit
no hard-coded runtime FA/EN introduced in touched Application/Endpoints
no Results.Json in new Party admin sellers list route
no ex.Message classification/client presentation
POST /v1/admin/sellers/query unchanged
dashboard unchanged
dev-context unchanged
Host/Admin/Panel final production file count = 3
schema unchanged
frontend unchanged
focused build/tests pass
Recovery points to W1 implementation commit
automaticNextImplementationTask = NONE
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W1
Parent-Task: TB-TMAR-HOST-ADMIN-PANEL-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Migration-State:
Active-Host-Folder:
Host-Panel-Initial-File-Count:
Host-Panel-Final-File-Count:
Get-Sellers-Route-Owner-State:
Host-Get-Sellers-Residue-State:
Duplicate-Route-State:
Party-Query-State:
Party-Handler-State:
ISender-State:
Result-State:
ApiResponseFactory-State:
Authorization-Seam-State:
AdminSellerListItem-Ownership-State:
Validator-Classification-State:
Cross-Module-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Cross-Module-Join-State:
Hardcoded-User-Facing-Text-State:
Exception-Message-Classification-State:
Path-Namespace-State:
Cohesion-State:
Post-Sellers-Query-State:
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

Do not start W2.
Do not migrate POST /v1/admin/sellers/query.
Do not touch dev-context.
Do not start another Host folder.

Wait for Architect/user review.

END_TOOBA_TASK