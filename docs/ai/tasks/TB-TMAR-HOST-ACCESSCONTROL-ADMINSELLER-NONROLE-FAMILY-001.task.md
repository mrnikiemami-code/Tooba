PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-NONROLE-FAMILY-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-SELLER-CEILING-ASSIGNMENTS-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Migrate complete AdminSeller non-role family: ceiling + assignments + effective
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-HOST-ACCESSCONTROL-SELLER-CEILING-ASSIGNMENTS-001

ACCEPTED-PARENT-COMMIT:
46c3777ca7f97e2c73a4e4c4f8b59f5982db4883

TIMEBOX:
Target <= 12 minutes.
Hard maximum 15 minutes.
If safe completion would exceed the timebox, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.

ONE OBJECTIVE:

Evacuate the complete remaining AdminSeller non-role family from Host:

GET /v1/admin/sellers/{sellerId:guid}/access-control/ceiling
PUT /v1/admin/sellers/{sellerId:guid}/access-control/ceiling
GET /v1/admin/sellers/{sellerId:guid}/access-control/assignments
POST /v1/admin/sellers/{sellerId:guid}/access-control/assignments
DELETE /v1/admin/sellers/{sellerId:guid}/access-control/assignments/{assignmentId:guid}
GET /v1/admin/sellers/{sellerId:guid}/access-control/users/{userId:guid}/effective

CURRENT HOST HANDLERS:

AdminGetCeilingAsync
AdminSetCeilingAsync
AdminSellerListAssignmentsAsync
AdminSellerAssignAsync
AdminSellerRemoveAssignmentAsync
AdminSellerEffectiveAsync

ARCHITECTURE DECISION:

Reuse the accepted CQRS wherever possible.

Already available and preferred for reuse:

GetSellerCeilingQuery
ListAssignmentsQuery
AssignRoleCommand
RemoveAssignmentCommand
GetEffectiveAccessQuery

Create ONLY the missing SetSellerCeiling command for this family.

Do NOT duplicate near-identical AdminSeller CQRS.

MANDATORY APPLICATION CQRS:

Add preferably:

Commands/SetSellerCeiling/SetSellerCeilingCommand.cs

Use real MediatR 12.5 IRequest<Unit> / IRequestHandler.

Command data:

SellerId
Entries
ActorUserId
TraceId

Use a small Application-layer input record/type for ceiling entries if needed.
Do not leak Http transport types into Application.

Handler may depend only on IAccessControlDirectory and existing AccessControl types.

APPLICATION MUST NOT depend on:
HttpRequest
HttpContext
IResult
Results.*
Host
Endpoints

ENDPOINT TARGET:

Extend existing:

src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Admin/AccessControlAdminSellerEndpoints.cs

Add all six routes above to that existing AdminSeller group.

Do NOT create another root group.

AUTHORIZATION PARITY:

All six routes use:
IAdminPanelAccess.RequireAuthorizedAsync(request, ct)

Capability gate:

GET ceiling -> accesscontrol.view
PUT ceiling -> accesscontrol.manage
GET assignments -> accesscontrol.view
POST assignments -> accesscontrol.manage
DELETE assignment -> accesscontrol.manage
GET effective -> accesscontrol.view

OWNER-SCOPE PARITY:

Assignments/effective:
AccessOwnerScopeKind.Seller
OwnerScopeId = sellerId from route
TenantId = tenant.Current?.TenantId.Value

Ceiling:
GetSellerCeilingQuery(sellerId)
SetSellerCeilingCommand(sellerId, ...)

TRACE PARITY:

Preserve X-Request-Id for:
PUT ceiling
POST assignments
DELETE assignment

GET routes carry no trace.

RESPONSE PARITY:

GET ceiling -> Results.Json(...)
PUT ceiling -> Results.NoContent()
GET assignments -> Results.Json(...)
POST assignments -> Results.Json(...)
DELETE assignment -> Results.NoContent()
GET effective -> Results.Json(...)

ERROR PARITY:

Preserve current Host behavior.

Routes with AccessControlException catch:

PUT ceiling
POST assignments
DELETE assignment

Keep same 400 / 403 escalation-or-ceiling mapping.

Read routes should preserve their existing uncaught behavior.

Do not add ex.Message classification.
Do not parse message text.
Do not normalize PlatformHttpException behavior.

CEILING INPUT PARITY:

Current Host body shape is equivalent to:
{
entries: [
{
permissionId,
enabled,
scopeKind,
scopeResourceId
}
]
}

Preserve the public JSON contract exactly.

Do not reuse private Host CeilingBody/CeilingEntry types from Endpoints.
Define endpoint-local body records if needed.

SetSellerCeilingCommand handler must invoke existing directory.SetSellerCeilingAsync semantics exactly.

VALIDATION:

GET ceiling:
NO_VALIDATOR_REQUIRED.

PUT ceiling:
Audit current SetSellerCeilingAsync authoritative validation.
Do not replace stable AccessControlException codes.
Add a focused validator only if a transport-invalid state is genuinely uncovered and parity is preserved.

GET assignments:
NO_VALIDATOR_REQUIRED.

POST assignments:
Reuse accepted AssignRoleCommand and its current classification/semantics.
Do not add ceremonial validation.

DELETE assignment:
Reuse accepted RemoveAssignmentCommand.
No ceremonial validator.

GET effective:
Reuse GetEffectiveAccessQuery.
No ceremonial validator unless an established invariant requires it.

HOST EVACUATION:

After module ownership is proven, remove ONLY these six mappings:

adminSeller.MapGet("/ceiling", AdminGetCeilingAsync)
adminSeller.MapPut("/ceiling", AdminSetCeilingAsync)
adminSeller.MapGet("/assignments", AdminSellerListAssignmentsAsync)
adminSeller.MapPost("/assignments", AdminSellerAssignAsync)
adminSeller.MapDelete("/assignments/{assignmentId:guid}", AdminSellerRemoveAssignmentAsync)
adminSeller.MapGet("/users/{userId:guid}/effective", AdminSellerEffectiveAsync)

and remove ONLY the six corresponding Host handlers.

Also remove private Host ceiling body/entry records ONLY if no Host consumer remains.

Do not remove shared helpers still used by Admin/Seller residue.

OUT OF SCOPE:

Admin platform assignments
Admin platform users/effective
Seller users/effective
Seller scope-resources
Admin scope-resources
demo-preview
DevelopmentSeed
DemoSnapshot
Program.cs final cleanup
Host folder final deletion
Checkout
Frontend
Fulfillment
schema/migrations

MANDATORY REGRESSION CHECK:

Because ListAssignmentsQuery / AssignRoleCommand / RemoveAssignmentCommand / GetEffectiveAccessQuery are shared, verify compilation and owner-scope intent remains:

Seller:

sellerId from authorized seller context
Seller owner scope

AdminSeller:

sellerId from route
Seller owner scope

Admin platform:

existing Platform/null usage remains unchanged where currently wired

Do not otherwise modify accepted Seller/Admin endpoints.

SINGLE OWNERSHIP PROOF:

After migration all six AdminSeller non-role routes above = EXACTLY ONE module mapping each.

Host mappings for those six = ZERO.

Old six Host handler names = ZERO repo-wide except historical docs/evidence.

APPLICATION BOUNDARY:

Application -> Host = ZERO
Application -> Endpoints = ZERO
No Http abstractions

ENDPOINT BOUNDARY:

All application dispatch via ISender.
No direct IAccessControlDirectory.
No Infrastructure/DbContext.
No Host namespace/types.

CANONICAL TASK ARTIFACT:

Commit this exact Architect-issued task at:

docs/ai/tasks/TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-NONROLE-FAMILY-001.task.md

Do not omit it.

EVIDENCE:

Create:

docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-NONROLE-FAMILY-001/adminseller-nonrole-family-migration.md

Evidence must include:

six before/after route ownerships
CQRS reuse/new-command summary
auth parity
owner-scope parity
trace parity
response/error parity
ceiling JSON contract parity
validator classification
Host removals
single-route ownership proof
residual Host AccessControl families

FOCUSED VALIDATION ONLY:

dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Run ONLY directly relevant focused tests if they already exist or if a tiny SetSellerCeiling handler/validator test is strictly necessary.

NO solution build.
NO broad integration suite.
NO broad architecture suite.
NO retries.

RECOVERY HONESTY:

AccessControl remains IN_PROGRESS.
Host AccessControl residue remains non-zero.
Do NOT mark COMPLETE_REFERENCE_PATTERN.
Do NOT structure-certify AccessControl.

If this task passes:

AdminSeller route family has no Host-owned routes left.
Seller role + ceiling + assignments are module-owned.
Remaining Host work is primarily Admin platform assignments/users/effective, Seller users/effective, scope-resources, demo/seed/snapshot, and final Host cleanup.

FINAL TRACK TARGET:
src/backend/Host/Tooba.Host/AccessControl = ZERO files
but NOT in this task.

PASS ONLY IF:

all six AdminSeller non-role routes migrate
existing shared CQRS is reused where applicable
only SetSellerCeiling CQRS is added if missing
auth/owner-scope/trace/response/error/json-contract parity preserved
Host mappings/handlers for only this family removed
canonical task/evidence committed
focused builds succeed
no unrelated family changed
hard timebox respected

If safe completion exceeds hard limit:
Status = INCOMPLETE
STOP IMMEDIATELY.
Do not loop.
Do not broaden scope.
Do not auto-start another task.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-NONROLE-FAMILY-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-SELLER-CEILING-ASSIGNMENTS-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
CQRS-Reuse-State:
Set-Seller-Ceiling-CQRS-State:
AdminSeller-Ceiling-State:
AdminSeller-Assignments-State:
AdminSeller-Effective-State:
Authorization-Parity-State:
Owner-Scope-Parity-State:
Trace-Parity-State:
Response-Parity-State:
Error-Parity-State:
Ceiling-Json-Contract-State:
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
