PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ADDRESSBOOK-PRECERT-STRUCTURE-REPAIR-001
Parent-Task: TB-TMAR-ADDRESSBOOK-STRUCTURE-AUDIT-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ADDRESSBOOK
Title: AddressBook pre-cert structure blocker closure
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-ADDRESSBOOK-STRUCTURE-AUDIT-001

ACCEPTED-PARENT-COMMIT:
bb4cc9f1f77d34d368f4278f7086ab89399c5bd3

AUDIT-EVIDENCE:
docs/evidence/TB-TMAR-ADDRESSBOOK-STRUCTURE-AUDIT-001/addressbook-structure-audit.md

TIMEBOX:
Target <= 12 minutes.
Hard maximum 15 minutes.
If safe complete closure cannot finish inside the hard limit:
Status = INCOMPLETE
STOP.
No retry loop.
No scope expansion.
Do not auto-start certification.

ONE OBJECTIVE ONLY:

Close all five certification blockers proven by
TB-TMAR-ADDRESSBOOK-STRUCTURE-AUDIT-001
without changing AddressBook behavior, HTTP ownership, CQRS semantics, data/schema semantics, or frontend/checkout behavior.

THE FIVE AUTHORIZED BLOCKERS:

AB-B1 — APPLICATION ROOT CAPABILITY FILE
Current:
src/backend/Modules/AddressBook/Tooba.AddressBook.Application/AddressBookContracts.cs

It currently contains:

CustomerAddressWrite
IAddressBookDirectory

Required final structure:
src/backend/Modules/AddressBook/Tooba.AddressBook.Application/Customer/Models/CustomerAddressWrite.cs
namespace Tooba.AddressBook.Application.Customer.Models

src/backend/Modules/AddressBook/Tooba.AddressBook.Application/Customer/Ports/IAddressBookDirectory.cs
namespace Tooba.AddressBook.Application.Customer.Ports

Update exact consumers/usings only.
Delete the old root file after all consumers are repointed.
No compatibility shim.
No type alias workaround.
No behavior change.

AB-B2 — INFRASTRUCTURE ROOT IMPLEMENTATION + MIGRATION LOCATION
Current root implementation:
src/backend/Modules/AddressBook/Tooba.AddressBook.Infrastructure/AddressBookDirectory.cs

Move to:
src/backend/Modules/AddressBook/Tooba.AddressBook.Infrastructure/Directories/AddressBookDirectory.cs

Namespace:
Tooba.AddressBook.Infrastructure.Directories

Update registration/consumers only.
Delete old root path.
No compatibility shim.

Current migrations:
src/backend/Modules/AddressBook/Tooba.AddressBook.Infrastructure/Migrations/

Move ALL four audited migration/snapshot files to:
src/backend/Modules/AddressBook/Tooba.AddressBook.Infrastructure/Persistence/Migrations/

Namespace must be:
Tooba.AddressBook.Infrastructure.Persistence.Migrations

Do NOT regenerate migrations.
Do NOT create a new migration.
Do NOT alter migration identifiers, Up/Down logic, snapshot semantics, schema, table names, columns, indexes, constraints, or history.
This is physical/namespace relocation only.

AB-B3 — CONTRACTS ROOT DUMPING
Current:
src/backend/Modules/AddressBook/Tooba.AddressBook.Contracts/CustomerAddressContracts.cs

Move to:
src/backend/Modules/AddressBook/Tooba.AddressBook.Contracts/Customer/CustomerAddressContracts.cs

Namespace:
Tooba.AddressBook.Contracts.Customer

Update exact consumers/usings only.
Delete old root path.
No compatibility shim.
No duplicate contract type.
No behavior change.

AB-B4 — DURABLE VALIDATOR COVERAGE GUARD
Create a durable AddressBook endpoint-reachable request coverage guard in the existing architecture/test location consistent with current TMAR certified-module precedent.

The guard MUST enforce the exact current inventory:

total endpoint-reachable requests = 6
VALIDATOR_REQUIRED = 5
validators present = 5
NO_VALIDATOR_REQUIRED = 1
the only NO_VALIDATOR_REQUIRED request is ListCustomerAddressesQuery
reason: no transport input; actor authority comes from trusted server-side seam
endpoints dispatch these through ISender
no direct IAddressBookDirectory call from endpoints

Do not make the guard brittle to irrelevant whitespace.
Do not implement production behavior in tests.
Do not broaden into a repository-wide architecture rewrite.

AB-B5 — PRE-CERT MANIFEST + SOT READINESS
Update:
docs/architecture/tmar-module-structure-manifests.json

Add AddressBook as a PRE-CERT entry using the post-repair real tree.

Required state:
module = AddressBook
structureCertified = false
lockVersion = ARCH-COMPLETE-002

Required project root allowlists after repair:

Tooba.AddressBook.Contracts = []
Tooba.AddressBook.Domain = [CustomerAddress.cs] if Domain is represented in the manifest convention; otherwise preserve the manifest's established project-scope convention and record Domain separately in evidence
Tooba.AddressBook.Application = []
Tooba.AddressBook.Endpoints = [AddressBookEndpointModule.cs]
Tooba.AddressBook.Infrastructure = [AddressBookModule.cs]

Forbidden root files must include the evacuated legacy names where appropriate:

AddressBookContracts.cs
AddressBookDirectory.cs
CustomerAddressContracts.cs
and any other exact blocker-era root filenames required by the manifest convention.

Infrastructure forbiddenTopLevelFolders must reject:

Migrations
because migrations now belong under Persistence/Migrations.

Do NOT set structureCertified=true in this task.

Update:
docs/architecture/tmar-current-state.json

Only the AddressBook/current-recovery fields necessary to make SoT honest after this repair.

Record at minimum:

audit parent accepted
pre-cert repair task current/accepted state as appropriate
Host AddressBook residue = []
Host HTTP ownership = ZERO
moduleOwnedRouteCount = 6
validator coverage = COMPLETE_5_OF_5_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED
pathNamespace = EXACT after repair
structureCertified = false
certification remains pending
next task = TB-TMAR-ADDRESSBOOK-ARCH-COMPLETE-002-STRUCTURE-001 ONLY IF all repair success criteria pass

Do not rewrite unrelated historical TMAR state.
Do not reopen certified modules.
Do not alter checkout/frontend state.

PATH ↔ NAMESPACE REQUIREMENT:

After all moves:

every non-generated AddressBook production .cs file must have exact physical-path-derived namespace
no using alias may hide a mismatch
no stale namespace references may remain

CROSS-MODULE BOUNDARY REQUIREMENT:

Preserve:
AddressBook -> Order.Contracts only.

Forbidden:
AddressBook -> Order.Application
AddressBook -> Order.Infrastructure
AddressBook -> Order.Domain
AddressBook -> Host

The existing Order.Contracts guest actor authority must remain canonical.

HOST / HTTP / CQRS PROTECTED STATE:

MUST REMAIN TRUE:

Host AddressBook folder residue = ZERO
Host AddressBook HTTP ownership = ZERO
all 6 routes remain module-owned
AddressBookEndpointModule remains the single endpoint composition entry
all six requests remain IRequest-based
all endpoint dispatch remains ISender-based
all six handlers remain real MediatR handlers
actor seam behavior unchanged
no direct directory call from endpoints
no legacy dispatcher introduced

PERSISTENCE PROTECTED STATE:

no schema change
no migration regeneration
no migration semantic change
AddressBookDbContext behavior unchanged
seed behavior unchanged
deterministic seed ids unchanged
idempotency unchanged
database schema name unchanged

DO NOT TOUCH:

frontend
Checkout behavior
Cart/Order/Payment behavior
existing certified modules except unavoidable compile-using updates in exact consumers (prefer none outside AddressBook/Host tests)
Host AddressBook endpoint ownership
actor semantics
business validation/domain rules
route shapes
HTTP status semantics
response DTO semantics
Request/Handler behavior

NON-BLOCKING DEBT — DO NOT FIX HERE:

no dedicated AddressBook.Tests project
Host CustomerPanelComposer AddressBook Application read-port consumption
Development folder precedent concern

Those are explicitly out of scope for this repair unless a compile failure proves a narrow namespace-only consumer update is required.

EXPECTED POST-REPAIR STRUCTURE:

Contracts:
Tooba.AddressBook.Contracts/
└─ Customer/
└─ CustomerAddressContracts.cs

Domain:
Tooba.AddressBook.Domain/
└─ CustomerAddress.cs

Application:
Tooba.AddressBook.Application/
├─ Customer/
│ ├─ Create/
│ ├─ Delete/
│ ├─ Get/
│ ├─ List/
│ ├─ SetDefault/
│ ├─ Update/
│ ├─ Models/
│ │ └─ CustomerAddressWrite.cs
│ └─ Ports/
│ └─ IAddressBookDirectory.cs
└─ Validators/...

Infrastructure:
Tooba.AddressBook.Infrastructure/
├─ AddressBookModule.cs
├─ Development/
│ └─ AddressBookDevelopmentSeed.cs
├─ Directories/
│ └─ AddressBookDirectory.cs
└─ Persistence/
├─ AddressBookDbContext.cs
└─ Migrations/
├─ 20260825171858_InitialAddressBook.cs
├─ 20260825171858_InitialAddressBook.Designer.cs
├─ 20260913180000_AddRecipientNameParts.cs
└─ AddressBookDbContextModelSnapshot.cs

Endpoints:
Tooba.AddressBook.Endpoints/
├─ AddressBookEndpointModule.cs
└─ Customer/...

FOCUSED VALIDATION:

Required builds:
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Contracts/Tooba.AddressBook.Contracts.csproj --no-restore
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Application/Tooba.AddressBook.Application.csproj --no-restore
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Infrastructure/Tooba.AddressBook.Infrastructure.csproj --no-restore
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Required focused tests:
dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~AddressBookFoundationTests

Run the newly added AddressBook validator coverage/structure guard by its exact focused filter as well.

No solution build.
No broad integration suite.
No broad architecture suite.
No retry cascade.

MANDATORY SOURCE CHECKS:

Prove after repair:

old Application/AddressBookContracts.cs absent
old Infrastructure/AddressBookDirectory.cs absent
old Infrastructure/Migrations folder absent
old Contracts/CustomerAddressContracts.cs absent
no AddressBook production namespace mismatch
no AddressBook using-alias workaround
no AddressBook Order.Application / Order.Infrastructure / Order.Domain / Host dependency
6 endpoint requests unchanged
validator counts unchanged 5/5 + 1
Host AddressBook folder absent
Host owned route count 0

CANONICAL TASK ARTIFACT:

Commit the exact received task at:
docs/ai/tasks/TB-TMAR-ADDRESSBOOK-PRECERT-STRUCTURE-REPAIR-001.task.md

EVIDENCE:

Create:
docs/evidence/TB-TMAR-ADDRESSBOOK-PRECERT-STRUCTURE-REPAIR-001/addressbook-precert-structure-repair.md

Evidence must include:

parent audit + commit
exact blocker closure matrix AB-B1..AB-B5
old → new file map
namespace old → new map
exact consumer updates
migration relocation proof and explicit NO_SCHEMA_CHANGE
final root .cs lists for all five projects
final path↔namespace state
endpoint request/handler/validator matrix
durable guard location and what it enforces
Host reference/ownership preservation
cross-module dependency proof
manifest changes with structureCertified=false
SoT changes
focused build results
focused test/guard results
explicit untouched list
residual non-blocking debt
exact next task

PASS ONLY IF:

all five audited certification blockers are closed
no production behavior changes
no schema/migration semantic change
Application root capability file removed
Infrastructure root directory implementation removed
top-level Infrastructure/Migrations removed
Contracts root DTO/port dumping removed
path↔namespace exact after moves
validator coverage still exactly 5 REQUIRED / 5 PRESENT / 1 NO_VALIDATOR_REQUIRED
durable coverage guard exists and passes
manifest has an honest AddressBook pre-cert entry with structureCertified=false
SoT is honest and points to final certification next
Host residue remains ZERO
Host HTTP ownership remains ZERO
all 6 routes remain module-owned via ISender
cross-module boundary remains Order.Contracts-only
focused builds pass
focused tests/guards pass
task and evidence committed
push succeeds
after git fetch, HEAD == origin/main
user work preserved

If any blocker cannot be safely closed:
Status = INCOMPLETE
report the exact blocker
STOP.

EXPECTED NEXT TASK AFTER PASS:

TB-TMAR-ADDRESSBOOK-ARCH-COMPLETE-002-STRUCTURE-001

Do NOT start it automatically.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ADDRESSBOOK-PRECERT-STRUCTURE-REPAIR-001
Parent-Task: TB-TMAR-ADDRESSBOOK-STRUCTURE-AUDIT-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Blocker-Closure-State:
AB-B1-Application-State:
AB-B2-Infrastructure-State:
AB-B3-Contracts-State:
AB-B4-Validator-Guard-State:
AB-B5-Manifest-SoT-State:
Old-Paths-State:
New-Paths-State:
Migration-Relocation-State:
Schema-Change-State:
Path-Namespace-State:
Root-Allowlist-State:
Endpoint-Reachable-Request-Count:
Validator-Required-Count:
Validators-Present-Count:
No-Validator-Required-Count:
Validator-Coverage-State:
MediatR-CQRS-State:
Host-AddressBook-Residue-State:
Host-HTTP-Ownership-State:
Cross-Module-Boundary-State:
Order-Contracts-Dependency-State:
Alias-Workaround-State:
Manifest-State:
Structure-Certified-State:
SoT-State:
Focused-Builds:
Focused-Tests:
Focused-Guard-Tests:
Canonical-Task-State:
Evidence:
Production-Code-Scope:
Test-Code-Scope:
Residual-Debt:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP
Do not auto-start certification.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK