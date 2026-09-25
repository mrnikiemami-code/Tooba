PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-WRITES-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-READS-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Migrate only Admin platform role create/update routes to module CQRS
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-READS-001-R1

ACCEPTED-PARENT-COMMIT:
3f16c1f6c2c0ce02477a9de77965af3aee2f13fc

TIMEBOX:
Target <= 10 minutes.
Hard maximum 15 minutes.
If safe completion would exceed the timebox, return INCOMPLETE and STOP.

ONE OBJECTIVE:

Evacuate ONLY these two Admin platform role mutation routes from Host:

POST /v1/admin/access-control/roles
PUT /v1/admin/access-control/roles/{roleId:guid}

CURRENT HOST HANDLERS:

AdminCreateRoleAsync
AdminUpdateRoleAsync

DO NOT migrate Clone or Archive in this task.

CURRENT BEHAVIOR TO PRESERVE:

AdminCreateRoleAsync:

Admin panel authorization
AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", ...)
owner scope = Platform + current tenant id
pass actor + X-Request-Id trace
call existing role creation authority
return JSON result
preserve AccessControlException / PlatformHttpException HTTP mapping exactly

AdminUpdateRoleAsync:

Admin panel authorization
AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", ...)
owner scope = Platform + current tenant id
pass roleId, actor, X-Request-Id trace
call existing role update authority
return JSON result
preserve current exception mapping exactly, including any existing asymmetry

MANDATORY TARGET:

APPLICATION CQRS

Create focused MediatR 12.5 commands, preferably:

Commands/CreateRole/CreateRoleCommand.cs
Commands/UpdateRole/UpdateRoleCommand.cs

Use real IRequest / IRequestHandler.

Do not create a generic role dispatcher.

Commands must contain only application-relevant data:

trusted actor id
tenant id
route/body values
trace id if required for parity

Application must not depend on:
HttpRequest
HttpContext
IResult
Results.*
Host
Endpoints

Handlers may use IAccessControlDirectory and existing AccessControl types.

ENDPOINTS

Add only these mappings in AccessControlAdminEndpoints:

POST /roles
PUT /roles/{roleId:guid}

Endpoint flow:

Admin panel authorization
→ accesscontrol.manage capability gate
→ extract X-Request-Id using a small endpoint-local/shared module helper if needed
→ sender.Send(...)
→ preserve exact response and expected error mapping

Do not introduce Host dependencies into Endpoints.

HOST EVACUATION

After module routes are proven, remove ONLY:

admin.MapPost("/roles", AdminCreateRoleAsync)
admin.MapPut("/roles/{roleId:guid}", AdminUpdateRoleAsync)

and remove ONLY:

AdminCreateRoleAsync
AdminUpdateRoleAsync

Do not touch shared helpers still used elsewhere.

OUT OF SCOPE:

POST /roles/{roleId}/clone
DELETE /roles/{roleId}
GET/PUT role permissions
AdminSeller role routes
Seller role routes
Assignments
Ceiling
Users/effective
Scope resources
Demo
DevelopmentSeed
DemoSnapshot
Program.cs cleanup
Checkout
Frontend
Fulfillment
Database/schema

VALIDATION / FLUENTVALIDATION:

Audit both commands under current TMAR validation rules.

Create role body contains user-controlled fields and SHOULD have a concrete validator if the application accepts invalid transport state.

Update role body contains user-controlled fields and SHOULD have a concrete validator if required by the existing domain/application contract.

Do not duplicate domain rules unnecessarily.
Do not add ceremonial validators.

Record exact validator coverage/classification.

ERROR CONTRACT:

Do not classify expected errors by ex.Message.
Do not add message parsing.
Preserve current typed/stable error behavior.

If current Host behavior contains an inconsistency, preserve it in this migration and record it as Residual-Defect; do not broaden scope to repair unless compilation requires it.

SINGLE ROUTE OWNERSHIP AFTER CHANGE:

Exactly one production mapping each for:

POST /v1/admin/access-control/roles
PUT /v1/admin/access-control/roles/{roleId:guid}

Both must be module-owned.

Host mappings = ZERO for these two routes.

MANDATORY SEARCH/AUDIT:

AdminCreateRoleAsync = ZERO
AdminUpdateRoleAsync = ZERO

module POST /roles = EXACTLY ONE
module PUT /roles/{roleId:guid} = EXACTLY ONE

new commands dispatched through ISender
real IRequestHandler implementations
Endpoints direct IAccessControlDirectory for these routes = ZERO
Application -> Host = ZERO
Application -> Endpoints = ZERO

CANONICAL TASK ARTIFACT:

This exact Architect-issued task MUST be committed at:

docs/ai/tasks/TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-WRITES-001.task.md

Do not omit it.

EVIDENCE:

Create:

docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-WRITES-001/admin-role-writes-migration.md

Evidence must include:

before/after route ownership
CQRS files
validator coverage/classification
auth parity
trace parity
response/error parity
Host removals
single-route ownership
residual role routes
focused builds/tests

FOCUSED VALIDATION:

dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Run only directly relevant focused tests if they already exist or are added for validator/handler behavior.

No solution build.
No broad suite.
No retry loop.

RECOVERY HONESTY:

AccessControl remains IN_PROGRESS.
Host AccessControl residue remains non-zero.
Do NOT mark role family complete.
Do NOT mark COMPLETE_REFERENCE_PATTERN.
Do NOT structure-certify AccessControl.

FINAL TRACK TARGET remains:
src/backend/Host/Tooba.Host/AccessControl = ZERO files
but NOT in this task.

PASS ONLY IF:

exactly Create + Update Admin role routes migrated
real CQRS used
validators correctly classified/implemented
authorization, trace, response and error semantics preserved
Host handlers/mappings removed only for these two routes
no unrelated route family changed
canonical task artifact committed
evidence committed
focused builds succeed
task stays bounded

If scope expands unexpectedly:
Status = INCOMPLETE
STOP.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-WRITES-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-READS-001-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Application-CQRS-State:
Create-Role-State:
Update-Role-State:
Validator-Coverage:
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
