PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-CLONE-ARCHIVE-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-WRITES-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Migrate only Admin platform role clone/archive routes to module CQRS
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-WRITES-001

ACCEPTED-PARENT-COMMIT:
c730061bc50835c0abf31c5ff17c1cdf898094b6

TIMEBOX:
Target <= 10 minutes.
Hard maximum 15 minutes.
If safe completion would exceed the timebox, return INCOMPLETE and STOP.

ONE OBJECTIVE:

Evacuate ONLY these two Admin platform role routes from Host:

POST /v1/admin/access-control/roles/{roleId:guid}/clone
DELETE /v1/admin/access-control/roles/{roleId:guid}

CURRENT HOST HANDLERS:

AdminCloneRoleAsync
AdminArchiveRoleAsync

CURRENT BEHAVIOR TO PRESERVE:

AdminCloneRoleAsync:

Admin panel authorization
AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", ...)
Platform owner scope + current tenant id
roleId from route
CloneAccessRoleCommand body
actor + X-Request-Id trace
directory.CloneRoleAsync(...)
Results.Json(...)
catches AccessControlException only
MapError semantics preserved

AdminArchiveRoleAsync:

Admin panel authorization
AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", ...)
Platform owner scope + current tenant id
roleId from route
actor + X-Request-Id trace
directory.ArchiveRoleAsync(...)
Results.NoContent()
catches AccessControlException only
MapError semantics preserved

DO NOT "FIX" exception asymmetry in this task.
Preserve current typed behavior exactly.

MANDATORY TARGET:

APPLICATION CQRS

Create focused real MediatR 12.5 commands, preferably:

Commands/CloneRole/CloneRoleCommand.cs
Commands/ArchiveRole/ArchiveRoleCommand.cs

Use IRequest / IRequestHandler.

No generic dispatcher.

Commands must contain only application data:

roleId
trusted actor id
tenant id
clone body values where applicable
trace id

Handlers may use IAccessControlDirectory and existing AccessControl types.

Application MUST NOT depend on:
HttpRequest
HttpContext
IResult
Results.*
Host
Endpoints

ADMIN ENDPOINTS

Extend:
Tooba.AccessControl.Endpoints/Admin/AccessControlAdminEndpoints.cs

Add exactly:

POST /roles/{roleId:guid}/clone
DELETE /roles/{roleId:guid}

Endpoint flow:
Admin panel authorization
→ accesscontrol.manage capability gate
→ Trace(request)
→ sender.Send(...)
→ preserve current response/error semantics

Clone success:
Results.Json(...)

Archive success:
Results.NoContent()

For expected AccessControlException:
preserve existing MapError-compatible payload/status.

Do not catch PlatformHttpException inside these handlers unless current Host behavior did.
Capability-gate PlatformHttpException behavior must remain semantically identical.

HOST EVACUATION

After module ownership is proven, remove ONLY:

admin.MapPost("/roles/{roleId:guid}/clone", AdminCloneRoleAsync)
admin.MapDelete("/roles/{roleId:guid}", AdminArchiveRoleAsync)

and remove ONLY:

AdminCloneRoleAsync
AdminArchiveRoleAsync

Do not remove shared helpers still used by residual routes.

OUT OF SCOPE:

GET/PUT /roles/{roleId}/permissions
all AdminSeller role routes
all Seller role routes
assignments
ceiling
user search/effective access
scope resources
demo-preview
DevelopmentSeed
DemoSnapshot
Program.cs cleanup
Checkout
Frontend
Fulfillment
database/schema/migrations

VALIDATOR CLASSIFICATION:

Clone:

user-controlled clone body exists.
Audit current underlying validation and stable error-code behavior.
Add a FluentValidation validator only if there is transport-invalid state not already authoritatively enforced without semantic drift.
Do not replace stable AccessControlException codes.

Archive:

no body.
route id is constrained by guid.
do not add ceremonial validation unless an accepted invariant requires it.

Record exact classification.

ERROR CONTRACT:

No ex.Message classification.
No message parsing.
No new generic exception translation.
Preserve typed/stable AccessControlException behavior exactly.

SINGLE ROUTE OWNERSHIP AFTER CHANGE:

Exactly one production mapping each:

POST /v1/admin/access-control/roles/{roleId:guid}/clone
DELETE /v1/admin/access-control/roles/{roleId:guid}

Both module-owned.

Host mappings for these routes = ZERO.

MANDATORY AUDIT:

AdminCloneRoleAsync = ZERO repo-wide
AdminArchiveRoleAsync = ZERO repo-wide
module clone mapping = EXACTLY ONE
module archive mapping = EXACTLY ONE
new commands sent through ISender
real IRequestHandler implementations
Endpoints direct IAccessControlDirectory for these routes = ZERO
Application -> Host = ZERO
Application -> Endpoints = ZERO

CANONICAL TASK ARTIFACT:

This exact Architect-issued task MUST be committed at:

docs/ai/tasks/TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-CLONE-ARCHIVE-001.task.md

Do not omit it.

EVIDENCE:

Create:

docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-CLONE-ARCHIVE-001/admin-role-clone-archive-migration.md

Evidence must include:

before/after route ownership
CQRS files
validator classification
auth parity
trace parity
response parity
exception parity
Host removals
single-route ownership
residual role routes
focused validation results

FOCUSED VALIDATION:

dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Run only directly relevant focused tests if they already exist or are added for validator/handler behavior.

No solution build.
No broad test suite.
No retries.

RECOVERY HONESTY:

AccessControl remains IN_PROGRESS.
Host AccessControl residue remains non-zero.
Do NOT mark role family complete.
Do NOT mark COMPLETE_REFERENCE_PATTERN.
Do NOT structure-certify AccessControl.

After this task, Admin platform base role CRUD may be module-owned, but Role Permissions and AdminSeller/Seller role families still remain.

FINAL TRACK TARGET remains:
src/backend/Host/Tooba.Host/AccessControl = ZERO files
but NOT in this task.

PASS ONLY IF:

exactly Clone + Archive Admin role routes migrated
real CQRS used
current auth/trace/response/error behavior preserved
Host mappings/handlers for only these two routes removed
canonical task artifact committed
evidence committed
focused builds succeed
no unrelated route family changes
bounded scope maintained

If dependency expansion is required:
Status = INCOMPLETE
STOP.
Do not widen scope.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-CLONE-ARCHIVE-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-WRITES-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Application-CQRS-State:
Clone-Role-State:
Archive-Role-State:
Validator-Classification:
Authorization-Parity-State:
Trace-Parity-State:
Response-Parity-State:
Error-Parity-State:
Host-Route-Residue:
Single-Route-Ownership-State:
Application-Boundary-State:
Endpoint-Boundary-State:
Canonical-Task-State:
Focused-Builds:
Focused-Tests:
Production-Code-Scope:
Evidence:
Checkout-State:
Frontend-Production-Changes:
Residual-Host-Role-Routes:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP
Do not auto-start another task.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
