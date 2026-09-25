PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-ADMINPLATFORM-ASSIGNMENTS-EFFECTIVE-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-NONROLE-FAMILY-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Migrate Admin platform assignments + effective routes to module CQRS
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-NONROLE-FAMILY-001

ACCEPTED-PARENT-COMMIT:
b9dda0d7b29906a63378b5f6eae6cdbba190d66b

TIMEBOX:
Target <= 12 minutes.
Hard maximum 15 minutes.
If safe completion would exceed the timebox, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.

ONE OBJECTIVE:

Evacuate ONLY these four Admin platform routes from Host:

GET /v1/admin/access-control/assignments
POST /v1/admin/access-control/assignments
DELETE /v1/admin/access-control/assignments/{assignmentId:guid}
GET /v1/admin/access-control/users/{userId:guid}/effective

CURRENT HOST HANDLERS:

AdminListAssignmentsAsync
AdminAssignAsync
AdminRemoveAssignmentAsync
AdminEffectiveAsync

ARCHITECTURE DECISION:

Reuse already accepted CQRS:

ListAssignmentsQuery
AssignRoleCommand
RemoveAssignmentCommand
GetEffectiveAccessQuery

Do NOT create duplicate Admin-specific requests.

Admin platform owner scope must remain:

AccessOwnerScopeKind.Platform
OwnerScopeId = null
TenantId = tenant.Current?.TenantId.Value

MANDATORY AUTHORIZATION PARITY:

All four routes use:
IAdminPanelAccess.RequireAuthorizedAsync(request, ct)

Capability gate:

GET assignments -> accesscontrol.view
POST assignments -> accesscontrol.manage
DELETE assignment -> accesscontrol.manage
GET effective -> accesscontrol.view

TRACE PARITY:

Preserve X-Request-Id for:
POST assignments
DELETE assignment

GET routes carry no trace.

RESPONSE PARITY:

GET assignments -> Results.Json(...)
POST assignments -> Results.Json(...)
DELETE assignment -> Results.NoContent()
GET effective -> Results.Json(...)

ERROR PARITY:

Preserve current Host behavior exactly:

POST assignments:

catch AccessControlException only
map 403 when Code contains escalation/ceiling, otherwise 400

DELETE assignment:

same AccessControlException-only behavior

GET assignments/effective:

preserve existing uncaught behavior

No ex.Message classification.
No message parsing.
Do not normalize PlatformHttpException behavior.

ENDPOINT TARGET:

Extend existing:

src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Admin/AccessControlAdminEndpoints.cs

Add exactly the four routes above.

All application dispatch via ISender.

No direct IAccessControlDirectory.

HOST EVACUATION:

After module ownership is proven, remove ONLY:

admin.MapGet("/assignments", AdminListAssignmentsAsync)
admin.MapPost("/assignments", AdminAssignAsync)
admin.MapDelete("/assignments/{assignmentId:guid}", AdminRemoveAssignmentAsync)
admin.MapGet("/users/{userId:guid}/effective", AdminEffectiveAsync)

and remove ONLY the four corresponding Host handlers.

Remove private AssignBody from Host ONLY if no Host consumer remains.

Do not remove shared helpers still used by residual Host routes.

OUT OF SCOPE:

GET /v1/admin/access-control/users
Seller users/effective
Admin/Seller scope-resources
demo-preview
DevelopmentSeed
DemoSnapshot
Program.cs final cleanup
Host folder final deletion
Checkout
Frontend
Fulfillment
schema/migrations

IMPORTANT BOUNDARY HOLD:

Do NOT touch AdminSearchUsersAsync in this task.

It currently uses cross-module Identity/OperatorProfile enrichment and requires a dedicated boundary/seam decision.
Do not opportunistically move those foreign Application dependencies into AccessControl.Application or Endpoints.

VALIDATION:

Reuse existing accepted request semantics.

GET assignments:
NO_VALIDATOR_REQUIRED.

POST assignments:
reuse accepted AssignRoleCommand classification.
Do not add ceremonial validation.

DELETE assignment:
reuse accepted RemoveAssignmentCommand classification.

GET effective:
reuse GetEffectiveAccessQuery; no ceremonial validator.

MANDATORY REGRESSION CHECK:

Because CQRS is shared:

Admin platform uses Platform/null
AdminSeller remains Seller/route sellerId
Seller remains Seller/authorized sellerId

Do not otherwise modify accepted AdminSeller/Seller endpoints.

SINGLE OWNERSHIP PROOF:

After migration:

GET /v1/admin/access-control/assignments = EXACTLY ONE module mapping
POST /v1/admin/access-control/assignments = EXACTLY ONE module mapping
DELETE /v1/admin/access-control/assignments/{assignmentId:guid} = EXACTLY ONE module mapping
GET /v1/admin/access-control/users/{userId:guid}/effective = EXACTLY ONE module mapping

Host mappings = ZERO.

Old four Host handler names = ZERO repo-wide except historical docs/evidence.

APPLICATION BOUNDARY:

No new Application dependencies are expected.
Application -> Host = ZERO
Application -> Endpoints = ZERO

ENDPOINT BOUNDARY:

ISender only.
No direct IAccessControlDirectory.
No Infrastructure/DbContext.
No Host namespace/types.

CANONICAL TASK ARTIFACT:

Commit this exact Architect-issued task at:

docs/ai/tasks/TB-TMAR-HOST-ACCESSCONTROL-ADMINPLATFORM-ASSIGNMENTS-EFFECTIVE-001.task.md

Do not omit it.

EVIDENCE:

Create:

docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-ADMINPLATFORM-ASSIGNMENTS-EFFECTIVE-001/adminplatform-assignments-effective-migration.md

Evidence must include:

four before/after route ownerships
CQRS reuse
auth parity
Platform owner-scope parity
trace parity
response/error parity
validator classification
Host removals
single-route ownership proof
confirmation AdminSearchUsersAsync untouched
residual Host AccessControl families

FOCUSED VALIDATION ONLY:

dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Run ONLY directly relevant existing focused tests if they cover these shared requests.
Do not create broad tests.
No solution build.
No broad integration suite.
No broad architecture suite.
No retries.

RECOVERY HONESTY:

AccessControl remains IN_PROGRESS.
Host AccessControl residue remains non-zero.

After this task, Admin platform assignment/effective routes should be module-owned.
AdminSearchUsersAsync remains intentionally Host-owned pending a dedicated cross-module enrichment seam task.

Do NOT mark COMPLETE_REFERENCE_PATTERN.
Do NOT structure-certify AccessControl.

FINAL TRACK TARGET:
src/backend/Host/Tooba.Host/AccessControl = ZERO files
but NOT in this task.

PASS ONLY IF:

exactly these four Admin platform routes migrate
shared CQRS is reused
Platform/null owner scope preserved
auth/trace/response/error parity preserved
AdminSearchUsersAsync untouched
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
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-ADMINPLATFORM-ASSIGNMENTS-EFFECTIVE-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-NONROLE-FAMILY-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
CQRS-Reuse-State:
Admin-Assignments-State:
Admin-Effective-State:
Authorization-Parity-State:
Owner-Scope-Parity-State:
Trace-Parity-State:
Response-Parity-State:
Error-Parity-State:
Validator-Classification:
Admin-SearchUsers-Hold-State:
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
