PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-SELLER-CEILING-ASSIGNMENTS-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-SELLER-ROLE-FAMILY-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Migrate Seller ceiling + assignments family to module CQRS
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-HOST-ACCESSCONTROL-SELLER-ROLE-FAMILY-001

ACCEPTED-PARENT-COMMIT:
284a3eb09212d696288d71c1739f7a9c419acd8e

TIMEBOX:
Target <= 12 minutes.
Hard maximum 15 minutes.
If safe completion would exceed the timebox, return INCOMPLETE and STOP.
No retry loop.

ONE OBJECTIVE:

Evacuate ONLY these four Seller routes from Host:

GET /v1/seller/access-control/ceiling
GET /v1/seller/access-control/assignments
POST /v1/seller/access-control/assignments
DELETE /v1/seller/access-control/assignments/{assignmentId:guid}

CURRENT HOST HANDLERS:

SellerGetCeilingAsync
SellerListAssignmentsAsync
SellerAssignAsync
SellerRemoveAssignmentAsync

CURRENT BEHAVIOR TO PRESERVE:

SellerGetCeilingAsync:

seller authorization
actor + sellerId from current seller context
accesscontrol.view capability gate
directory.GetSellerCeilingAsync(sellerId, ct)
Results.Json(...)

SellerListAssignmentsAsync:

seller authorization
accesscontrol.view capability gate
owner scope = Seller + sellerId + current tenant
directory.ListAssignmentsAsync(scope, null, ct)
Results.Json(...)

SellerAssignAsync:

seller authorization
accesscontrol.manage capability gate
owner scope = Seller + sellerId + current tenant
body = UserId + RoleId
actor + X-Request-Id trace
directory.AssignRoleAsync(...)
Results.Json(...)
AccessControlException-only catch
preserve MapError semantics

SellerRemoveAssignmentAsync:

seller authorization
accesscontrol.manage capability gate
owner scope = Seller + sellerId + current tenant
assignmentId from route
actor + X-Request-Id trace
directory.RemoveAssignmentAsync(...)
Results.NoContent()
AccessControlException-only catch
preserve MapError semantics

MANDATORY APPLICATION CQRS:

Create only the requests required for this family, preferably:

Queries/GetSellerCeiling/GetSellerCeilingQuery.cs
Queries/ListAssignments/ListAssignmentsQuery.cs
Commands/AssignRole/AssignRoleCommand.cs
Commands/RemoveAssignment/RemoveAssignmentCommand.cs

Use real MediatR 12.5 IRequest/IRequestHandler.

No generic dispatcher.

Preferred request design:

trusted sellerId / owner scope inputs
trusted actor id for mutations
tenant id
body/route values
trace id for mutations

Handlers may depend on IAccessControlDirectory.

APPLICATION MUST NOT depend on:
HttpRequest
HttpContext
IResult
Results.*
Host
Endpoints

ENDPOINT TARGET:

Extend existing:

src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Seller/AccessControlSellerEndpoints.cs

Use:
ISellerPanelAccess.RequireAuthorizedAsync(request, ct)

Do NOT use Host RequireSellerAsync.
Do NOT depend on Host.

Capability gate:
GET ceiling -> accesscontrol.view
GET assignments -> accesscontrol.view
POST assignments -> accesscontrol.manage
DELETE assignment -> accesscontrol.manage

All application calls MUST go through ISender.

No direct IAccessControlDirectory from Endpoints.

TRACE PARITY:

Preserve X-Request-Id for:
POST assignments
DELETE assignment

GET routes carry no trace.

ERROR PARITY:

For POST/DELETE preserve AccessControlException-only catch semantics.
Preserve existing 400 / 403 escalation-or-ceiling mapping.
Do not add ex.Message classification.
Do not parse message text.
Do not normalize PlatformHttpException behavior.

VALIDATION:

GET ceiling:
NO_VALIDATOR_REQUIRED.

GET assignments:
NO_VALIDATOR_REQUIRED.

POST assignments:
UserId and RoleId are transport input.
Audit downstream authoritative checks.
Add a focused FluentValidation validator only if there is a real transport-invalid state that is not already authoritatively rejected with stable AccessControlException codes.
Do not replace stable error semantics.

DELETE assignment:
route has guid constraint.
Do not add ceremonial validator unless Guid.Empty is an established invalid invariant.

Record exact classification.

HOST EVACUATION:

After module ownership is proven, remove ONLY:

seller.MapGet("/ceiling", SellerGetCeilingAsync)
seller.MapGet("/assignments", SellerListAssignmentsAsync)
seller.MapPost("/assignments", SellerAssignAsync)
seller.MapDelete("/assignments/{assignmentId:guid}", SellerRemoveAssignmentAsync)

and remove ONLY the four corresponding Host handlers.

Keep RequireSellerAsync if residual Seller routes still use it.
Keep shared Host helpers if still used by residual routes.

OUT OF SCOPE:

Seller users/effective
Seller scope-resources
AdminSeller ceiling
AdminSeller assignments
AdminSeller effective
Admin platform assignments/users/effective
demo-preview
DevelopmentSeed
DemoSnapshot
Program.cs final cleanup
Checkout
Frontend
Fulfillment
schema/migrations

SINGLE OWNERSHIP PROOF:

After migration:

GET /v1/seller/access-control/ceiling = EXACTLY ONE module mapping
GET /v1/seller/access-control/assignments = EXACTLY ONE module mapping
POST /v1/seller/access-control/assignments = EXACTLY ONE module mapping
DELETE /v1/seller/access-control/assignments/{assignmentId:guid} = EXACTLY ONE module mapping

Host mappings = ZERO.
Old four Host handler names = ZERO repo-wide except historical docs/evidence.

BOUNDARY AUDIT:

Application -> Host = ZERO
Application -> Endpoints = ZERO
Endpoints direct IAccessControlDirectory = ZERO
Endpoints Infrastructure/DbContext = ZERO
Host types in Endpoints = ZERO

CANONICAL TASK ARTIFACT:

Commit this exact Architect-issued task at:

docs/ai/tasks/TB-TMAR-HOST-ACCESSCONTROL-SELLER-CEILING-ASSIGNMENTS-001.task.md

Do not omit it.

EVIDENCE:

Create:

docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-SELLER-CEILING-ASSIGNMENTS-001/seller-ceiling-assignments-migration.md

Evidence must include:

four before/after route ownerships
CQRS files
auth parity
owner-scope parity
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

Run ONLY directly relevant focused tests if they already exist or a tiny validator test is strictly necessary.

NO solution build.
NO broad integration suite.
NO broad architecture suite.
NO retries.

RECOVERY HONESTY:

AccessControl remains IN_PROGRESS.
Host AccessControl residue remains non-zero.
Do NOT mark COMPLETE_REFERENCE_PATTERN.
Do NOT structure-certify AccessControl.

FINAL TRACK TARGET:
src/backend/Host/Tooba.Host/AccessControl = ZERO files
but NOT in this task.

PASS ONLY IF:

exactly these four Seller routes migrate
real CQRS/ISender used
auth/owner-scope/trace/response/error behavior preserved
Host handlers/mappings for only this family removed
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
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-SELLER-CEILING-ASSIGNMENTS-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-SELLER-ROLE-FAMILY-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Application-CQRS-State:
Seller-Ceiling-State:
Seller-Assignments-State:
Authorization-Parity-State:
Owner-Scope-Parity-State:
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
