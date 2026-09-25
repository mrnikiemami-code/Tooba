PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-USER-SEARCH-EFFECTIVE-SEAM-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-ADMINPLATFORM-ASSIGNMENTS-EFFECTIVE-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Evacuate Admin/Seller user search and Seller effective through Contracts-only seams
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-HOST-ACCESSCONTROL-ADMINPLATFORM-ASSIGNMENTS-EFFECTIVE-001

ACCEPTED-PARENT-COMMIT:
99d59d894a4464b57d1f53a6df9800843a76a592

TIMEBOX:
Target <= 12 minutes.
Hard maximum 15 minutes.
If safe completion would exceed the timebox, return INCOMPLETE and STOP immediately.
No retry loop.
No scope expansion.
No Application-to-Application shortcut.

ONE OBJECTIVE:

Evacuate ONLY these three remaining user/effective routes from Host:

GET /v1/admin/access-control/users
GET /v1/seller/access-control/users
GET /v1/seller/access-control/users/{userId:guid}/effective

CURRENT HOST HANDLERS:

AdminSearchUsersAsync
SellerSearchUsersAsync
SellerEffectiveAsync

CURRENT HOST ENRICHMENT:

AdminSearchUsersAsync / SellerSearchUsersAsync currently:

query AccessControl scope users through IAccessControlDirectory.SearchUsersInScopeAsync(...)
optionally resolve q:
Guid directly
Email via Identity
Phone via Identity
add resolved user id if not already present
enrich contacts from Identity
enrich display name from OperatorProfile
filter/sort by q
return AccessUserHitDto list

CURRENT HOST FOREIGN DEPENDENCIES TO ELIMINATE:

Tooba.Identity.Application
Tooba.Identity.Domain
Tooba.OperatorProfile.Application

These MUST NOT be moved into AccessControl.Application or AccessControl.Endpoints.

KNOWN CONTRACTS AVAILABLE:

Identity.Contracts:

IActorContactLookup
ActorContactProjection

OperatorProfile.Contracts:

IActorDisplayLookup
ActorDisplayProjection

ARCHITECTURE DECISION:

AccessControl may depend on foreign module CONTRACTS only.

Use:
Tooba.Identity.Contracts
Tooba.OperatorProfile.Contracts

If email/phone -> user-id resolution has no suitable Identity.Contracts abstraction, add the SMALLEST stable Identity contract required, e.g. an identifier resolver that accepts a neutral identifier kind/value and returns Guid?.

The implementation of that contract must stay owned by Identity.
Do NOT expose IIdentityAuthenticationService or LoginIdentifierKind from Identity.Application/Domain to AccessControl.

If creating + wiring this small contract cannot be completed safely inside the hard timebox:
Status = INCOMPLETE
STOP.
Do not fall back to foreign Application references.

MANDATORY APPLICATION CQRS:

Create/reuse focused requests, preferably:

Queries/SearchAccessUsers/SearchAccessUsersQuery.cs

The handler may depend on:

IAccessControlDirectory
IActorContactLookup from Identity.Contracts
IActorDisplayLookup from OperatorProfile.Contracts
smallest Identity.Contracts identifier resolver if required

Query data:

OwnerScopeKind
OwnerScopeId
TenantId
q

The handler owns the current enrichment/filter/sort behavior.

Do NOT put HttpRequest/IResult/Results in Application.

Seller effective:
reuse accepted GetEffectiveAccessQuery.
No duplicate request.

SEARCH BEHAVIOR PARITY:

Preserve EXACTLY the observable behavior of current Host EnrichUserHitsAsync:

blank/whitespace q => null semantic
Guid q => directly consider that user id
q containing '@' => resolve as email
q with digit(s) and length >= 8 => resolve as phone
resolved user added if not already in scope-hit dictionary
contact email/mobile enrichment
OperatorProfile display name preferred over email/mobile
FirstNonEmpty semantics preserved
filtering against DisplayName, Email, Mobile, UserId string, RoleCodes
StringComparison.OrdinalIgnoreCase
final ordering by DisplayName ?? Email ?? UserId.ToString("D"), OrdinalIgnoreCase

Do not silently simplify search behavior.

CONTRACTS-ONLY BOUNDARY:

AccessControl.Application references allowed:

Tooba.Identity.Contracts
Tooba.OperatorProfile.Contracts

Forbidden:

Tooba.Identity.Application
Tooba.Identity.Domain
Tooba.OperatorProfile.Application
Tooba.OperatorProfile.Domain
Host

If project references must be added, add only Contracts project references.

IDENTITY CONTRACT IMPLEMENTATION:

If a new identifier resolver contract is needed:

define it in Tooba.Identity.Contracts
implement it in Identity-owned Application/Infrastructure using existing Identity internals
register it in Identity-owned DI
keep neutral contract input types
no AccessControl-specific implementation inside Host

Keep this change minimal and reusable.

ENDPOINT TARGET:

Admin:
extend AccessControlAdminEndpoints.cs
map:
GET /users

Seller:
extend AccessControlSellerEndpoints.cs
map:
GET /users
GET /users/{userId:guid}/effective

Authorization:

Admin /users:
IAdminPanelAccess.RequireAuthorizedAsync
-> accesscontrol.view

Seller /users:
ISellerPanelAccess.RequireAuthorizedAsync
-> accesscontrol.view
-> owner scope Seller + authorized seller id

Seller effective:
ISellerPanelAccess.RequireAuthorizedAsync
-> accesscontrol.view
-> GetEffectiveAccessQuery(userId, Seller, authorized sellerId, tenant)

All application dispatch through ISender.

No direct IAccessControlDirectory in Endpoints.

RESPONSE PARITY:

Admin /users -> Results.Json(search results)
Seller /users -> Results.Json(search results)
Seller effective -> Results.Json(effective access)

Preserve current uncaught behavior for these reads.
Do not introduce new error translation.

HOST EVACUATION:

After module ownership is proven, remove ONLY:

admin.MapGet("/users", AdminSearchUsersAsync)
seller.MapGet("/users", SellerSearchUsersAsync)
seller.MapGet("/users/{userId:guid}/effective", SellerEffectiveAsync)

Remove handlers:
AdminSearchUsersAsync
SellerSearchUsersAsync
SellerEffectiveAsync

Remove Host helpers ONLY if now unused:
EnrichUserHitsAsync
FirstNonEmpty
RequireSellerAsync
PlatformScope
SellerScope

Remove foreign Host usings for Identity.Application/Domain/OperatorProfile.Application if no residual Host consumer remains.

Do NOT touch scope-resource handlers in this task.

OUT OF SCOPE:

Admin/Seller scope-resources
demo-preview
DevelopmentSeed
DemoSnapshot
Program.cs final cleanup
Host folder deletion
Catalog boundary repair
Checkout
Frontend
schema/migrations

VALIDATION:

Search q:
NO_VALIDATOR_REQUIRED unless a current invariant already exists.
Do not add arbitrary length/format restrictions not present today.

Seller effective:
reuse accepted GetEffectiveAccessQuery classification.

FOCUSED TEST REQUIREMENT:

Tests remain minimal.

If SearchAccessUsersQuery introduces non-trivial filtering/enrichment logic, add ONLY a small focused test set covering the critical parity cases:

blank q ordering/enrichment
email or phone resolution path (one representative)
filtering by one enriched field

Maximum a few focused tests.
Do NOT create a broad suite.

If existing test infrastructure makes this expensive, document and rely on focused builds; do not exceed timebox.

MANDATORY AUDIT:

After migration:

AdminSearchUsersAsync = ZERO production
SellerSearchUsersAsync = ZERO production
SellerEffectiveAsync = ZERO production

Host references:
Tooba.Identity.Application = ZERO in Host/AccessControl/AccessControlEndpoints.cs
Tooba.Identity.Domain = ZERO there
Tooba.OperatorProfile.Application = ZERO there

AccessControl.Application foreign references:
Identity.Application = ZERO
Identity.Domain = ZERO
OperatorProfile.Application = ZERO
OperatorProfile.Domain = ZERO

Contracts-only dependencies allowed.

Single route ownership:
GET /v1/admin/access-control/users = EXACTLY ONE module mapping
GET /v1/seller/access-control/users = EXACTLY ONE module mapping
GET /v1/seller/access-control/users/{userId:guid}/effective = EXACTLY ONE module mapping
Host = ZERO for all three.

CANONICAL TASK ARTIFACT:

Commit this exact Architect-issued task at:

docs/ai/tasks/TB-TMAR-HOST-ACCESSCONTROL-USER-SEARCH-EFFECTIVE-SEAM-001.task.md

Do not omit it.

EVIDENCE:

Create:

docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-USER-SEARCH-EFFECTIVE-SEAM-001/user-search-effective-seam.md

Evidence must include:

three route before/after ownerships
exact Contracts used
any new Identity.Contracts identifier resolver and its Identity-owned implementation/registration
search parity statement
owner-scope/auth parity
boundary audit
Host helper removals
focused tests/builds
residual Host AccessControl families

FOCUSED VALIDATION ONLY:

dotnet build src/backend/Modules/Identity/Tooba.Identity.Contracts/Tooba.Identity.Contracts.csproj --no-restore
dotnet build src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Contracts/Tooba.OperatorProfile.Contracts.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Only build an additional Identity implementation project if a new Identity contract implementation was actually added there.

Run ONLY directly relevant focused tests as described above.

NO solution build.
NO broad integration suite.
NO broad architecture suite.
NO retries.

RECOVERY HONESTY:

AccessControl remains IN_PROGRESS.
Do NOT mark COMPLETE_REFERENCE_PATTERN.
Do NOT structure-certify AccessControl.

If this task passes:

all Admin/Seller user-search/effective Host routes are evacuated
Identity/OperatorProfile enrichment is Contracts-only
remaining AccessControlEndpoints Host residue is scope-resources + demo-preview/shared helpers

FINAL TRACK TARGET:
src/backend/Host/Tooba.Host/AccessControl = ZERO files
but NOT in this task.

PASS ONLY IF:

all three routes migrate
exact search behavior preserved
cross-module dependency becomes Contracts-only
no foreign Application/Domain leak into AccessControl
Seller effective reuses GetEffectiveAccessQuery
Host handlers/mappings removed
focused validation succeeds
canonical task/evidence committed
hard timebox respected

If a clean Contracts-only solution cannot safely finish inside 15 minutes:
Status = INCOMPLETE
STOP IMMEDIATELY.
Do not loop.
Do not use a shortcut.
Do not auto-start another task.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-USER-SEARCH-EFFECTIVE-SEAM-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-ADMINPLATFORM-ASSIGNMENTS-EFFECTIVE-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Search-CQRS-State:
Identity-Contracts-State:
Identity-Identifier-Resolver-State:
OperatorProfile-Contracts-State:
Admin-Users-State:
Seller-Users-State:
Seller-Effective-State:
Search-Parity-State:
Authorization-Parity-State:
Owner-Scope-Parity-State:
Response-Parity-State:
Application-Boundary-State:
Endpoint-Boundary-State:
Host-Foreign-Dependency-State:
Host-Helper-Residue:
Single-Route-Ownership-State:
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
