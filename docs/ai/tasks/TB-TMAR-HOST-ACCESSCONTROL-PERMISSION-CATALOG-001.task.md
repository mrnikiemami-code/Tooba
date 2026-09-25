PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-PERMISSION-CATALOG-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Migrate AccessControl permission-catalog HTTP ownership from Host to module CQRS
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001-R1

ACCEPTED-PARENT-COMMIT:
12cc5a3a2589fcd4bb7302a13a00af786e97b67c

ADOPTED-INTERMEDIATE-COMMIT:
f059c82544c9710c6beb8301f51be2d41930c767

ARCHITECTURE DECISION:

The capability-gate relocation is canonically accepted.
AccessControlCapabilityGate remains owned by AccessControl.Application/Authorization.

PermissionCatalog itself is already correctly owned by AccessControl.Application and is consumed by
AccessControl.Infrastructure for bootstrap, ceiling, grant validation, effective-access composition
and authorization tuple synchronization.

DO NOT relocate PermissionCatalog.
DO NOT create a duplicate catalog.
DO NOT create a compatibility facade.
DO NOT move unrelated Role / Assignment / Ceiling / User / Scope Resource routes in this task.

CURRENT MODULE-OWNED ACCEPTED ROUTES:

POST /v1/admin/access-control/bootstrap
GET /v1/admin/access-control/me/capabilities
GET /v1/seller/access-control/me/capabilities

ONE OBJECTIVE:

Evacuate only the Permission Catalog HTTP family from Host into AccessControl.Endpoints and route
both endpoints through real MediatR 12.5 CQRS.

ROUTES IN SCOPE:

GET /v1/admin/access-control/permissions
GET /v1/seller/access-control/permissions

CURRENT HOST OWNERSHIP:

src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs

Admin:
AdminListCatalogAsync
→ AdminPanelAccess.RequireAuthorizedAsync(...)
→ AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", ...)
→ IAccessControlDirectory.ListCatalog()

Seller:
SellerListCatalogAsync
→ seller panel authorization
→ AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", ...)
→ IAccessControlDirectory.ListCatalog()

MANDATORY TARGET ARCHITECTURE:

APPLICATION CQRS

Create a focused query under:

src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Queries/ListPermissionCatalog/

Preferred type:

ListPermissionCatalogQuery

It MUST be a real MediatR IRequest handled by IRequestHandler.

Handler MUST use the existing AccessControl authority.
Preferred authority call:

IAccessControlDirectory.ListCatalog()

Do not reproduce PermissionCatalog.All projection in Endpoints.
Do not duplicate permission metadata.
Do not introduce a generic dispatcher.

ADMIN ENDPOINT

Extend:

src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Admin/AccessControlAdminEndpoints.cs

Map exactly:

GET /permissions

Required behavior order:

a. authorize through IAdminPanelAccess
b. apply AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", ...)
c. sender.Send(ListPermissionCatalogQuery)
d. Results.Json(...)

Preserve current authorization behavior exactly.

SELLER ENDPOINT

Extend:

src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Seller/AccessControlSellerEndpoints.cs

Map exactly:

GET /permissions

Required behavior order:

a. authorize through ISellerPanelAccess
b. preserve seller actor/seller resolution semantics
c. apply AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", ...)
d. sender.Send(ListPermissionCatalogQuery)
e. Results.Json(...)

No seller-specific catalog filtering may be invented unless it already exists in current Host behavior.
Current response parity is authoritative.

HOST EVACUATION

After module routes are proven:

Remove only these Host mappings:

admin.MapGet("/permissions", AdminListCatalogAsync)
seller.MapGet("/permissions", SellerListCatalogAsync)

Remove only the now-dead Host handlers:

AdminListCatalogAsync
SellerListCatalogAsync

Do not remove shared helpers still consumed by other Host AccessControl routes.

Do not migrate any additional route.

SINGLE ROUTE OWNERSHIP

After migration there must be exactly one production mapping for:

GET /v1/admin/access-control/permissions
GET /v1/seller/access-control/permissions

Those mappings must be module-owned by AccessControl.Endpoints.

Host must not map either route.

CQRS / ENDPOINT BOUNDARY

Endpoints may use:

MediatR ISender
AccessControl.Application
BuildingBlocks shared abstractions
module endpoint security seams already accepted

Endpoints must NOT directly use:

IAccessControlDirectory for the permission route
PermissionCatalog.All
AccessControl.Infrastructure
DbContext
Host namespace/types

APPLICATION BOUNDARY

The new query/handler must contain:

NO HttpContext
NO HttpRequest
NO IResult
NO Results.*
NO Tooba.Host.*
NO Tooba.AccessControl.Endpoints dependency

SEMANTIC PARITY

Preserve:

response payload shape
permission ordering
PermissionId
Module
DisplayNameKey
DescriptionKey
Delegable
ScopeKinds
authorization ordering
accesscontrol.view capability-gate behavior
HTTP status behavior

Do not "fix" fail-open accesscontrol.view/manage policy here.
That is explicitly outside this task.

VALIDATORS

Audit whether ListPermissionCatalogQuery has transport input.

Expected classification:
NO_VALIDATOR_REQUIRED if it contains no user-controlled transport input.

Do not add ceremonial validators.

Record the classification in evidence.

PROTECTED STATE

DO NOT TOUCH:

AccessControl role routes
AccessControl assignment routes
AccessControl ceiling routes
AccessControl user-search/effective routes except existing accepted me/capabilities
Catalog scope-resource routes
EnrichUserHitsAsync
AccessControlDevelopmentSeed.cs
AccessControlDemoSnapshot.cs
Fulfillment
Checkout
frontend
database schema/migrations

FRONTEND:
FROZEN — NO PRODUCTION CHANGES.

CHECKOUT:
PAUSED_AT_SAFE_W5_CHECKPOINT — UNTOUCHED.

MANDATORY AUDIT AFTER CHANGE:

Search and prove:

Host AdminListCatalogAsync = ZERO
Host SellerListCatalogAsync = ZERO
Host "/permissions" route mappings = ZERO
AccessControl.Endpoints admin "/permissions" mapping = EXACTLY ONE
AccessControl.Endpoints seller "/permissions" mapping = EXACTLY ONE
new query is sent through ISender
new query handler is real MediatR
no direct IAccessControlDirectory use for these routes in Endpoints
no duplicate PermissionCatalog implementation
no AccessControl.Application -> Host dependency
no AccessControl.Application -> Endpoints dependency

FOCUSED VALIDATION:

dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj --no-restore

dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj --no-restore

dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Run only focused AccessControl/Host architecture or endpoint tests if existing tests directly cover
the touched route ownership or CQRS wiring.

No solution-wide build.
No broad test suite.
No retry loop.

RECOVERY SOT SYNC:

Update the canonical Recovery SoT only if this repository's current workflow requires accepted implementation
tasks to advance it at worker time.

Do not falsely mark AccessControl COMPLETE_REFERENCE_PATTERN.
Do not structure-certify AccessControl.
Do not mark Host AccessControl residue ZERO.

AccessControl remains IN_PROGRESS after this task.

EVIDENCE REQUIRED:

Create:

docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-PERMISSION-CATALOG-001/permission-catalog-migration.md

Evidence must contain:

before/after route ownership
query/handler path
admin endpoint path
seller endpoint path
Host removals
authorization parity statement
response parity statement
validator classification
single-route-ownership proof
focused build results
residual AccessControl Host families

SUCCESS CRITERIA:

PASS only if all are true:

Both /permissions routes are owned by AccessControl.Endpoints.
Both routes use ISender.
Real MediatR query/handler exists.
Existing PermissionCatalog remains the single permission catalog authority.
Authorization and response behavior are preserved.
Host mappings and Host handlers for this family are removed.
No other AccessControl route family is migrated.
No unrelated production code changes.
Focused builds succeed with 0 errors.
Checkout and frontend remain untouched.
AccessControl is NOT falsely marked complete or structure-certified.

If any required parity or ownership proof fails:
Status = INCOMPLETE
STOP.

If repository state conflicts with the accepted parent:
Status = RECOVERY_CONFLICT
STOP.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-PERMISSION-CATALOG-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Permission-Catalog-Authority:
Application-CQRS-State:
Admin-Route-State:
Seller-Route-State:
Host-Permission-Route-Residue:
Single-Route-Ownership-State:
Authorization-Parity-State:
Response-Parity-State:
Validator-Classification:
Application-Boundary-State:
Endpoint-Boundary-State:
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
