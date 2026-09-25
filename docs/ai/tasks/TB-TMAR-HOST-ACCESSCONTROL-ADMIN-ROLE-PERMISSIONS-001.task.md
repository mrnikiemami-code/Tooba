PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-PERMISSIONS-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-CLONE-ARCHIVE-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Migrate only Admin platform role-permissions routes to module CQRS
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-CLONE-ARCHIVE-001

ACCEPTED-PARENT-COMMIT:
4aba9f1a2cf6086fb8ce812ef410271edc927d4f

TIMEBOX:
Target <= 10 minutes.
Hard maximum 15 minutes.
If safe completion would exceed the timebox, return INCOMPLETE and STOP.

ONE OBJECTIVE:

Evacuate ONLY these two Admin platform role-permissions routes from Host:

GET /v1/admin/access-control/roles/{roleId:guid}/permissions
PUT /v1/admin/access-control/roles/{roleId:guid}/permissions

CURRENT HOST HANDLERS:

AdminGetRolePermissionsAsync
AdminSetRolePermissionsAsync

CURRENT BEHAVIOR TO PRESERVE:

GET:

Admin panel authorization
AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", ...)
Platform owner scope + current tenant id
directory.GetRolePermissionsAsync(roleId, scope, ct)
Results.Json(...)
catches AccessControlException only
MapError semantics preserved

PUT:

Admin panel authorization
AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", ...)
Platform owner scope + current tenant id
body = List<RolePermissionGrant>
actor + X-Request-Id trace
directory.SetRolePermissionsAsync(...)
Results.NoContent()
catches AccessControlException only
MapError semantics preserved

DO NOT repair existing exception asymmetries.
Preserve behavior.

MANDATORY TARGET:

APPLICATION CQRS

Create focused real MediatR 12.5 requests, preferably:

Queries/GetRolePermissions/GetRolePermissionsQuery.cs
Commands/SetRolePermissions/SetRolePermissionsCommand.cs

No generic dispatcher.

Query/command must contain only application data:

roleId
trusted actor id where needed
tenant id
grants
trace id for mutation

Handlers may use IAccessControlDirectory.

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

GET /roles/{roleId:guid}/permissions
PUT /roles/{roleId:guid}/permissions

GET endpoint:
Admin authorization
→ accesscontrol.view capability gate
→ sender.Send(query)
→ Results.Json(...)

PUT endpoint:
Admin authorization
→ accesscontrol.manage capability gate
→ Trace(request)
→ sender.Send(command)
→ Results.NoContent()

Preserve AccessControlException-only catch behavior for these routes.

VALIDATION

This task contains meaningful user-controlled input in PUT grants.

Audit current stable validation inside IAccessControlDirectory.SetRolePermissionsAsync.

Do NOT replace stable AccessControlException codes with generic validation behavior.

Add FluentValidation ONLY for transport-invalid states that are:

not already authoritatively enforced downstream, and
can be rejected without changing existing error semantics.

Otherwise classify precisely as DOMAIN/APPLICATION-ENFORCED and do not add ceremonial validators.

GET route-id:
guid route constraint already applies.
Do not add ceremonial validator unless Guid.Empty is an accepted invalid-state invariant elsewhere.

Evidence MUST record exact validator classification.

HOST EVACUATION

After module ownership is proven, remove ONLY:

admin.MapGet("/roles/{roleId:guid}/permissions", AdminGetRolePermissionsAsync)
admin.MapPut("/roles/{roleId:guid}/permissions", AdminSetRolePermissionsAsync)

and remove ONLY:

AdminGetRolePermissionsAsync
AdminSetRolePermissionsAsync

Do not remove shared helpers still used by residual routes.

OUT OF SCOPE:

all AdminSeller role/permission routes
all Seller role/permission routes
assignments
ceiling
users/effective
scope resources
demo-preview
DevelopmentSeed
DemoSnapshot
Program.cs cleanup
Checkout
Frontend
Fulfillment
database/schema/migrations

SINGLE ROUTE OWNERSHIP AFTER CHANGE:

Exactly one production mapping each:

GET /v1/admin/access-control/roles/{roleId:guid}/permissions
PUT /v1/admin/access-control/roles/{roleId:guid}/permissions

Both module-owned.

Host mappings = ZERO.

MANDATORY AUDIT:

AdminGetRolePermissionsAsync = ZERO repo-wide
AdminSetRolePermissionsAsync = ZERO repo-wide
module GET role-permissions mapping = EXACTLY ONE
module PUT role-permissions mapping = EXACTLY ONE
requests dispatched through ISender
real IRequestHandler implementations
Endpoints direct IAccessControlDirectory for these routes = ZERO
Application -> Host = ZERO
Application -> Endpoints = ZERO

CANONICAL TASK ARTIFACT:

Commit this exact Architect-issued task at:

docs/ai/tasks/TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-PERMISSIONS-001.task.md

Do not omit it.

EVIDENCE:

Create:

docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-PERMISSIONS-001/admin-role-permissions-migration.md

Evidence must include:

before/after ownership
CQRS files
validator classification
auth parity
trace parity for PUT
response/error parity
Host removals
single-route ownership proof
residual Host role families
focused validation results

FOCUSED VALIDATION:

dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Run ONLY directly relevant focused tests if:

they already exist, or
a small validator/handler test is necessary for behavior introduced in this task.

NO solution build.
NO broad integration suite.
NO broad architecture suite.
NO retries.

RECOVERY HONESTY:

AccessControl remains IN_PROGRESS.
Host AccessControl residue remains non-zero.
Admin platform role family may be considered module-owned only after this task if no other Admin platform role routes remain.
AdminSeller and Seller role families remain out of scope.

Do NOT mark COMPLETE_REFERENCE_PATTERN.
Do NOT structure-certify AccessControl.

FINAL TRACK TARGET remains:
src/backend/Host/Tooba.Host/AccessControl = ZERO files
but NOT in this task.

PASS ONLY IF:

exactly GET/PUT Admin role-permissions routes migrated
real CQRS used
existing validation/error semantics preserved
Host handlers/mappings for only these two routes removed
canonical task artifact committed
evidence committed
focused builds succeed
no unrelated family changed
bounded scope maintained

If unexpected dependency expansion is required:
Status = INCOMPLETE
STOP.
Do not widen scope.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-PERMISSIONS-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-CLONE-ARCHIVE-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Application-CQRS-State:
Get-Role-Permissions-State:
Set-Role-Permissions-State:
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
