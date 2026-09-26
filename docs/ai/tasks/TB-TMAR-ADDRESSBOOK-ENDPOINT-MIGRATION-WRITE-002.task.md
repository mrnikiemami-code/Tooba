PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-002
Parent-Task: TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ADDRESSBOOK
Title: AddressBook endpoint migration — Delete + SetDefault only
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-001

ACCEPTED-PARENT-COMMIT:
2a17ae332d16fe1b5f4b8e31d1b194bdbde9c568

RECOVERY-SOT-CHECKPOINT:
21ae229bd54fa8ab7b6bf0a855df9a1bd8e177f9

TIMEBOX:
Target <= 6 minutes.
Hard maximum 8 minutes.
If safe completion exceeds hard limit, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.

ONLY ONE MIGRATION SLICE:

Move exactly the final two AddressBook routes from Host to Tooba.AddressBook.Endpoints:

DELETE /v1/customer/addresses/{addressId:guid}
POST /v1/customer/addresses/{addressId:guid}/default

After this task:

module-owned route count = 6
Host-owned AddressBook route count = 0
duplicate ownership = ZERO

MODULE ENDPOINTS:

Extend the existing module-owned customer write endpoint composition.

Prefer to keep all write endpoints in:
Tooba.AddressBook.Endpoints/Customer/AddressBookCustomerWriteEndpoints.cs

Map:

DELETE "/{addressId:guid}"
POST "/{addressId:guid}/default"

Dispatch only via MediatR ISender:

DeleteCustomerAddressCommand
SetDefaultCustomerAddressCommand

Do NOT inject IAddressBookDirectory.

ACTOR RESOLUTION:

Reuse existing:
IAddressBookCustomerActorResolver

Do NOT create another resolver.
Do NOT reference:

CurrentAuthenticatedSession
Tooba.Host.*
Order.Application

HTTP SEMANTIC PARITY:

Delete:

unauthorized -> 401
{ title = "Unauthorized", errorCode = "customer.session.required" }
success -> 204 NoContent

SetDefault:

unauthorized -> same 401 body
success -> 200 JSON CustomerAddressRecord

Do not introduce new error mapping.
Do not catch/translate business/domain exceptions.

HOST ROUTE EVACUATION:

After moving both routes, Host must own ZERO AddressBook HTTP routes.

In:
src/backend/Host/Tooba.Host/AddressBook/AddressBookEndpoints.cs

Remove:

MapDelete
MapPost default
DeleteAsync
SetDefaultAsync
Host-local ResolveActor
Host-local Unauthorized
DevActorHeader
now-unused IAddressBookDirectory / AddressBook.Application dependency from this file

If the file becomes route-empty:
DELETE AddressBookEndpoints.cs entirely.

Then in Program.cs:

remove using Tooba.Host.AddressBook; only if no longer needed
remove app.MapAddressBookEndpoints();
retain exactly one app.MapAddressBookModuleEndpoints();

Do NOT touch AddressBookDevelopmentSeed.cs in this task.
Do NOT touch its bootstrap call sites.

BOUNDARY END-STATE:

Host AddressBook HTTP ownership = ZERO.

AddressBook.Endpoints:

no Host dependency
no AddressBook.Infrastructure dependency
no foreign Application/Domain/Infrastructure dependency
Order.Contracts only as already accepted
ISender-only dispatch

CQRS/VALIDATION:

Do NOT modify existing 6 requests/handlers or 5 validators unless compilation strictly requires a trivial namespace/import fix.

Expected unchanged inventory:

6 endpoint-reachable CQRS requests
5 VALIDATOR_REQUIRED / 5 present
1 NO_VALIDATOR_REQUIRED
validator gap ZERO

TEST GUARDS:

Update only the focused AddressBookFoundation source-text guard if it still expects the Host endpoint file or Host route markers.

Do not broaden test scope.

FOCUSED VALIDATION:

Build:
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Test:
dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~AddressBookFoundationTests

No solution build.
No integration suite.
No broad architecture suite.
No retries.

CANONICAL TASK ARTIFACT:

Commit exact task at:
docs/ai/tasks/TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-002.task.md

EVIDENCE:

Create:
docs/evidence/TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-002/addressbook-write-endpoint-migration-002.md

Evidence must include:

exact two routes migrated
module route count = 6
Host AddressBook route count = 0
duplicate ownership ZERO
ISender-only proof
actor resolver reuse proof
Delete 204 parity
SetDefault 200 parity
Host AddressBookEndpoints.cs removed if route-empty
Program single module map state
AddressBookDevelopmentSeed untouched
focused build/test results
exact next recommended bounded cleanup slice

RECOVERY HONESTY:

AddressBook still IN_PROGRESS after this task because:

AddressBookDevelopmentSeed still remains under Host
root/capability structure cleanup may remain
final ARCH-COMPLETE-002 certification not yet done

Expected next task after PASS:
small Host seed evacuation / cleanup slice.
Do NOT start it automatically.

PASS ONLY IF:

Delete migrated
SetDefault migrated
all 6 AddressBook routes module-owned
Host owns ZERO AddressBook routes
duplicate ownership ZERO
AddressBookEndpoints.cs removed if empty
Program no longer calls app.MapAddressBookEndpoints()
module endpoints use ISender only
actor seam reused
focused builds pass
focused AddressBook tests pass
seed untouched
canonical task/evidence committed
hard timebox respected

If safe completion exceeds 8 minutes:
Status = INCOMPLETE
STOP.
Do not start seed work.
Do not loop.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-002
Parent-Task: TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Migrated-Route-Count:
Module-Owned-Route-Count:
Host-Owned-Route-Count:
Delete-Route-State:
SetDefault-Route-State:
Actor-Seam-Reuse-State:
ISender-State:
Duplicate-Route-State:
Host-AddressBook-Endpoint-File-State:
Program-Mapping-State:
Seed-State:
CQRS-Validator-State:
Focused-Builds:
Focused-Tests:
Canonical-Task-State:
Evidence:
Production-Code-Scope:
Test-Code-Scope:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP
Do not auto-start seed cleanup.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK