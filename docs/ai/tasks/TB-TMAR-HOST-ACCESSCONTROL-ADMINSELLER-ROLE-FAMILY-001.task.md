PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-ROLE-FAMILY-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-PERMISSIONS-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Migrate complete AdminSeller role + role-permissions family to module CQRS
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-PERMISSIONS-001

ACCEPTED-PARENT-COMMIT:
41ba084d7a8e86cd822eb957527646af8eb78c4b

TIMEBOX:
Target <= 12 minutes.
Hard maximum 15 minutes.
If safe completion would exceed the timebox, return INCOMPLETE and STOP.
Do not enter a retry loop.

ONE OBJECTIVE:

Evacuate the complete AdminSeller role + role-permissions family from Host:

GET /v1/admin/sellers/{sellerId:guid}/access-control/roles
POST /v1/admin/sellers/{sellerId:guid}/access-control/roles
PUT /v1/admin/sellers/{sellerId:guid}/access-control/roles/{roleId:guid}
POST /v1/admin/sellers/{sellerId:guid}/access-control/roles/{roleId:guid}/clone
DELETE /v1/admin/sellers/{sellerId:guid}/access-control/roles/{roleId:guid}
GET /v1/admin/sellers/{sellerId:guid}/access-control/roles/{roleId:guid}/permissions
PUT /v1/admin/sellers/{sellerId:guid}/access-control/roles/{roleId:guid}/permissions

CURRENT HOST HANDLERS:

AdminSellerListRolesAsync
AdminSellerCreateRoleAsync
AdminSellerUpdateRoleAsync
AdminSellerCloneRoleAsync
AdminSellerArchiveRoleAsync
AdminSellerGetPermsAsync
AdminSellerSetPermsAsync

ARCHITECTURE DECISION:

Reuse/generalize the already accepted Role CQRS where cleanly possible.

Existing accepted requests currently include:

ListRolesQuery already supports OwnerScopeKind/OwnerScopeId
GetRoleQuery already supports OwnerScopeKind/OwnerScopeId
CreateRoleCommand / UpdateRoleCommand / CloneRoleCommand / ArchiveRoleCommand currently hardcode Platform
GetRolePermissionsQuery / SetRolePermissionsCommand currently hardcode Platform

Preferred bounded approach:

Generalize existing role mutation / permission requests so they carry:

AccessOwnerScopeKind OwnerScopeKind
Guid? OwnerScopeId
string? TenantId

and construct AccessOwnerScope from those values.

Then:

existing Admin platform endpoints explicitly pass Platform/null
new AdminSeller endpoints pass Seller/sellerId

Do NOT duplicate near-identical AdminSeller CQRS files if generalizing the accepted requests is smaller and cleaner.

Do NOT change semantics for already accepted Admin platform routes.

MANDATORY BEHAVIOR PARITY:

AdminSeller authorization:

use IAdminPanelAccess
capability gate:
view for GET list / GET permissions
manage for create/update/clone/archive/PUT permissions

Owner scope:
AccessOwnerScopeKind.Seller
OwnerScopeId = sellerId
TenantId = tenant.Current?.TenantId.Value

Trace:
Preserve X-Request-Id forwarding for create/update/clone/archive/set-permissions.

Responses:

List -> Results.Json(list)
Create -> Results.Json(role)
Update -> Results.Json(role)
Clone -> Results.Json(role)
Archive -> Results.NoContent()
Get permissions -> Results.Json(grants)
Set permissions -> Results.NoContent()

Error handling:
Preserve current AdminSeller Host behavior:

AccessControlException-only catch where currently present
existing MapError semantics
do NOT add ex.Message classification
do NOT normalize unrelated PlatformHttpException asymmetry in this task

ENDPOINT TARGET:

Add an AdminSeller mapping surface owned by AccessControl.Endpoints.

Preferred structure:
Tooba.AccessControl.Endpoints/Admin/...

You may add a focused AdminSeller endpoint class if that keeps route ownership clear.

The module root must map:

/v1/admin/sellers/{sellerId:guid}/access-control

exactly once.

Do not duplicate route groups.

HOST EVACUATION:

After module ownership is proven, remove ONLY the seven AdminSeller role/permission mappings and their seven handlers.

Do NOT touch AdminSeller:

ceiling
assignments
users/{userId}/effective

Do NOT touch Seller role family in this task.

OUT OF SCOPE:

AdminSeller ceiling
AdminSeller assignments
AdminSeller effective access
all Seller role routes
Seller ceiling
Seller assignments/users/effective
Admin platform assignments/users/effective
scope resources
demo-preview
DevelopmentSeed
DemoSnapshot
Program.cs final cleanup
Checkout
Frontend
Fulfillment
schema/migrations

VALIDATION:

Reuse current accepted validation behavior.

Do not add ceremonial validators.
Do not replace stable AccessControlException codes.

Only add/modify a focused validator if generalizing an existing request introduces a genuinely new invalid transport state.

Otherwise record:
DOMAIN/APPLICATION-ENFORCED or NO_VALIDATOR_REQUIRED as applicable.

MANDATORY REGRESSION CHECK:

Because existing Admin platform CQRS may be generalized, verify that these accepted Admin routes still compile and retain Platform/null owner scope:

GET /v1/admin/access-control/roles
GET /v1/admin/access-control/roles/{roleId}
POST /v1/admin/access-control/roles
PUT /v1/admin/access-control/roles/{roleId}
POST /v1/admin/access-control/roles/{roleId}/clone
DELETE /v1/admin/access-control/roles/{roleId}
GET /v1/admin/access-control/roles/{roleId}/permissions
PUT /v1/admin/access-control/roles/{roleId}/permissions

Do not otherwise modify them.

SINGLE OWNERSHIP PROOF:

After migration:
all seven AdminSeller role/permission routes = EXACTLY ONE module mapping each
Host mappings for these seven routes = ZERO
all seven old Host handler names = ZERO repo-wide

APPLICATION BOUNDARY:

No HttpRequest
No HttpContext
No IResult
No Results.*
No Host dependency
No Endpoints dependency

ENDPOINT BOUNDARY:

Use ISender.
No direct IAccessControlDirectory.
No Infrastructure/DbContext.
No Host namespace/types.

CANONICAL TASK ARTIFACT:

Commit this exact Architect-issued task at:

docs/ai/tasks/TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-ROLE-FAMILY-001.task.md

Do not omit it.

EVIDENCE:

Create:

docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-ROLE-FAMILY-001/adminseller-role-family-migration.md

Evidence must include:

all seven before/after route ownerships
whether accepted CQRS was generalized or reused
Admin platform regression/parity statement
owner-scope parity
auth/capability parity
trace parity
response/error parity
validator classification
Host removals
single ownership proof
residual Host AccessControl families

FOCUSED VALIDATION ONLY:

dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Run only directly relevant existing focused tests if they cover generalized CQRS owner-scope semantics.
Do not create a broad test suite.
No solution build.
No broad integration suite.
No broad architecture suite.
No retries.

RECOVERY HONESTY:

AccessControl remains IN_PROGRESS.
Host AccessControl residue remains non-zero.
Do NOT mark COMPLETE_REFERENCE_PATTERN.
Do NOT structure-certify AccessControl.

If this task passes:
Admin platform Role family = module-owned
AdminSeller Role family = module-owned
Seller Role family still remains in Host

FINAL TRACK TARGET:
src/backend/Host/Tooba.Host/AccessControl = ZERO files
but NOT in this task.

PASS ONLY IF:

all seven AdminSeller role/permission routes migrate
existing Admin platform route semantics stay intact
real CQRS/ISender used
Host handlers/mappings for only this family removed
no unrelated AccessControl family changed
canonical task/evidence committed
focused builds succeed
task remains within hard timebox

If full family cannot be safely completed by the hard limit:
Status = INCOMPLETE
STOP IMMEDIATELY.
Do not continue looping.
Do not silently split or start another task.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-ROLE-FAMILY-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-PERMISSIONS-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
CQRS-Reuse-Generalization-State:
AdminSeller-Role-Route-State:
AdminSeller-RolePermissions-State:
Admin-Platform-Regression-State:
Owner-Scope-Parity-State:
Authorization-Parity-State:
Trace-Parity-State:
Response-Parity-State:
Error-Parity-State:
Validator-Classification:
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
Residual-Host-AccessControl-Families:
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
