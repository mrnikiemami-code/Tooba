PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ADDRESSBOOK-CQRS-READ-SLICE-001
Parent-Task: TB-TMAR-ADDRESSBOOK-MODULE-FOUNDATION-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ADDRESSBOOK
Title: AddressBook CQRS read slice — List + Get only
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-ADDRESSBOOK-MODULE-FOUNDATION-001

ACCEPTED-PARENT-COMMIT:
089b6a8b386ca3eb01adddf2421bee74bab4cd26

RECOVERY-SOT-CHECKPOINT:
55fca24818dac94dbe3bd09206894b7c0e9a7bfb

TIMEBOX:
Target <= 6 minutes.
Hard maximum 8 minutes.
If safe completion exceeds hard limit, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.

ONLY ONE SLICE:

Add MediatR CQRS for exactly two AddressBook read use-cases:

List customer addresses
Get one customer address by id

Do NOT migrate HTTP routes in this task.

DO NOT touch create/update/delete/set-default yet.

APPLICATION STRUCTURE:

Create capability folders:

Tooba.AddressBook.Application/Customer/List/
Tooba.AddressBook.Application/Customer/Get/

Create exactly:

ListCustomerAddressesQuery
ListCustomerAddressesQueryHandler

GetCustomerAddressQuery
GetCustomerAddressQueryHandler

Use MediatR 12.5 IRequest / IRequestHandler pattern consistent with already-certified modules.

The handlers must delegate to the existing IAddressBookDirectory abstraction.

Do NOT access DbContext directly from handlers.

SEMANTIC PARITY:

List query:

inputs: trusted ActorUserId only
output: IReadOnlyList<CustomerAddressRecord>
handler delegates to IAddressBookDirectory.ListAsync(actorUserId, ct)

Get query:

inputs: trusted ActorUserId + AddressId
output: CustomerAddressRecord? exactly preserving current null-on-missing behavior
handler delegates to inherited IAddressBookCheckoutLookup/IAddressBookDirectory.GetAsync(actorUserId, addressId, ct)

No HTTP status mapping here.
No new error envelope.
No behavior normalization.

VALIDATION CLASSIFICATION:

Classify and implement only what is transport-shape necessary.

ListCustomerAddressesQuery:

NO_VALIDATOR_REQUIRED because ActorUserId is trusted server context, not request payload.

GetCustomerAddressQuery:

AddressId comes from route input and must not be Guid.Empty.
Add one FluentValidation validator only if this matches the existing platform transport-shape pattern.
ActorUserId must NOT be validated as untrusted payload.

Preferred location:
Tooba.AddressBook.Application/Validators/Customer/Get/GetCustomerAddressQueryValidator.cs

If the established platform pattern clearly treats route Guid constraints as sufficient and does not require an additional non-empty Guid validator, document NO_VALIDATOR_REQUIRED instead and do not invent one.
Do not guess: inspect one certified module precedent and follow it.

Do NOT add validators for future write commands in this task.

NO ENDPOINT MIGRATION:

The new Tooba.AddressBook.Endpoints project must still map ZERO AddressBook routes after this task.

Host must still own all six routes.

Do NOT modify:

Host AddressBookEndpoints.cs
app.MapAddressBookEndpoints()
AddressBookEndpointModule route mapping
Program route ownership
AddressBookDevelopmentSeed.cs

NO ROOT REFACTOR YET:

Do NOT move:

AddressBookContracts.cs
AddressBookDirectory.cs
CustomerAddress.cs
CustomerAddressContracts.cs
AddressBookModule.cs

This task is CQRS read slice only.

BOUNDARIES:

Application:

no Host
no Infrastructure
no foreign Application/Domain/Infrastructure
AddressBook.Contracts allowed
AddressBook.Domain only if already legitimately required by existing Application contract; prefer not needed here

Handlers must depend only on IAddressBookDirectory.

FOCUSED TESTS:

Add the smallest direct handler tests only if a cheap existing test project/location already exists.

At minimum prove:

List handler delegates actor + cancellation token and returns directory result
Get handler delegates actor/address + cancellation token and preserves null

If adding tests would require a new test project or broad infrastructure setup, skip tests and rely on focused builds; document why.

FOCUSED BUILDS:

dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Application/Tooba.AddressBook.Application.csproj --no-restore
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

No solution build.
No integration suite.
No broad architecture suite.
No retry cascade.

CANONICAL TASK ARTIFACT:

Commit exact task at:
docs/ai/tasks/TB-TMAR-ADDRESSBOOK-CQRS-READ-SLICE-001.task.md

EVIDENCE:

Create:
docs/evidence/TB-TMAR-ADDRESSBOOK-CQRS-READ-SLICE-001/addressbook-cqrs-read-slice.md

Evidence must include:

exact two requests
exact handlers
folder/namespace paths
validator classification for both requests
handler -> IAddressBookDirectory delegation proof
Host six-route ownership unchanged
Endpoints new route count still ZERO
focused build/test results
exact next recommended slice

RECOVERY HONESTY:

AddressBook remains IN_PROGRESS.
NOT COMPLETE_REFERENCE_PATTERN.
NOT STRUCTURE_CERTIFIED.

Expected next slice after PASS:
TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-001
or another small bounded write subset chosen by Architect after verification.

PASS ONLY IF:

exactly two read CQRS requests exist
exactly two handlers exist
no write CQRS added
no HTTP route moved
Endpoints project still maps ZERO routes
Host still owns six routes
validation classification is explicit
focused builds pass
tests pass if added
canonical task/evidence committed
hard timebox respected

If safe completion exceeds 8 minutes:
Status = INCOMPLETE
STOP.
Do not start write CQRS.
Do not migrate routes.
Do not loop.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ADDRESSBOOK-CQRS-READ-SLICE-001
Parent-Task: TB-TMAR-ADDRESSBOOK-MODULE-FOUNDATION-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Read-Request-Count:
Read-Handler-Count:
List-Query-State:
Get-Query-State:
Validator-Classification-State:
Validator-Files:
Application-Folder-State:
Application-Boundary-State:
Directory-Delegation-State:
Host-Route-Ownership-State:
New-Endpoint-Route-Count:
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
Do not auto-start the next slice.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
