PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W4
Parent-Task: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Admin Panel AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_PANEL_MIGRATE_W4_DEV_CONTEXT_SPLIT
Title: Move GET /v1/admin/dev-context from Panel to Admin/Development and remove hard-coded runtime presentation text
Estimated-Time-Minutes: 16
Hard-Timebox-Minutes: 20

ARCHITECT REVIEW STATE

W3:
ACCEPTED

Architect verified:

dashboard remains Host/Admin/Panel thin cross-module composition
dashboard auth = IAdminPanelAccess
dashboard Result + ApiResponseFactory
dashboard Results.Json = ZERO
dashboard local PlatformHttpException mapping = ZERO
HOST_PRESENTATION_COMPOSITION_CQRS_EXCEPTION documented
sellers GET/POST remain Party-owned
current accepted implementation:
1cc1659cc31e98adab04479a92a3ff107b5f3727
dev-context remains the only non-dashboard HTTP responsibility in AdminPanelEndpoints

PARENT ANALYZE DECISION RESOLVED BY ARCHITECT

Parent Analyze marked dev-context:
MUST_SPLIT_NEEDS_ARCHITECT_DECISION

Architect decision is now:

MOVE_TO_HOST_ADMIN_DEVELOPMENT

Destination:
src/backend/Host/Tooba.Host/Admin/Development/

Reason:

responsibility is explicitly Development-only
snapshot source already lives there
avoids contaminating retained Panel composition
avoids using locked generic Host/Development as a sink
keeps ownership semantically local
does not invent a module/BFF

This task AUTHORIZES the exact Admin/Development allowlist expansion required for this responsibility.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

MIGRATE ONLY.
Do NOT certify yet.
Do NOT start Host/Admin/Access recovery.
Do NOT start Host/Admin/Grid recovery.
Do NOT start another top-level Host folder.

ACTIVE SOURCE UNIT

src/backend/Host/Tooba.Host/Admin/Panel/

AUTHORIZED DESTINATION UNIT

src/backend/Host/Tooba.Host/Admin/Development/

Current destination file:

AdminDevActorBootstrap.cs

Authorized final destination files after W4:

AdminDevActorBootstrap.cs
AdminDevContextEndpoints.cs

No other Admin/Development production file is authorized in this wave.

REQUIRED MIGRATION

MOVE DEV-CONTEXT HTTP OWNERSHIP

Move ownership of:

GET /v1/admin/dev-context

from:
Tooba.Host.Admin.Panel.AdminPanelEndpoints

to:
Tooba.Host.Admin.Development.AdminDevContextEndpoints

Required mapping:

route remains exactly /v1/admin/dev-context
GET method unchanged
route exists exactly once
Panel mapping residue = ZERO

Do NOT leave a forwarding method in Panel.
Do NOT leave a compatibility shim.
Do NOT create duplicate route ownership.

DEVELOPMENT-ONLY AVAILABILITY

Preserve the route's Development-only semantics.

If environment is not Development OR snapshot is unavailable:
return the same stable machine code:

admin.dev.unavailable

HTTP status:
404

Do not expose internal exception messages.

CANONICAL ERROR PRESENTATION

Remove hard-coded:

"Not Found"

from this route.

Use the existing canonical presentation/error mechanism.

Required:

stable code = admin.dev.unavailable
canonical ErrorDescriptor/catalog ownership
canonical localization/resource path
ApiResponseFactory / SemanticError path
HTTP 404 preserved
no local Results.Json error object
no hard-coded FA/EN runtime title
no ex.Message/exception.Message presentation or classification

Do NOT create a second error system.

If admin.dev.unavailable is not currently registered, register it exactly once in the narrowest legitimate Host/platform error catalog/resource location.

Do not create duplicate descriptors.

SUCCESS RESPONSE

Preserve success JSON shape:

{
actorUserId,
actorLabel,
tenantId
}

Use canonical success presentation; raw JSON success is acceptable ONLY through ApiResponseFactory Result<T> path.

Preferred:
Result.Success(...)
-> ApiResponseFactory.From(...)

Do NOT introduce MediatR solely for this Development-only Host endpoint.

Record:
HOST_DEVELOPMENT_PRESENTATION_CQRS_EXCEPTION

HARD-CODED PERSIAN/ENGLISH RUNTIME TEXT — DEV PATH

The user-level architecture lock is strict:
no hard-coded Persian/English runtime presentation prose.

Audit BOTH:

AdminDevContextEndpoints.cs
AdminDevActorBootstrap.cs

Current bootstrap contains a hard-coded Persian actor label:
"مدیر نمونهٔ توبا"

This must not remain as user-facing/localized prose in production code.

Preserve response FIELD SHAPE, but normalize actorLabel to a stable non-localized development identity value derived from existing machine identity data (prefer the existing AdminEmail or another already-existing machine identifier) rather than localized prose.

Do NOT add another hard-coded display sentence in English.

This is an intentional Development-only hygiene change:
response shape preserved; localized prose removed.

Evidence must state exact before/after actorLabel semantics.

PANEL CLEANUP

After route move, AdminPanelEndpoints must own only:

GET /v1/admin/dashboard

Remove:

using Tooba.Host.Admin.Development from Panel
GetDevContext method
MapGet("/dev-context", ...)

Panel remains exactly 3 files:

AdminPanelComposer.cs
AdminPanelEndpoints.cs
AdminPanelModels.cs

No dev-context responsibility remains in Panel.

ADMIN/DEVELOPMENT STRUCTURE

Final Admin/Development allowlist:

AdminDevActorBootstrap.cs
AdminDevContextEndpoints.cs

Required namespace:
Tooba.Host.Admin.Development

No nested single-file folder.

No flat Admin root file.

No alias.
No shim.
No TypeForwardedTo.

HOST ADMIN STRUCTURE GUARDS

Intentional recursive Host/Admin production file count changes:

18 -> 19

This is NOT baseline weakening.
It is an Architect-authorized semantic move from Panel responsibility into the correct existing Development capability.

Update exact structure guards to prove:

Development:

AdminDevActorBootstrap.cs
AdminDevContextEndpoints.cs

Panel:

AdminPanelComposer.cs
AdminPanelEndpoints.cs
AdminPanelModels.cs

Recursive file count:
19

Do not loosen exact membership checks.
Do not replace exact allowlist with a floor/minimum assertion.

PROGRAM / MAPPING

Wire AdminDevContextEndpoints from Host composition/Program in the smallest canonical way.

Preferred:
explicit Development endpoint mapping guarded by route behavior/environment policy as appropriate.

Do NOT make module code depend on Host.

Ensure:

exactly one GET /v1/admin/dev-context
no route in production modules
no duplicate mapping
DASHBOARD PRESERVATION

W3 dashboard must remain untouched in behavior:

owner Host/Admin/Panel
IAdminPanelAccess
Result<AdminDashboardSummary>
ApiResponseFactory
no Results.Json
no local catch
Contracts-only composer
CQRS exception unchanged
SELLERS PRESERVATION

Remain:

GET /v1/admin/sellers -> Party.Endpoints
POST /v1/admin/sellers/query -> Party.Endpoints

No seller regression.

MICROSERVICE READINESS

Host/Admin/Development may depend on:

neutral BuildingBlocks
Identity.Contracts as already accepted
Host-level development/bootstrap seams

Forbidden:

foreign .Application
foreign .Infrastructure
foreign .Domain
foreign DbContext
cross-module EF/SQL join
shared mutable business entity

No module -> Host dependency introduced.

SENSITIVE DATA

Do not log:

password
actor token
authorization header
cookie
client IP
raw personal identifiers beyond existing response semantics

Do not expose the bootstrap password in dev-context response.

Current bootstrap password behavior is OUT OF SCOPE unless a direct touched-line repair is required.
Do not redesign development authentication in this wave.

OBSERVABILITY

No new:

ActivitySource
Meter
custom correlation
traceparent parsing
sensitive log scope

Use platform pipeline.

FOCUSED VALIDATION ONLY

Build:

Host
directly affected Host tests

Run focused only:

Admin panel route ownership
Admin Development route mapping
exact Host/Admin 19-file structure
admin.dev.unavailable code/status/localization
success response shape
W3 dashboard guard
sellers ownership guard
TmarDurableGuardTests only where Recovery assertions require it

NO solution-wide tests.

NO OPEN-ENDED TEST LOOP.

If one focused failure has one deterministic local cause:
ONE bounded repair + ONE affected rerun only.

Otherwise STOP INCOMPLETE.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W4/

Required:

ownership.md
route-parity.md
development-boundary.md
error-localization.md
actor-label-hygiene.md
structure.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W4.task.md

RECOVERY / SOT — MANDATORY

On PASS:

lastAcceptedTask = TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W4
lastAcceptedCommit = actual W4 implementation SHA
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
currentHostCheckpoint = Admin/Panel
devContextOwner = Host/Admin/Development
panelDevContextResidue = ZERO
devContextErrorCode = admin.dev.unavailable
devContextErrorHttp = 404
devContextHardcodedNotFound = ZERO
devContextCqrsState = HOST_DEVELOPMENT_PRESENTATION_CQRS_EXCEPTION
Admin/Development file count = 2
Host/Admin recursive file count = 19
Panel file count = 3
dashboard = W3_PRESERVED
sellers GET/POST = Party.Endpoints
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_ADMIN_PANEL_AMC_001_W4
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

If docs/stamp is separate:
lastAcceptedCommit MUST remain W4 implementation SHA.

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

GET /v1/admin/dev-context owner = Host/Admin/Development
Panel dev-context route/method/reference residue = ZERO
duplicate dev-context route = ZERO
route path/method unchanged
unavailable error code remains admin.dev.unavailable
unavailable HTTP remains 404
hard-coded "Not Found" = ZERO on dev-context path
error uses canonical descriptor/localizer/presentation
descriptor duplication = ZERO
success response fields unchanged
actorLabel no longer hard-coded localized Persian/English prose
no bootstrap password is exposed in response
Admin/Development exact allowlist = 2 files
Host/Admin exact recursive allowlist = 19 files
exact path↔namespace
Panel file count = 3
dashboard W3 behavior preserved
sellers W1/W2 behavior preserved
foreign App/Infra/Domain = ZERO
foreign DbContext/join = ZERO
schema unchanged
frontend unchanged
focused validation passes
Recovery points to W4 implementation SHA
automaticNextImplementationTask = NONE
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W4
Parent-Task: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Migration-State:
Dev-Context-Owner-State:
Panel-Dev-Context-Residue-State:
Duplicate-Route-State:
Dev-Context-Route-State:
Dev-Context-Error-Code-State:
Dev-Context-Error-Http-State:
Dev-Context-ApiResponseFactory-State:
Dev-Context-Descriptor-State:
Dev-Context-Localization-State:
Dev-Context-Hardcoded-NotFound-State:
Dev-Context-CQRS-State:
Dev-Context-Success-Shape-State:
Actor-Label-Hardcoded-Prose-State:
Actor-Label-Semantics-State:
Bootstrap-Password-Exposure-State:
Admin-Development-File-Count:
Admin-Development-Allowlist-State:
Host-Admin-Recursive-File-Count:
Host-Admin-Allowlist-State:
Panel-File-Count:
Dashboard-State:
Get-Sellers-State:
Post-Sellers-Query-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Cross-Module-Join-State:
Path-Namespace-State:
Hardcoded-User-Facing-Text-State:
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

Do not certify yet.
Do not start Host/Admin/Access.
Do not start Host/Admin/Grid.
Do not start another top-level Host folder.

Wait for Architect/user review.

END_TOOBA_TASK