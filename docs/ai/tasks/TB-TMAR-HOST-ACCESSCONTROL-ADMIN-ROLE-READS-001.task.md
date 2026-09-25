PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-READS-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-PERMISSION-CATALOG-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Migrate only Admin platform role read routes to AccessControl module CQRS
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-HOST-ACCESSCONTROL-PERMISSION-CATALOG-001-R1

ACCEPTED-PARENT-COMMIT:
4e8008030c987c58d8355b45877b67281a01add8

TIMEBOX:
Target <= 10 minutes.
Hard maximum 15 minutes.
If scope cannot be completed safely inside the timebox, return INCOMPLETE and STOP.

ONE OBJECTIVE:
Evacuate ONLY the two Admin platform role READ routes from Host into AccessControl.Endpoints using real MediatR 12.5 CQRS.

ROUTES IN SCOPE ONLY:

GET /v1/admin/access-control/roles
GET /v1/admin/access-control/roles/{roleId:guid}

CURRENT HOST HANDLERS:

AdminListRolesAsync
AdminGetRoleAsync

CURRENT BEHAVIOR TO PRESERVE:

AdminListRolesAsync:

authorize with Admin panel authority
AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", ...)
owner scope = Platform + current tenant id
includeArchived query parameter semantics preserved
return directory.ListRolesAsync(...)
JSON payload shape/order unchanged

AdminGetRoleAsync:

authorize with Admin panel authority
AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", ...)
owner scope = Platform + current tenant id
directory.GetRoleAsync(...)
null => 404
AccessControlException mapping semantics unchanged

MANDATORY TARGET:

APPLICATION CQRS

Create focused queries under capability folders, e.g.:

Queries/ListRoles/
Queries/GetRole/

Use real MediatR IRequest / IRequestHandler.

Handlers may depend on IAccessControlDirectory.
Handlers must construct/use the same AccessOwnerScope semantics currently used by Host.

No HttpRequest/HttpContext/IResult in Application.
No Host dependency.
No Endpoints dependency.
No fake dispatcher.

ADMIN ENDPOINTS

Add the two mappings to:

Tooba.AccessControl.Endpoints/Admin/AccessControlAdminEndpoints.cs

Endpoint order:

IAdminPanelAccess authorization
AccessControlCapabilityGate.EnsureAsync(... "accesscontrol.view" ...)
sender.Send(...)
preserve 404/error semantics

Do not move shared error policy broadly unless strictly required for these two routes.
If a tiny endpoint-local mapper is required, keep it narrowly scoped and behavior-identical.

HOST REMOVAL

After module ownership is proven, remove ONLY:

admin.MapGet("/roles", AdminListRolesAsync)
admin.MapGet("/roles/{roleId:guid}", AdminGetRoleAsync)

and ONLY the now-dead methods:

AdminListRolesAsync
AdminGetRoleAsync

Do not touch any role mutation route.

OUT OF SCOPE — DO NOT TOUCH:

POST /v1/admin/access-control/roles
PUT /v1/admin/access-control/roles/{roleId}
POST /v1/admin/access-control/roles/{roleId}/clone
DELETE /v1/admin/access-control/roles/{roleId}
GET/PUT role permissions
all admin-seller role routes
all seller role routes
assignments
ceiling
users/effective
scope resources
demo preview
AccessControlDevelopmentSeed.cs
AccessControlDemoSnapshot.cs
Program.cs mapping cleanup
Checkout
Frontend
Fulfillment
database schema/migrations

VALIDATOR CLASSIFICATION:

ListRoles:

includeArchived is transport input.
Audit whether a validator is actually required under current TMAR transport-validation rules.
Do not add ceremonial validation for a bool if no invalid state exists.

GetRole:

route constraint already requires guid.
Do not add ceremonial validation unless Guid.Empty is explicitly invalid under existing accepted patterns.

Record exact classification in evidence.

SINGLE ROUTE OWNERSHIP AFTER CHANGE:

Exactly one production mapping for:
GET /v1/admin/access-control/roles
GET /v1/admin/access-control/roles/{roleId:guid}

Both must be module-owned.

Host mappings for these two routes = ZERO.

MANDATORY SEARCH/AUDIT:

AdminListRolesAsync = ZERO in Host
AdminGetRoleAsync = ZERO in Host
module mappings = EXACTLY ONE each
new requests sent via ISender
real IRequestHandler implementations
AccessControl.Endpoints direct IAccessControlDirectory usage for these routes = ZERO
Application -> Host = ZERO
Application -> Endpoints = ZERO

FOCUSED VALIDATION:

dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Only focused existing tests if directly relevant.
No solution build.
No broad test suite.
No retries.

EVIDENCE:

Create:
docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-READS-001/admin-role-reads-migration.md

Evidence must record:

before/after route ownership
exact CQRS files
authorization parity
response/404/error parity
validator classification
single-route-ownership proof
focused build results
residual Host AccessControl role routes

RECOVERY HONESTY:

AccessControl remains IN_PROGRESS.
Host AccessControl residue remains non-zero.
Do NOT mark COMPLETE_REFERENCE_PATTERN.
Do NOT structure-certify AccessControl.
Do NOT claim role family complete.

FINAL TRACK GOAL remains:
src/backend/Host/Tooba.Host/AccessControl = ZERO files
but that is NOT the scope of this task.

PASS ONLY IF:

both Admin role read routes are module-owned
both use ISender
real CQRS handlers exist
Host mappings/handlers for only these two routes are removed
behavior is preserved
no other route family changes
focused builds succeed
task completes within bounded scope

If unexpected dependency expansion is required:
Status = INCOMPLETE
STOP.
Do not enlarge scope.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-READS-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-PERMISSION-CATALOG-001-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Application-CQRS-State:
Admin-List-Roles-State:
Admin-Get-Role-State:
Host-Route-Residue:
Single-Route-Ownership-State:
Authorization-Parity-State:
Response-Parity-State:
Error-Parity-State:
Validator-Classification:
Application-Boundary-State:
Endpoint-Boundary-State:
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
