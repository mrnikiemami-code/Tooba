PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-READ-001
Parent-Task: TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ADDRESSBOOK
Title: Migrate AddressBook read endpoints — List + Get only
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002-R1

ACCEPTED-PARENT-COMMIT:
5a083e2a516f2c6b10f79872107a00fbb6059c72

RECOVERY-SOT-CHECKPOINT:
1c34d03839afa1cf3c5c32094eaafb95de470fb3

TIMEBOX:
Target <= 7 minutes.
Hard maximum 9 minutes.
If safe completion exceeds hard limit, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.

ONLY ONE SLICE:

Move exactly two read routes from Host to AddressBook.Endpoints:

GET /v1/customer/addresses
GET /v1/customer/addresses/{addressId:guid}

Do NOT migrate Create/Update/Delete/SetDefault in this task.

CURRENT CQRS:

ListCustomerAddressesQuery
GetCustomerAddressQuery
validator coverage for Get present

USE MODULE CQRS ONLY:

New module endpoints must dispatch via ISender.

Do NOT call IAddressBookDirectory directly from Endpoints.

ACTOR RESOLUTION:

Preserve current semantics exactly:

authenticated session user
production without authenticated session -> 401
Development/Testing X-Tooba-Dev-Actor-User-Id when valid non-empty Guid
Development/Testing fallback guest actor

Do NOT use Order.Application StorefrontCheckoutService.StorefrontGuestActorId.

Use neutral stable authority:
Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId

If AddressBook.Endpoints currently cannot reference Order.Contracts under the certified boundary pattern, do NOT add a foreign Application/Domain reference.
A direct Order.Contracts reference is allowed only if consistent with current cross-module contracts rules; otherwise introduce the smallest neutral BuildingBlocks/shared seam and document it.

Do not invent a Host dependency.

HOST SECURITY/SESSION SEAM:

AddressBook.Endpoints must not reference Tooba.Host.Storefront.CurrentAuthenticatedSession directly if that creates a Host dependency.

Inspect existing BuildingBlocks neutral security abstractions and use the established seam if available.

If a tiny Host adapter is required, keep it generic/security-only and register in Host composition.
No AddressBook business behavior in Host adapter.

DO NOT broaden into a general auth refactor.

MODULE ENDPOINT FILE:

Create a Customer capability endpoint file under:

src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Customer/

Preferred:
AddressBookCustomerReadEndpoints.cs

Namespace:
Tooba.AddressBook.Endpoints.Customer

Map only the two read routes in this task.

AddressBookEndpointModule.MapAddressBookModuleEndpoints must map the module group/slice needed for these two routes.

ROUTE PARITY:

List:
GET /v1/customer/addresses

unauthorized -> 401
success -> 200 JSON list
actor semantics unchanged
dispatch ListCustomerAddressesQuery

Get:
GET /v1/customer/addresses/{addressId:guid}

unauthorized -> 401
success -> 200 JSON item
missing/foreign -> 404 JSON:
{ title = "Not Found", errorCode = "customer.address.missing" }
dispatch GetCustomerAddressQuery

Do not change response shape/status.

HOST ROUTE REMOVAL:

Remove only the two read route mappings/handlers from:
src/backend/Host/Tooba.Host/AddressBook/AddressBookEndpoints.cs

Keep Host class/file because four write routes still remain.

After this task Host file must still own exactly:

POST create
PUT update
DELETE
POST set-default

Program mapping strategy:

Host legacy MapAddressBookEndpoints remains because four write routes still exist.
Add app.MapAddressBookModuleEndpoints() exactly once if not already mapped.
ensure no duplicate List/Get route ownership.

Do not remove Host AddressBook using/namespace while write routes remain.

NO WRITE ROUTE MIGRATION:

Do NOT touch:
CreateAsync
UpdateAsync
DeleteAsync
SetDefaultAsync
CustomerAddressWriteRequest
CustomerAddressWriteRequestExtensions
ToWrite

NO SEED WORK:

Do NOT touch AddressBookDevelopmentSeed.cs.
Do NOT touch ProductWorkspaceDevelopmentBootstrap seed calls.

FOCUSED VALIDATION:

Build:

Tooba.AddressBook.Application
Tooba.AddressBook.Endpoints
Tooba.Host

Run only existing focused AddressBook Host endpoint tests if a narrow filter exists and can validate List/Get parity cheaply.

No solution build.
No broad integration suite.
No broad architecture suite.
No retry cascade.

EVIDENCE:

Create:
docs/evidence/TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-READ-001/addressbook-read-endpoint-migration.md

Evidence must include:

exact two routes migrated
exact four routes still Host-owned
actor resolution seam and guest actor authority
proof no Order.Application dependency in AddressBook.Endpoints
ISender dispatch proof
duplicate route ownership = ZERO
response parity for List/Get
Host file remains because writes still pending
focused build/test results
exact next recommended slice

CANONICAL TASK ARTIFACT:

Commit exact task at:
docs/ai/tasks/TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-READ-001.task.md

RECOVERY HONESTY:

AddressBook remains IN_PROGRESS.
Host residue remains NON-ZERO because four write routes + seed remain.
NOT COMPLETE_REFERENCE_PATTERN.
NOT STRUCTURE_CERTIFIED.

Expected next:
a small write endpoint migration slice, preferably Create+Update only.

PASS ONLY IF:

exactly two read routes are module-owned
exactly four write routes remain Host-owned
module endpoints use ISender only
actor semantics preserved
no AddressBook.Endpoints -> Host dependency
no AddressBook.Endpoints -> Order.Application dependency
guest fallback uses Order.Contracts or an accepted neutral seam
no duplicate List/Get routes
builds pass
focused tests pass if run
canonical task/evidence committed
hard timebox respected

If safe completion exceeds 9 minutes:
Status = INCOMPLETE
STOP.
Do not migrate write routes.
Do not loop.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-READ-001
Parent-Task: TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Migrated-Route-Count:
Module-Owned-Read-Routes:
Host-Owned-Write-Route-Count:
Host-Owned-Write-Routes:
Actor-Seam-State:
Guest-Actor-Authority-State:
Order-Application-Dependency-State:
ISender-State:
Duplicate-Route-State:
List-Parity-State:
Get-Parity-State:
Host-AddressBook-File-State:
Program-Mapping-State:
Focused-Builds:
Focused-Tests:
Canonical-Task-State:
Evidence:
Production-Code-Scope:
Test-Code-Scope:
Checkout-State:
Frontend-Production-Changes:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP
Do not auto-start write endpoint migration.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
