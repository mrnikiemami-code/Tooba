PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-SCOPE-RESOURCES-001
Parent-Task: TB-TMAR-RECOVERY-SOT-SYNC-ACCESSCONTROL-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Evacuate Admin + Seller scope-resources through Catalog.Contracts seam
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-RECOVERY-SOT-SYNC-ACCESSCONTROL-001

ACCEPTED-RECOVERY-COMMIT:
a53a866b8d08895b7df0598abee91621973126f2

PRODUCTION-PARENT-STATE:
Latest accepted production task:
TB-TMAR-HOST-ACCESSCONTROL-USER-SEARCH-EFFECTIVE-SEAM-001
commit:
4a6074e62fbaf557f57aa2770d76b8d14164dc72

TIMEBOX:
Target <= 12 minutes.
Hard maximum 15 minutes.
If clean completion would exceed the hard limit, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.

ONE OBJECTIVE:

Evacuate the complete Admin + Seller scope-resources family from Host.

ADMIN ROUTES:

GET /v1/admin/access-control/scope-resources/categories
GET /v1/admin/access-control/scope-resources/brands
GET /v1/admin/access-control/scope-resources/products
GET /v1/admin/access-control/scope-resources/warehouses
GET /v1/admin/access-control/scope-resources/stores
GET /v1/admin/access-control/scope-resources/order-segments

SELLER ROUTES:

GET /v1/seller/access-control/scope-resources/categories
GET /v1/seller/access-control/scope-resources/brands
GET /v1/seller/access-control/scope-resources/products
GET /v1/seller/access-control/scope-resources/warehouses
GET /v1/seller/access-control/scope-resources/stores
GET /v1/seller/access-control/scope-resources/order-segments

CURRENT HOST HANDLERS:

AdminListCategoriesAsync
AdminListBrandsAsync
AdminListProductsAsync
AdminDeferredScopeAsync

SellerListCategoriesAsync
SellerListBrandsAsync
SellerListProductsAsync
SellerDeferredScopeAsync

CURRENT PROBLEM:

Host currently consumes:
Tooba.Catalog.Application.ICatalogLookupGateway

This is NOT an acceptable boundary to move into AccessControl.

AccessControl must not gain a Catalog.Application dependency.

ARCHITECTURE DECISION:

Introduce the smallest reusable Catalog-owned CONTRACT seam in:

Tooba.Catalog.Contracts

Suggested shape:

IAccessControlScopeResourceLookup

with neutral contract DTOs for:

category item
brand item
product item

and methods equivalent to current observable behavior:

ListCategoriesAsync(string? search, CancellationToken)
ListBrandsAsync(string? search, CancellationToken)
ListProductsAsync(string? search, CancellationToken)

Use neutral contract records matching the JSON shape currently produced by Host.

Catalog owns the implementation.

Preferred implementation strategy:

add a thin Catalog-owned adapter in Catalog.Infrastructure around the existing ICatalogLookupGateway
register the Contracts interface in CatalogModule
do NOT expose ICatalogLookupGateway to AccessControl
do NOT move Catalog Application types into Catalog.Contracts
do NOT make AccessControl depend on Catalog.Domain

If the existing CatalogDirectory can safely implement the new contract without leaking Application types, that is acceptable only if it remains a clean Catalog-owned implementation and is smaller than an adapter.

ACCESSCONTROL APPLICATION:

Create a focused real MediatR query, preferably:

Queries/ListScopeResources/ListScopeResourcesQuery.cs

Use an AccessControl-owned neutral enum/kind such as:
Category
Brand
Product
Warehouse
Store
OrderSegment

The handler may depend on:
Tooba.Catalog.Contracts.IAccessControlScopeResourceLookup

Behavior:

Category/Brand/Product -> call Catalog Contracts seam
Warehouse/Store/OrderSegment -> return deferred marker with empty items, preserving existing behavior

Do NOT depend on:
Catalog.Application
Catalog.Domain
Host
Endpoints

Do NOT use a generic string dispatcher if a typed enum is cleaner.

OBSERVABLE RESPONSE PARITY:

Existing concrete resources:

categories / brands / products:
{
deferred: false,
items: [...]
}

Deferred resources:

warehouses / stores / order-segments:
{
deferred: true,
items: []
}

Preserve response JSON shape and search query q.

For concrete resources:

q behavior must remain whatever current ICatalogLookupGateway methods provide
do not invent new filtering or paging

AUTHORIZATION PARITY:

Admin routes:
IAdminPanelAccess.RequireAuthorizedAsync
→ accesscontrol.view

Seller routes:
ISellerPanelAccess.RequireAuthorizedAsync
→ accesscontrol.view

Seller sellerId is NOT used to alter the current scope-resource result because current Host behavior does not pass sellerId into Catalog lookup.
Do not silently introduce seller filtering.

ENDPOINT TARGET:

Extend existing:

AccessControlAdminEndpoints.cs
AccessControlSellerEndpoints.cs

All application dispatch via ISender.

No direct Catalog contract usage in Endpoints if query handling can own it.
No direct ICatalogLookupGateway.
No Infrastructure/DbContext.
No Host types.

HOST EVACUATION:

After module ownership is proven, remove ONLY the 12 scope-resource mappings and the eight Host handler methods listed above.

Remove Host using:
Tooba.Catalog.Application

if no residual AccessControlEndpoints consumer remains.

Remove RequireSellerAsync / Trace / MapError ONLY if now truly unused.
Do not remove helpers merely because they appear small.

OUT OF SCOPE:

Admin demo-preview
AccessControlDevelopmentSeed.cs
AccessControlDemoSnapshot.cs
Program.cs final cleanup
final Host folder deletion
AccessControl structure certification
other modules
Checkout
Frontend
schema/migrations

CATALOG CONTRACT BOUNDARY AUDIT:

After change:

AccessControl.Application -> Catalog.Contracts = ALLOWED

AccessControl.Application -> Catalog.Application = ZERO
AccessControl.Application -> Catalog.Domain = ZERO
AccessControl.Endpoints -> Catalog.Application = ZERO
AccessControl.Endpoints -> Catalog.Domain = ZERO

Host AccessControlEndpoints.cs -> Catalog.Application = ZERO

Catalog contract assembly should remain dependency-light and must not reference Catalog.Application/Infrastructure.

VALIDATION:

All routes are GET with optional q.
NO_VALIDATOR_REQUIRED unless an existing invariant already exists.

Do not add ceremonial validators.
Do not add arbitrary q length restrictions.

STALE TEST MAINTENANCE — EXPLICITLY ALLOWED BUT TINY:

Known debt:
Tooba.Host.Tests/AccessControlFoundationTests.AccessControl_module_boundary_static_checks

It contains stale assertions expecting Host-owned AccessControl route text already evacuated by accepted tasks.

In this task you MAY update ONLY the stale AccessControl boundary assertions necessary to reflect current module ownership, provided:

change is tiny and directly related to current/previous accepted AccessControl route ownership
no broad test rewrite
no new large suite
no unrelated assertions altered

If the stale test is not needed for this scope or fixing it would expand work:
leave it recorded as residual debt and do not spend the timebox on it.

FOCUSED TESTING:

Priority:

focused builds
one directly relevant AccessControl boundary static test only if tiny
no broad tests

If you update AccessControlFoundationTests, run ONLY the directly relevant test/filter.

No solution build.
No broad architecture suite.
No integration suite.
No retries.

MANDATORY SINGLE OWNERSHIP PROOF:

All 12 scope-resource routes must have:
module mapping = EXACTLY ONE
Host mapping = ZERO

Old production Host handler names:
AdminListCategoriesAsync
AdminListBrandsAsync
AdminListProductsAsync
AdminDeferredScopeAsync
SellerListCategoriesAsync
SellerListBrandsAsync
SellerListProductsAsync
SellerDeferredScopeAsync

= ZERO production occurrences after migration.

CANONICAL TASK ARTIFACT:

Commit this exact Architect task at:

docs/ai/tasks/TB-TMAR-HOST-ACCESSCONTROL-SCOPE-RESOURCES-001.task.md

EVIDENCE:

Create:

docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-SCOPE-RESOURCES-001/scope-resources-migration.md

Evidence must include:

all 12 before/after route ownerships
new Catalog.Contracts seam
Catalog-owned implementation + registration
AccessControl CQRS query
JSON parity
q parity
Admin/Seller auth parity
explicit note that Seller filtering was NOT added
deferred-resource parity
Host removals
boundary audit
stale test handling
focused build/test results
residual Host AccessControl files/routes

FOCUSED BUILDS:

dotnet build src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Tooba.Catalog.Contracts.csproj --no-restore
dotnet build src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Tooba.Catalog.Infrastructure.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Do NOT build Catalog.Infrastructure if the chosen Catalog-owned implementation lives elsewhere and no Infrastructure code changed; build the actual changed Catalog implementation project instead.

RECOVERY HONESTY:

AccessControl remains IN_PROGRESS.
Do NOT mark COMPLETE_REFERENCE_PATTERN.
Do NOT structure-certify AccessControl.

If this task passes, expected remaining Host AccessControl runtime endpoint residue is:

Admin demo-preview only.

Separate files still remain:
AccessControlDevelopmentSeed.cs
AccessControlDemoSnapshot.cs

Program.cs legacy AccessControl mapping/bootstrap residue remains pending final cleanup.

The final AccessControl Host target is still:
src/backend/Host/Tooba.Host/AccessControl = ZERO files

but NOT in this task.

PASS ONLY IF:

all 12 scope-resource routes are module-owned
Catalog boundary is Contracts-only
no Catalog.Application/Domain leak into AccessControl
current response/q/deferred/auth behavior is preserved
no seller filtering is invented
Host scope-resource mappings/handlers are removed
canonical task/evidence committed
focused builds pass
only tiny directly related test maintenance occurs, if any
hard timebox is respected

If clean completion exceeds the hard limit:
Status = INCOMPLETE
STOP IMMEDIATELY.
Do not loop.
Do not broaden scope.
Do not bypass the Contracts boundary.
Do not auto-start another task.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-SCOPE-RESOURCES-001
Parent-Task: TB-TMAR-RECOVERY-SOT-SYNC-ACCESSCONTROL-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Recovery-State:
Catalog-Contracts-State:
Catalog-Implementation-State:
Scope-Resources-CQRS-State:
Admin-ScopeResources-State:
Seller-ScopeResources-State:
Authorization-Parity-State:
Seller-Filtering-Parity-State:
Search-Q-Parity-State:
Deferred-Resource-Parity-State:
Response-Contract-State:
Catalog-Boundary-State:
Application-Boundary-State:
Endpoint-Boundary-State:
Host-Catalog-Dependency-State:
Host-Route-Residue:
Single-Route-Ownership-State:
Stale-Test-Maintenance-State:
Focused-Builds:
Focused-Tests:
Canonical-Task-State:
Evidence:
Production-Code-Scope:
Test-Code-Scope:
Checkout-State:
Frontend-Production-Changes:
Residual-Host-AccessControl-State:
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
