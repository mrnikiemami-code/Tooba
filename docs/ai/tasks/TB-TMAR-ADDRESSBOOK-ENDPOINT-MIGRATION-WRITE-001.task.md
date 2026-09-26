PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-001
Parent-Task: TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-READ-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ADDRESSBOOK
Title: AddressBook endpoint migration — Create + Update only
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-READ-001

ACCEPTED-PARENT-COMMIT:
e8761e244add7315dadc87ff511d5748bf07ff94

RECOVERY-SOT-CHECKPOINT:
f9626083094e1cb6a93cbb087c479d73acb88d03

TIMEBOX:
Target <= 6 minutes.
Hard maximum 8 minutes.
If safe completion exceeds hard limit, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.

ONLY ONE MIGRATION SLICE:

Move exactly these two write routes from Host to Tooba.AddressBook.Endpoints:

POST /v1/customer/addresses
PUT /v1/customer/addresses/{addressId:guid}

Do NOT move Delete or SetDefault in this task.

MODULE ENDPOINTS:

Create/update module-owned write endpoint file, preferably:

Tooba.AddressBook.Endpoints/Customer/AddressBookCustomerWriteEndpoints.cs

Map only:

POST ""
PUT "/{addressId:guid}"

Dispatch only via MediatR ISender:

CreateCustomerAddressCommand
UpdateCustomerAddressCommand

Do NOT inject IAddressBookDirectory in Endpoints.

ACTOR RESOLUTION:

Reuse existing:
AddressBookCustomerActorResolver

Do NOT create a second actor resolver.
Do NOT reference Host session types.
Do NOT reference Order.Application.

REQUEST DTO:

Move/recreate the existing Host CustomerAddressWriteRequest and ToWrite mapping into AddressBook.Endpoints.Customer.

Preserve exact wire fields:

RecipientName
ContactMobile
Country
ProvinceName
CityName
PostalCode
PostalAddress
BuildingUnit
Label
IsDefault
FirstName
LastName

Preserve current defaults:
FirstName/LastName null -> string.Empty in CustomerAddressWrite.

Do not change JSON/wire shape.

HTTP SEMANTIC PARITY:

Create:

unauthorized -> 401
{ title = "Unauthorized", errorCode = "customer.session.required" }
success -> 201 JSON of CustomerAddressRecord

Update:

unauthorized -> same 401 body
success -> 200 JSON of CustomerAddressRecord

Do not normalize or introduce new error mapping.

HOST CLEANUP FOR THIS SLICE:

In Host/AddressBook/AddressBookEndpoints.cs:

remove MapPost("", CreateAsync)
remove MapPut("/{addressId:guid}", UpdateAsync)
remove CreateAsync
remove UpdateAsync
remove CustomerAddressWriteRequest
remove CustomerAddressWriteRequestExtensions
remove ToWrite
ONLY IF no remaining Host write route needs them after this slice.

After this task Host must still own exactly:

DELETE /v1/customer/addresses/{addressId:guid}
POST /v1/customer/addresses/{addressId:guid}/default

Retain:

ResolveActor
Unauthorized
DevActorHeader
ONLY because Delete/SetDefault still use them.

Do NOT delete Host AddressBookEndpoints.cs yet.

DUPLICATE ROUTE RULE:

After task:

module-owned routes = 4 total (List, Get, Create, Update)
Host-owned routes = 2 total (Delete, SetDefault)
duplicate ownership = ZERO

PROGRAM:

Keep both map calls for now:

app.MapAddressBookEndpoints()
app.MapAddressBookModuleEndpoints()

Do not remove Host map call yet because two Host routes remain.

CQRS/VALIDATION:

Do NOT modify existing commands/handlers/validators unless compilation strictly requires a namespace/import correction.

Current CQRS inventory remains:
6 requests total
5 validator-required / 5 present
1 no-validator-required

FOCUSED VALIDATION:

Build:
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Run only focused existing AddressBook tests:
dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~AddressBookFoundationTests

No solution build.
No integration suite.
No broad architecture suite.
No retries.

CANONICAL TASK ARTIFACT:

Commit exact task at:
docs/ai/tasks/TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-001.task.md

EVIDENCE:

Create:
docs/evidence/TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-001/addressbook-write-endpoint-migration-001.md

Evidence must include:

exact two routes migrated
exact two routes remaining Host-owned
DTO/wire-shape parity
actor resolver reuse
ISender-only proof
no IAddressBookDirectory in module endpoint file
duplicate route ownership ZERO
Program dual-map state
focused build/test results
exact next recommended slice

RECOVERY HONESTY:

AddressBook remains IN_PROGRESS.
Host AddressBookEndpoints.cs still exists.
NOT COMPLETE_REFERENCE_PATTERN.
NOT STRUCTURE_CERTIFIED.

Expected next task after PASS:
TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-002
for Delete + SetDefault only.

PASS ONLY IF:

exactly Create + Update routes moved
Delete + SetDefault remain Host-owned
module endpoints use ISender only
actor resolver reused
wire/status parity preserved
duplicate ownership ZERO
Host file retained
focused builds pass
focused AddressBook tests pass
canonical task/evidence committed
hard timebox respected

If safe completion exceeds 8 minutes:
Status = INCOMPLETE
STOP.
Do not move Delete/SetDefault.
Do not loop.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-001
Parent-Task: TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-READ-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Migrated-Route-Count:
Module-Owned-Route-Count:
Host-Owned-Route-Count:
Create-Route-State:
Update-Route-State:
Remaining-Host-Routes:
Actor-Seam-Reuse-State:
ISender-State:
Request-Dto-State:
Create-Parity-State:
Update-Parity-State:
Duplicate-Route-State:
Host-AddressBook-File-State:
Program-Mapping-State:
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
Do not auto-start next migration slice.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
