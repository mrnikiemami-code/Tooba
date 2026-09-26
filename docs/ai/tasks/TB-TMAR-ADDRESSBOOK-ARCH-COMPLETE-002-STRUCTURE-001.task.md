PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ADDRESSBOOK-ARCH-COMPLETE-002-STRUCTURE-001
Parent-Task: TB-TMAR-ADDRESSBOOK-PRECERT-STRUCTURE-REPAIR-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ADDRESSBOOK
Title: AddressBook ARCH-COMPLETE-002 final structure certification
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-ADDRESSBOOK-PRECERT-STRUCTURE-REPAIR-001

ACCEPTED-PARENT-COMMIT:
898072c18ceb3c7a0854cc12aa7c719dcbf1fef9

PARENT-EVIDENCE:
docs/evidence/TB-TMAR-ADDRESSBOOK-PRECERT-STRUCTURE-REPAIR-001/addressbook-precert-structure-repair.md

USER-STOP-GATE:
After this task PASS and Architect verification, STOP AddressBook work and DO NOT start Authentication or any other module/folder until explicit user release.

TIMEBOX:
Target <= 10 minutes.
Hard maximum 15 minutes.
If certification cannot be completed safely inside the hard limit:
Status = INCOMPLETE
STOP.
No retry loop.
No scope expansion.
Do not start another module.

ONE OBJECTIVE ONLY:

Perform the final ARCH-COMPLETE-002 certification of AddressBook using the already-repaired physical structure and durable validator guard.

This is a certification/lock task, NOT a new refactor wave.

CURRENT PRE-CERT STATE — MUST BE VERIFIED, NOT ASSUMED:

Host AddressBook folder residue = ZERO
Host AddressBook HTTP ownership = ZERO
all 6 AddressBook routes are module-owned
all endpoints dispatch through ISender
MediatR 12.5 real handlers exist for all 6 use cases
validator coverage = 5 VALIDATOR_REQUIRED / 5 PRESENT / 1 NO_VALIDATOR_REQUIRED
NO_VALIDATOR_REQUIRED = ListCustomerAddressesQuery only
Application root capability file evacuated
Infrastructure root directory implementation evacuated
top-level Infrastructure/Migrations evacuated to Persistence/Migrations
Contracts root dumping evacuated
path↔namespace = EXACT
alias workaround = NONE
AddressBook foreign module dependency = Order.Contracts only
AddressBook pre-cert manifest entry exists with structureCertified=false
AddressBookValidatorCoverageGuardTests exists and passes
no production behavior/schema/frontend/checkout change from the repair

MANDATORY CERTIFICATION CHECKS:

PHYSICAL STRUCTURE
Verify the real current tree against:
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md

Required root state:

Contracts root .cs = none
Domain root = CustomerAddress.cs only
Application root .cs = none
Endpoints root = AddressBookEndpointModule.cs only
Infrastructure root = AddressBookModule.cs only

Required capability structure:

Application Customer capability + Validators
Endpoints Customer capability
Infrastructure Development / Directories / Persistence / Persistence/Migrations
Contracts Customer capability

PATH ↔ NAMESPACE
Verify exact equality for every non-generated AddressBook production .cs file.
No mismatch.
No using alias workaround.
No compatibility shim.
No TypeForwardedTo workaround.

ENDPOINT OWNERSHIP
Verify:

exactly 6 module-owned routes
Host-owned AddressBook routes = 0
Host AddressBook folder absent
AddressBookEndpointModule is the sole module endpoint composition root
Host only maps the module composition entry
no duplicate route ownership
CQRS
Verify all 6 endpoint-reachable requests:
real IRequest/IRequest<T>
real IRequestHandler implementations
ISender dispatch
no endpoint direct IAddressBookDirectory call
no legacy dispatcher
VALIDATION
Verify durable guard inventory:
total = 6
VALIDATOR_REQUIRED = 5
validators present = 5
NO_VALIDATOR_REQUIRED = 1
only ListCustomerAddressesQuery is NO_VALIDATOR_REQUIRED
justification remains NO_TRANSPORT_INPUT / trusted actor seam

The durable guard itself MUST PASS.

CROSS-MODULE BOUNDARIES
Verify AddressBook production references:
Order.Contracts allowed
Order.Application = ZERO
Order.Infrastructure = ZERO
Order.Domain = ZERO
Host = ZERO
HOST AUTHORITY
Verify:
Host business authority for AddressBook = NONE
Host persistence authority for AddressBook = NONE
Host endpoint ownership = ZERO
remaining Host references are only legitimate composition/consumer edges already classified

Do not attempt to make all Host textual references zero if they are legitimate composition or contract consumption.

MIGRATION / SCHEMA SAFETY
Verify migration relocation did not change:
migration ids
Up/Down behavior
snapshot semantics
AddressBookDbContext schema
tables/columns/indexes/constraints

No new migration.
No schema change.

MANIFEST PROMOTION
Update:
docs/architecture/tmar-module-structure-manifests.json

Promote AddressBook from preCertModules to the certified modules collection.

Required final AddressBook state:
module = AddressBook
structureCertified = true
lockVersion = ARCH-COMPLETE-002

Use the exact post-repair project root allowlists and forbidden-root/top-level-folder rules already proven in pre-cert.

Remove the AddressBook pre-cert duplicate after successful promotion.
There must be exactly ONE AddressBook manifest entry after certification.

Do not modify unrelated certified module definitions.

STRUCTURE LOCK / CURRENT STATE
Update:
docs/architecture/tmar-current-state.json

Required final AddressBook state:

lastAccepted/closure fields honest for this certification
AddressBook state = COMPLETE_REFERENCE_PATTERN
structureCertifiedUnderArchComplete002 = true
endpointOwnership = MODULE_ENDPOINTS / MODULE_OWNED_6_OF_6
cqrs = MEDIATR_12_5
validatorCoverage = COMPLETE_5_OF_5_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED
pathNamespace = EXACT
aliasWorkaround = NONE
hostAddressBookResidue = []
hostOwnedRouteCount = 0
hostBusinessAuthority = NONE
crossModuleBoundary = ORDER_CONTRACTS_ONLY
certificationPending = false
certifiedModules / structureLock list includes AddressBook exactly once

Do NOT set a next implementation module/folder.
Because the user explicitly requested a stop after AddressBook:
nextTask / recovery next must be a USER_REVIEW_ADDRESSBOOK_CHECKPOINT or equivalent non-implementation stop gate.
Do NOT point to Authentication yet.

DURABLE GUARD DRIFT CLOSURE
The parent disclosed stale assertions in TmarDurableGuardTests.

This certification task MUST repair only the stale AddressBook/current-recovery pins necessary to make the durable guard agree with the certified SoT.

Specifically inspect:
src/backend/Host/Tooba.Host.Tests/Architecture/TmarDurableGuardTests.cs
or its actual current path if moved.

Update only stale expected values tied to:

AddressBook current/next task state
accessControlEvacuation/current recovery pin if that assertion is demonstrably stale solely because global recovery advanced

Do NOT weaken or delete the guard.
Do NOT replace exact assertions with vague always-pass logic.
Do NOT broaden to unrelated recovery history.

After update, the exact durable recovery guard test MUST PASS.

COMPLETE_REFERENCE_PATTERN CLAIM
Only after every check above passes:
AddressBook may be certified as:
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
HTTP_OWNING
MODULE_ENDPOINTS
MEDIATR_12_5

No partial claim.

PROTECTED STATE:

MUST NOT CHANGE:

AddressBook behavior
route shapes
HTTP status semantics
DTO semantics
CQRS handlers/use-case behavior
actor seam behavior
seed semantics/idempotency
Order.Contracts guest actor authority
DB schema
migration semantics
checkout behavior
frontend
Cart/Order/Payment behavior
other certified modules
Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT
frontendFrozen = true

NON-BLOCKING DEBT — DO NOT FIX:

dedicated AddressBook.Tests project absent
Host CustomerPanelComposer direct Application read-port consumption
Development folder precedent concern

Record them honestly, but do not expand scope.

ALLOWED PRODUCTION CHANGES:

Prefer NONE.

Only certification metadata/docs and stale durable guard expectations should change.

No AddressBook production refactor is authorized unless a concrete certification-breaking mismatch proves the parent PASS false. If such mismatch exists:
Status = INCOMPLETE
report it
STOP.
Do not silently repair production structure in certification.

FOCUSED VALIDATION — REQUIRED:

Build:
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Contracts/Tooba.AddressBook.Contracts.csproj --no-restore
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Application/Tooba.AddressBook.Application.csproj --no-restore
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Infrastructure/Tooba.AddressBook.Infrastructure.csproj --no-restore
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --no-restore

Tests:
dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~AddressBookFoundationTests
dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~AddressBookValidatorCoverageGuardTests
dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~TmarCompleteReferenceStructureGateTests
dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~TmarDurableGuardTests

All required focused tests MUST PASS.
No known-failing durable guard may remain at final certification.

No solution build.
No broad integration suite.
No broad architecture suite.
No retry cascade.

CANONICAL TASK ARTIFACT:

Commit exact received task:
docs/ai/tasks/TB-TMAR-ADDRESSBOOK-ARCH-COMPLETE-002-STRUCTURE-001.task.md

EVIDENCE:

Create:
docs/evidence/TB-TMAR-ADDRESSBOOK-ARCH-COMPLETE-002-STRUCTURE-001/addressbook-structure-certification.md

Evidence must include:

parent repair commit
exact final physical tree
final root allowlists
path↔namespace proof
alias/shim proof
route ownership proof 6 module / 0 Host
route→request→handler→validator matrix
validator coverage 5/5 + 1
durable validator guard PASS
durable recovery guard drift repair and PASS
Host authority classification
cross-module dependency proof
migration/schema no-change proof
manifest promotion proof
exactly one AddressBook manifest entry
structure lock / certifiedModules update
final SoT AddressBook closure state
focused build results
all focused test results
residual non-blocking debt
explicit untouched list
USER_REVIEW_ADDRESSBOOK_CHECKPOINT stop state

PASS ONLY IF:

parent pre-cert claims are reverified
zero certification blocker remains
physical structure complies with ARCH-COMPLETE-002
path↔namespace exact
no alias/shim workaround
Host AddressBook residue zero
Host HTTP ownership zero
6 routes module-owned
all six use cases CQRS/ISender/MediatR
validator coverage exactly 5/5 + 1
AddressBook durable validator guard passes
TmarCompleteReferenceStructureGateTests passes
TmarDurableGuardTests passes
cross-module boundary remains Order.Contracts-only
no schema/migration semantic change
manifest promotes AddressBook to structureCertified=true
AddressBook removed from preCertModules
exactly one AddressBook manifest entry remains
tmar-current-state records COMPLETE_REFERENCE_PATTERN + ARCH-COMPLETE-002 certification
no next implementation task is issued
recovery next state is USER_REVIEW_ADDRESSBOOK_CHECKPOINT
all required builds pass
all required focused tests pass
canonical task/evidence committed
push succeeds
after git fetch HEAD == origin/main
user work preserved

If any required certification test fails:
Status = INCOMPLETE
do not claim certification
STOP.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ADDRESSBOOK-ARCH-COMPLETE-002-STRUCTURE-001
Parent-Task: TB-TMAR-ADDRESSBOOK-PRECERT-STRUCTURE-REPAIR-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Physical-Structure-State:
Root-Allowlist-State:
Path-Namespace-State:
Alias-Shim-State:
Endpoint-Ownership-State:
Module-Owned-Route-Count:
Host-Owned-Route-Count:
MediatR-CQRS-State:
Endpoint-Reachable-Request-Count:
Validator-Required-Count:
Validators-Present-Count:
No-Validator-Required-Count:
Validator-Coverage-State:
Validator-Guard-State:
Durable-Recovery-Guard-State:
Host-AddressBook-Residue-State:
Host-Business-Authority-State:
Host-Persistence-Authority-State:
Cross-Module-Boundary-State:
Order-Contracts-Dependency-State:
Migration-Schema-State:
Manifest-Promotion-State:
AddressBook-Manifest-Entry-Count:
Structure-Lock-State:
Structure-Certified-State:
Complete-Reference-Pattern-State:
SoT-State:
Focused-Builds:
Focused-Tests:
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
AddressBook closure only.
Do NOT start Authentication.
Do NOT start another Host folder/module.
Do NOT poll.
Wait for explicit user review/release.

END_TOOBA_TASK