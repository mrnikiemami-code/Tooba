PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-SELLER-ROLE-FAMILY-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-ROLE-FAMILY-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Migrate complete Seller role + role-permissions family to module CQRS
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-ROLE-FAMILY-001

ACCEPTED-PARENT-COMMIT:
51bb762bc6496ea4e018725d3e1d579f81add847

TIMEBOX:
Target <= 12 minutes.
Hard maximum 15 minutes.
If safe completion would exceed the timebox, return INCOMPLETE and STOP.
No retry loop.

ONE OBJECTIVE:

Evacuate the complete Seller role + role-permissions family from Host:

GET /v1/seller/access-control/roles
POST /v1/seller/access-control/roles
GET /v1/seller/access-control/roles/{roleId:guid}
PUT /v1/seller/access-control/roles/{roleId:guid}
POST /v1/seller/access-control/roles/{roleId:guid}/clone
DELETE /v1/seller/access-control/roles/{roleId:guid}
GET /v1/seller/access-control/roles/{roleId:guid}/permissions
PUT /v1/seller/access-control/roles/{roleId:guid}/permissions

CURRENT HOST HANDLERS:

SellerListRolesAsync
SellerCreateRoleAsync
SellerGetRoleAsync
SellerUpdateRoleAsync
SellerCloneRoleAsync
SellerArchiveRoleAsync
SellerGetRolePermissionsAsync
SellerSetRolePermissionsAsync

ARCHITECTURE DECISION:

Reuse the already generalized accepted Role CQRS.
Do NOT create Seller-specific duplicate commands/queries unless a compile-only necessity proves reuse impossible.

Existing generalized requests already support:
AccessOwnerScopeKind
OwnerScopeId
TenantId

Seller endpoints must pass:
AccessOwnerScopeKind.Seller
OwnerScopeId = sellerId from ISellerPanelAccess
TenantId = tenant.Current?.TenantId.Value

MANDATORY AUTHORIZATION PARITY:

Use ISellerPanelAccess.RequireAuthorizedAsync(request, ct) to obtain:
(actor, sellerId)

Then capability gate:

accesscontrol.view for GET list / GET role / GET permissions
accesscontrol.manage for POST create / PUT update / POST clone / DELETE archive / PUT permissions

Do not reintroduce Host SellerPanelAccess static helper.
Do not depend on Host.

TRACE PARITY:

Preserve X-Request-Id forwarding for:
create
update
clone
archive
set-permissions

GET routes carry no trace, matching Host.

RESPONSE PARITY:

GET list -> Results.Json(list)
POST create -> Results.Json(role)
GET role -> role null ? Results.NotFound() : Results.Json(role)
PUT update -> Results.Json(role)
POST clone -> Results.Json(role)
DELETE archive -> Results.NoContent()
GET permissions -> Results.Json(grants)
PUT permissions -> Results.NoContent()

ERROR PARITY:

Preserve current Seller Host behavior:

AccessControlException-only catch where currently present
existing 400 / 403 escalation-or-ceiling mapping
PlatformHttpException from capability gate remains uncaught if that is the existing Host behavior
no ex.Message classification
no message parsing
no broad error-policy normalization

MODULE ENDPOINT TARGET:

Extend the existing:

Tooba.AccessControl.Endpoints/Seller/AccessControlSellerEndpoints.cs

Map exactly the eight Seller role/permission routes above.

Do not add another /v1/seller/access-control root group.
The module root already owns that group.

CQRS REUSE:

Use accepted requests where applicable:

ListRolesQuery
GetRoleQuery
CreateRoleCommand
UpdateRoleCommand
CloneRoleCommand
ArchiveRoleCommand
GetRolePermissionsQuery
SetRolePermissionsCommand

Do not create a generic dispatcher.
Do not move IAccessControlDirectory into Endpoints.

HOST EVACUATION:

After module ownership is proven, remove ONLY the eight Seller role/permission route mappings and the eight handler bodies listed above.

Do not remove RequireSellerAsync yet if residual Seller routes still consume it.

OUT OF SCOPE:

Seller ceiling
Seller assignments
Seller users/effective
Seller scope resources
AdminSeller ceiling/assignments/effective
Admin platform assignments/users/effective
demo-preview
DevelopmentSeed
DemoSnapshot
Program.cs final cleanup
Checkout
Frontend
Fulfillment
schema/migrations

VALIDATION:

Reuse existing accepted validation and stable AccessControlException codes.

Do not add ceremonial validators.
Only add a focused validator if this Seller mapping introduces a genuinely new invalid transport state not already enforced authoritatively.

Expected classifications:

ListRoles: NO_VALIDATOR_REQUIRED
GetRole: NO_VALIDATOR_REQUIRED unless existing accepted invariant says otherwise
Create/Update/Clone/SetPermissions: DOMAIN/APPLICATION-ENFORCED unless a real uncovered transport invalid state is found
Archive: NO_VALIDATOR_REQUIRED / DOMAIN-ENFORCED

Record exact classification in evidence.

MANDATORY REGRESSION CHECK:

Because CQRS is shared across Admin/AdminSeller/Seller, verify compilation and owner-scope intent remains:

Admin platform -> Platform/null
AdminSeller -> Seller/route sellerId
Seller -> Seller/authorized sellerId

Do not otherwise alter accepted Admin/AdminSeller endpoints.

SINGLE OWNERSHIP PROOF:

After migration:
all eight Seller role/permission routes = EXACTLY ONE module mapping each
Host mappings for these eight routes = ZERO
all eight old Host handler names = ZERO repo-wide except historical docs/evidence

APPLICATION BOUNDARY:

No HttpRequest
No HttpContext
No IResult
No Results.*
No Host dependency
No Endpoints dependency

ENDPOINT BOUNDARY:

ISender only for application dispatch.
No direct IAccessControlDirectory.
No Infrastructure/DbContext.
No Host namespace/types.

CANONICAL TASK ARTIFACT:

Commit this exact Architect-issued task at:

docs/ai/tasks/TB-TMAR-HOST-ACCESSCONTROL-SELLER-ROLE-FAMILY-001.task.md

Do not omit it.

EVIDENCE:

Create:

docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-SELLER-ROLE-FAMILY-001/seller-role-family-migration.md

Evidence must include:

all eight before/after route ownerships
CQRS reuse statement
Seller auth parity
owner-scope parity
trace parity
response/error parity
validator classification
Admin/AdminSeller regression statement
Host removals
single ownership proof
residual Host AccessControl families

FOCUSED VALIDATION ONLY:

dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Run only directly relevant focused tests if they already cover shared owner-scope CQRS semantics.
Do not create broad tests.
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
Seller Role family = module-owned

Remaining Host AccessControl work is then non-role families.

FINAL TRACK TARGET:
src/backend/Host/Tooba.Host/AccessControl = ZERO files
but NOT in this task.

PASS ONLY IF:

all eight Seller role/permission routes migrate
existing Admin/AdminSeller role behavior remains intact
real CQRS/ISender is used
Host handlers/mappings for only this family are removed
no unrelated family changes
canonical task/evidence committed
focused builds succeed
task stays within hard timebox

If full family cannot safely complete by hard limit:
Status = INCOMPLETE
STOP IMMEDIATELY.
Do not loop.
Do not silently split.
Do not start another task.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-SELLER-ROLE-FAMILY-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-ROLE-FAMILY-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
CQRS-Reuse-State:
Seller-Role-Route-State:
Seller-RolePermissions-State:
Admin-Regression-State:
AdminSeller-Regression-State:
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
