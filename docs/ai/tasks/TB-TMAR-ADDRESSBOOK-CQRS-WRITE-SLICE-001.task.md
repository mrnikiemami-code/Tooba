PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-001
Parent-Task: TB-TMAR-ADDRESSBOOK-CQRS-READ-SLICE-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ADDRESSBOOK
Title: AddressBook CQRS write slice — Create + Update only
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-ADDRESSBOOK-CQRS-READ-SLICE-001

ACCEPTED-PARENT-COMMIT:
3679505e35598dd1b76e3ade3a540caed6a1ab4d

RECOVERY-SOT-CHECKPOINT:
61c9777a52291d0e08d00883b74d02f7469acb9a

TIMEBOX:
Target <= 6 minutes.
Hard maximum 8 minutes.
If safe completion exceeds hard limit, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.

ONLY ONE SLICE:

Add MediatR CQRS for exactly two AddressBook write use-cases:

Create customer address
Update customer address

Do NOT add Delete or SetDefault in this task.
Do NOT migrate HTTP routes.

APPLICATION STRUCTURE:

Create capability folders:

Tooba.AddressBook.Application/Customer/Create/
Tooba.AddressBook.Application/Customer/Update/

Create exactly:

CreateCustomerAddressCommand
CreateCustomerAddressCommandHandler

UpdateCustomerAddressCommand
UpdateCustomerAddressCommandHandler

Use MediatR 12.5 IRequest / IRequestHandler pattern consistent with certified modules.

Handlers must depend only on IAddressBookDirectory.

Do NOT access DbContext/EF directly from handlers.

REQUEST SHAPE:

Preserve current Host semantics.

Trusted server context:

ActorUserId

User-controlled write payload fields come from existing CustomerAddressWriteRequest mapping:

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

Reuse existing CustomerAddressWrite application model rather than inventing a duplicate business DTO if practical.

Create command:

ActorUserId + CustomerAddressWrite

Update command:

ActorUserId + AddressId + CustomerAddressWrite

SEMANTIC PARITY:

Create handler:
return addresses.CreateAsync(request.ActorUserId, request.Input, ct)

Update handler:
return addresses.UpdateAsync(request.ActorUserId, request.AddressId, request.Input, ct)

No HTTP status mapping here.
No error-envelope change.
No domain/business rule duplication.

VALIDATION:

Add transport-shape validators only.

Create:

validate only untrusted input shape
do NOT validate ActorUserId
do NOT duplicate domain/business rules from CustomerAddress / AddressBookDirectory

Update:

same input-shape rules
AddressId must be non-empty as route input
do NOT validate ActorUserId

Inspect current CustomerAddress domain/application constraints and use only stable primitive shape that is already established.
Do not invent new business limits.

Prefer:
Validators/Customer/Create/CreateCustomerAddressCommandValidator.cs
Validators/Customer/Update/UpdateCustomerAddressCommandValidator.cs

If shared identical rules are needed, create one small helper:
Validators/AddressBookFluentRules.cs

Do not over-abstract.

NO ENDPOINT MIGRATION:

Tooba.AddressBook.Endpoints must still map ZERO routes after this task.

Host still owns all six AddressBook routes.

Do NOT modify:

Host/AddressBook/AddressBookEndpoints.cs
Host/AddressBook/AddressBookDevelopmentSeed.cs
app.MapAddressBookEndpoints()
AddressBookEndpointModule route mapping
Program route ownership

NO OTHER WRITE CQRS:

Do NOT add:

DeleteCustomerAddressCommand
SetDefaultCustomerAddressCommand

NO ROOT REFACTOR:

Do NOT move:

AddressBookContracts.cs
AddressBookDirectory.cs
CustomerAddress.cs
CustomerAddressContracts.cs
AddressBookModule.cs

BOUNDARIES:

Application new files:

no Host
no Infrastructure
no foreign Application/Domain/Infrastructure
AddressBook.Contracts allowed
existing AddressBook.Application model allowed

FOCUSED TESTS:

If a cheap existing test location is available, add only direct validator/handler tests for Create+Update.

If not, skip test creation and rely on focused builds; document why.

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
docs/ai/tasks/TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-001.task.md

EVIDENCE:

Create:
docs/evidence/TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-001/addressbook-cqrs-write-slice.md

Evidence must include:

exact two commands
exact two handlers
exact validator files
rule classification
handler -> IAddressBookDirectory delegation proof
no Delete/SetDefault CQRS
Host six-route ownership unchanged
Endpoints route count still ZERO
focused build/test results
exact next recommended slice

RECOVERY HONESTY:

AddressBook remains IN_PROGRESS.
NOT COMPLETE_REFERENCE_PATTERN.
NOT STRUCTURE_CERTIFIED.

Expected next task after PASS:
TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002
for Delete + SetDefault only.

PASS ONLY IF:

exactly Create + Update CQRS added
exactly two handlers added
only transport-shape validators added
no Delete/SetDefault CQRS
no HTTP route moved
Endpoints maps ZERO routes
Host still owns all six routes
focused builds pass
tests pass if added
canonical task/evidence committed
hard timebox respected

If safe completion exceeds 8 minutes:
Status = INCOMPLETE
STOP.
Do not start next write slice.
Do not migrate routes.
Do not loop.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-001
Parent-Task: TB-TMAR-ADDRESSBOOK-CQRS-READ-SLICE-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Write-Request-Count:
Write-Handler-Count:
Create-Command-State:
Update-Command-State:
Validator-Classification-State:
Validator-Files:
Application-Folder-State:
Application-Boundary-State:
Directory-Delegation-State:
Delete-SetDefault-State:
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
Do not auto-start next slice.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
