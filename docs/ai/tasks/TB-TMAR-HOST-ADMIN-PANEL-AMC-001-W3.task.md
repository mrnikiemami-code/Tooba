PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W3
Parent-Task: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W2-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Admin Panel AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_PANEL_MIGRATE_W3_DASHBOARD_HYGIENE
Title: Canonicalize retained Host admin dashboard presentation without changing ownership
Estimated-Time-Minutes: 14
Hard-Timebox-Minutes: 18

ARCHITECT REVIEW STATE

W2-R1:
ACCEPTED

Architect verified:

validator and validation codes now under Admin/Sellers/Validators
exact namespace = Tooba.Party.Application.Admin.Sellers.Validators
old copies = ZERO
validation machine code unchanged
W2 behavior unchanged
current accepted implementation = 86c2ac7308ab90c17f1d5d125fa59e97cdaaac8c
Host/Admin/Panel remains exactly 3 files

CURRENT PANEL STATE

Retained responsibilities:

GET /v1/admin/dashboard

legitimate thin Host cross-module presentation composition
current composer uses Contracts-only:
Catalog.Contracts
Offer.Contracts
Order.Contracts
current endpoint still uses:
direct AdminPanelAccess
CurrentAuthenticatedSession
ICurrentTenant
IAuthorizationGuard
IHostEnvironment
raw Results.Json
catch PlatformHttpException
local ToError

GET /v1/admin/dev-context

development-only responsibility
still deferred
NOT part of this W3

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

MIGRATE/HYGIENE ONLY.

Do NOT certify.
Do NOT start Host/Admin/Development.
Do NOT move dev-context.
Do NOT change dashboard ownership.
Do NOT invent a new Admin/BFF module.
Do NOT re-open Party sellers migration.

ARCHITECTURE DECISION — DASHBOARD

Disposition remains:

KEEP_AS_THIN_HOST_CROSS_MODULE_PRESENTATION_COMPOSITION

The dashboard has no natural single business-module owner.
Host may retain this presentation aggregation because:

reads are Contracts-only
no business write/policy
no persistence
no DbContext
no cross-module join
no foreign Application/Infrastructure/Domain

CQRS EXCEPTION — EXPLICIT

Do NOT add MediatR/ISender/IRequestHandler inside Host/Admin solely to satisfy shape.

Reason:
this retained Host surface is composition/presentation, not a module-owned application use case, and existing Host Admin canonical guards intentionally prohibit MediatR/business-handler ownership in Host/Admin.

Required quality bar remains:

typed neutral authorization seam
Result/ApiResponseFactory presentation
no raw Results.Json
no local exception mapping
no hard-coded user-facing runtime text
unknown exception propagation to global boundary

Record this as:
HOST_PRESENTATION_COMPOSITION_CQRS_EXCEPTION

This exception is ownership-specific, not a quality exception.

REQUIRED W3 CHANGES

AUTHORIZATION SEAM

Replace dashboard endpoint's direct dependency on:

AdminPanelAccess
CurrentAuthenticatedSession
ICurrentTenant
IAuthorizationGuard
IHostEnvironment

with the neutral platform seam:

Tooba.BuildingBlocks.Security.IAdminPanelAccess

Required behavior:
await adminAccess.RequireAuthorizedAsync(request, cancellationToken)

Do NOT modify Host/Admin/Access.
Do NOT change authorization semantics.
Do NOT add a new authorizer type.

API RESULT

Dashboard success must flow through:

Result<AdminDashboardSummary>
-> ApiResponseFactory

Smallest legal implementation is preferred.

Do not create a new generic dispatcher.
Do not create a Host Application layer.
Do not create a Host CQRS handler.

No:

Results.Json for dashboard
local ToError
local ProblemDetails
catch PlatformHttpException for dashboard
ex.Message classification

Expected/known platform exceptions from IAdminPanelAccess must flow to the existing global exception presentation boundary.

COMPOSER

AdminPanelComposer remains:

thin
read-only
Contracts-only
no HTTP
no auth
no Result mapping if presentation mapping is cleaner at endpoint
no persistence

Preserve GetDashboardAsync calculations exactly:

PublishedProducts
ActiveOffers
OpenOrders
PaidOrders
PendingOrders
Sellers = distinct Offer seller count
Customers

No count semantic change.
No parallelization redesign unless already behavior-equivalent and clearly trivial; default preserve sequential behavior.

ADMIN DASHBOARD SUMMARY

Keep:
AdminDashboardSummary

in:
Tooba.Host.Admin.Panel

Do not move it into module Contracts.
Do not duplicate it.

DEV-CONTEXT — STRICTLY UNCHANGED

GET /v1/admin/dev-context remains byte-for-behavior unchanged in W3.

Do not:

move it
rename it
change its 404
change "admin.dev.unavailable"
change its JSON shape
fix its "Not Found" string in this task

It is the explicit W4 target.

Because AdminPanelEndpoints contains both dashboard and dev-context, edit carefully so only the dashboard path changes.

HARD-CODED USER-FACING TEXT

For the dashboard runtime path after W3:

hard-coded FA/EN user-facing title/message = ZERO

Do not count XML/docs/comments.

Do not alter the deferred dev-context "Not Found" in this W3; evidence must explicitly classify it as:
DEFERRED_EXISTING_DEV_CONTEXT_ONLY_NOT_DASHBOARD_PATH

BOUNDARY / MICROSERVICE READINESS

Dashboard composition must continue to use only module Contracts.

Required:

foreign .Application = ZERO
foreign .Infrastructure = ZERO
foreign .Domain = ZERO
module DbContext = ZERO
cross-module EF/SQL join = ZERO
shared mutable entities = ZERO

No module -> Host dependency may be introduced.

This retained Host composition is allowed to become a future edge/BFF/read-projection seam without contaminating module ownership.

OBSERVABILITY

Do not add:

ActivitySource
Meter
logging scopes
actor/client IP logging
custom correlation
manual traceparent parsing

Use existing platform pipeline.

PATH / NAMESPACE / STRUCTURE

Host/Admin/Panel stays exactly 3 production files:

AdminPanelComposer.cs
AdminPanelEndpoints.cs
AdminPanelModels.cs

Namespace remains:
Tooba.Host.Admin.Panel

No new file unless absolutely required; preferred file count stays 3.

No alias/shim/forwarder.

GUARDS

Strengthen/update focused guard coverage to prove:

dashboard route remains Host-owned
dashboard uses IAdminPanelAccess
dashboard uses ApiResponseFactory
dashboard has no Results.Json path
dashboard has no local PlatformHttpException catch/ToError
composer remains Contracts-only
Host/Admin remains MediatR/ISender-free for this presentation composition exception
sellers GET/POST remain Party-owned
dev-context remains deferred unchanged
Panel file count stays 3

Do NOT weaken pre-existing canonical guards.

FOCUSED VALIDATION ONLY

Build:

Host
directly affected Host tests

Run:

AdminPanelCompositionTests
directly affected HostAdmin canonical guards
TmarDurableGuardTests only if current Recovery assertions require it

No solution-wide tests.

NO open-ended test loop.

If one focused failure has one deterministic local cause:
ONE bounded repair and ONE affected rerun only.

Otherwise STOP INCOMPLETE.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W3/

Required:

dashboard-ownership.md
authorization.md
api-result.md
boundary.md
cqrs-exception.md
behavior-parity.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W3.task.md

RECOVERY / SOT — MANDATORY

On PASS:

lastAcceptedTask = TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W3
lastAcceptedCommit = actual W3 implementation SHA
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
currentHostCheckpoint = Admin/Panel
dashboardDisposition = KEEP_AS_THIN_HOST_CROSS_MODULE_PRESENTATION_COMPOSITION
dashboardAuthorization = IAdminPanelAccess
dashboardApiResult = ApiResponseFactory
dashboardRawResultsJson = ZERO
dashboardLocalPlatformExceptionMapping = ZERO
dashboardCqrsState = HOST_PRESENTATION_COMPOSITION_CQRS_EXCEPTION
sellers GET/POST = Party.Endpoints
devContext = DEFERRED_UNCHANGED_W4_TARGET
Host/Admin/Panel file count = 3
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_ADMIN_PANEL_AMC_001_W3
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

If docs/stamp is separate, keep implementation SHA as lastAcceptedCommit.

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

dashboard owner remains Host/Admin/Panel
dashboard composition remains Contracts-only
dashboard auth uses IAdminPanelAccess
dashboard success uses Result + ApiResponseFactory
dashboard Results.Json = ZERO
dashboard local ToError = ZERO
dashboard local PlatformHttpException catch = ZERO
dashboard hard-coded runtime FA/EN = ZERO
dashboard count semantics preserved exactly
AdminDashboardSummary shape/ownership unchanged
Host/Admin does not gain MediatR/ISender/IRequestHandler
HOST_PRESENTATION_COMPOSITION_CQRS_EXCEPTION explicitly evidenced
sellers GET/POST remain Party-owned
dev-context behavior unchanged
existing dev-context "Not Found" explicitly deferred, not misreported as globally removed
foreign App/Infra/Domain = ZERO
cross-module DbContext/join = ZERO
Host/Admin/Panel file count = 3
schema unchanged
frontend unchanged
focused validation passes
Recovery points to W3 implementation SHA
automaticNextImplementationTask = NONE
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W3
Parent-Task: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W2-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Migration-State:
Dashboard-Disposition-State:
Dashboard-Owner-State:
Dashboard-Authorization-State:
Dashboard-Result-State:
Dashboard-ApiResponseFactory-State:
Dashboard-Raw-ResultsJson-State:
Dashboard-Local-ToError-State:
Dashboard-Local-PlatformException-Catch-State:
Dashboard-CQRS-State:
Dashboard-Hardcoded-User-Facing-Text-State:
Dashboard-Behavior-Parity-State:
AdminDashboardSummary-State:
Composer-Contracts-Only-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Cross-Module-Join-State:
Host-Admin-MediatR-State:
Get-Sellers-State:
Post-Sellers-Query-State:
Dev-Context-State:
Deferred-Dev-Context-Hardcoded-Text-State:
Host-Panel-File-Count:
Path-Namespace-State:
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

Do not start W4.
Do not move dev-context.
Do not start Host/Admin/Development.
Do not start another Host folder.

Wait for Architect/user review.

END_TOOBA_TASK