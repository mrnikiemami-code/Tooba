PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002
Parent-Task: TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ADDRESSBOOK
Title: AddressBook CQRS write slice — Delete + SetDefault only
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-001

ACCEPTED-PARENT-COMMIT:
3f7562865f4d6b3dc51fec3046223cc3598fd37a

RECOVERY-SOT-CHECKPOINT:
a9cdc67e5c940ab9515aa037ef8e4746336985f4

TIMEBOX:
Target <= 6 minutes.
Hard maximum 8 minutes.
If safe completion exceeds hard limit, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.

ONLY ONE SLICE:

Add MediatR CQRS for exactly two remaining AddressBook write use-cases:

Delete customer address
Set default customer address

Do NOT migrate HTTP routes in this task.

APPLICATION STRUCTURE:

Create capability folders:

Tooba.AddressBook.Application/Customer/Delete/
Tooba.AddressBook.Application/Customer/SetDefault/

Create exactly:

DeleteCustomerAddressCommand
DeleteCustomerAddressCommandHandler

SetDefaultCustomerAddressCommand
SetDefaultCustomerAddressCommandHandler

Use MediatR 12.5 IRequest / IRequestHandler pattern.

Handlers must depend only on IAddressBookDirectory.

Do NOT access DbContext/EF directly from handlers.

REQUEST SHAPE:

Both requests:

ActorUserId from trusted server context
AddressId from route input

Delete command:

returns Unit / no payload equivalent consistent with current MediatR foundation
handler delegates to IAddressBookDirectory.DeleteAsync(actor, addressId, ct)

SetDefault command:

returns CustomerAddressRecord
handler delegates to IAddressBookDirectory.SetDefaultAsync(actor, addressId, ct)

SEMANTIC PARITY:

Do not change current directory/domain behavior.
Do not add HTTP status mapping.
Do not catch/translate InvalidOperationException here.
Do not invent business error codes.

VALIDATION:

Both commands have route-derived AddressId.

Use the existing AddressBookFluentRules.RequireId helper and existing stable code:
customer.address.id_required

Add exactly two validators:

Validators/Customer/Delete/DeleteCustomerAddressCommandValidator.cs
Validators/Customer/SetDefault/SetDefaultCustomerAddressCommandValidator.cs

Validate AddressId only.
Do NOT validate ActorUserId as untrusted payload.

Do NOT add new helper/codes unless absolutely required.

NO ENDPOINT MIGRATION:

Tooba.AddressBook.Endpoints must still map ZERO routes.

Host still owns all six routes.

Do NOT modify:

Host/AddressBook/AddressBookEndpoints.cs
Host/AddressBook/AddressBookDevelopmentSeed.cs
Program app.MapAddressBookEndpoints()
AddressBookEndpointModule route mapping

NO ROOT REFACTOR:

Do NOT move:

AddressBookContracts.cs
AddressBookDirectory.cs
CustomerAddress.cs
CustomerAddressContracts.cs
AddressBookModule.cs

BOUNDARIES:

New Application files:

no Host
no Infrastructure
no foreign Application/Domain/Infrastructure
AddressBook.Contracts allowed if needed

FOCUSED TESTS:

If a cheap existing test location is available, add only tiny direct validator/handler tests for these two requests.

Otherwise no new test project; document why.

FOCUSED BUILDS:

dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Application/Tooba.AddressBook.Application.csproj --no-restore
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

No solution build.
No integration suite.
No broad architecture suite.
No retries.

CANONICAL TASK ARTIFACT:

Commit exact task at:
docs/ai/tasks/TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002.task.md

EVIDENCE:

Create:
docs/evidence/TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002/addressbook-cqrs-write-slice-002.md

Evidence must include:

exact two commands
exact two handlers
exact two validators
handler -> IAddressBookDirectory delegation proof
AddressId-only validation proof
full CQRS inventory now = 6 use-cases total
Host six-route ownership unchanged
Endpoints route count still ZERO
focused build/test results
exact next recommended slice

RECOVERY HONESTY:

AddressBook remains IN_PROGRESS.
NOT COMPLETE_REFERENCE_PATTERN.
NOT STRUCTURE_CERTIFIED.

Expected next work after PASS:
small endpoint migration slice(s), not a big-bang.

PASS ONLY IF:

exactly Delete + SetDefault CQRS added
exactly two handlers added
exactly two AddressId validators added
no route moved
Endpoints maps ZERO routes
Host still owns all six routes
focused builds pass
tests pass if added
canonical task/evidence committed
hard timebox respected

If safe completion exceeds 8 minutes:
Status = INCOMPLETE
STOP.
Do not start endpoint migration.
Do not loop.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002
Parent-Task: TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Write-Request-Count:
Write-Handler-Count:
Delete-Command-State:
SetDefault-Command-State:
Validator-Classification-State:
Validator-Files:
Total-AddressBook-CQRS-UseCases:
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
Do not auto-start endpoint migration.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
