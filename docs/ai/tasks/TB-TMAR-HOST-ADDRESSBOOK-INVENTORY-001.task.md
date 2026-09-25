PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001
Parent-Task: TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ADDRESSBOOK
Title: AddressBook Host inventory and ownership classification
Backend-Only: YES
Audit-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001-R1

ACCEPTED-PARENT-COMMIT:
227760882efe6ce3b5c1f81a481e5d212a836d4f

TIMEBOX:
Target <= 6 minutes.
Hard maximum 8 minutes.
If safe completion exceeds hard limit, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.
No production code changes.

ONE OBJECTIVE:

Inventory the two current Host AddressBook files completely and produce a member-level Content Disposition Map plus the smallest safe migration plan.

CURRENT VERIFIED HOST FILES:

src/backend/Host/Tooba.Host/AddressBook/AddressBookEndpoints.cs
src/backend/Host/Tooba.Host/AddressBook/AddressBookDevelopmentSeed.cs

DO NOT migrate them in this task.

MANDATORY AUDIT 1 — AddressBookEndpoints.cs:

Read the file completely and classify every responsibility/member, including:

MapAddressBookEndpoints
GET /v1/customer/addresses
GET /v1/customer/addresses/{addressId:guid}
POST /v1/customer/addresses
PUT /v1/customer/addresses/{addressId:guid}
DELETE /v1/customer/addresses/{addressId:guid}
POST /v1/customer/addresses/{addressId:guid}/default
ResolveActor
Unauthorized
DevActorHeader
CustomerAddressWriteRequest
CustomerAddressWriteRequestExtensions
ToWrite

For each member classify destination ownership:

AddressBook.Endpoints
AddressBook.Application
AddressBook.Contracts
shared neutral security seam
Development-only seam
REMOVE only if proven dead/syntax-only

Do not guess.
Do not delete anything.

MANDATORY AUDIT 2 — AddressBookDevelopmentSeed.cs:

Read the file completely and classify:

DefaultAddressId
AlternateAddressId
ApplyAsync
direct AddressBookDbContext use
CustomerAddress.Create use
StorefrontGuestActorId dependency
deterministic demo address data
Program/bootstrap call sites

Determine whether the seed should:

remain Host composition-only,
move to AddressBook.Infrastructure/Development,
split across module + Host thin trigger,
or be retired if truly obsolete.

No implementation in this task.

BOUNDARY AUDIT:

Record all current AddressBook Host dependencies, especially:

Host AddressBook -> AddressBook.Application
Host AddressBook -> AddressBook.Infrastructure.Persistence
Host AddressBook -> AddressBook.Domain
Host AddressBook -> Tooba.Host.Storefront
Host AddressBook -> Order.Application.Storefront.Services.StorefrontCheckoutService.StorefrontGuestActorId

Classify which are architectural leaks vs acceptable temporary Host composition.

MODULE READINESS AUDIT:

Inventory current AddressBook module structure:

Tooba.AddressBook.Application
Tooba.AddressBook.Contracts
Tooba.AddressBook.Domain
Tooba.AddressBook.Infrastructure

Record:

current root .cs files
current CQRS/MediatR presence or absence
current Endpoints project presence or absence
current validators presence/absence
current Contracts contents
Infrastructure structure
whether Host routes call IAddressBookDirectory directly
whether a proper AddressBook.Endpoints project must be created before migration
whether Application needs CQRS request/handler extraction
whether root structure already violates ARCH-COMPLETE-002

Do NOT repair structure in this task.

SEMANTIC PARITY INVENTORY:

For each HTTP route record:

auth/actor source
dev/testing fallback behavior
request DTO
response shape/status
not-found behavior
unauthorized behavior
directory method invoked
trace/correlation behavior if any
validation behavior if any

Do not normalize behavior yet.

SPECIAL ACTOR AUTHORITY AUDIT:

ResolveActor currently uses:

authenticated session
X-Tooba-Dev-Actor-User-Id in Development/Testing
StorefrontCheckoutService.StorefrontGuestActorId fallback in Development/Testing

Classify this carefully.

Do NOT move the Order.Application StorefrontGuestActorId dependency into AddressBook.Application/Endpoints.

Record the correct neutral ownership decision needed for the migration.

PROGRAM / CALL-SITE INVENTORY:

Find and record exact production call sites for:

app.MapAddressBookEndpoints()
AddressBookDevelopmentSeed.ApplyAsync(...)

Do not modify Program.cs.

OUTPUT PLAN:

Evidence must recommend the smallest bounded follow-up sequence.

Prefer 2-3 small tasks maximum, for example:

AddressBook module foundation/endpoints/CQRS
Host endpoint evacuation
Development seed/Program cleanup

But derive actual sequence from repository evidence.

Do not create those tasks automatically.

RECOVERY SOT:

Do not rewrite broad SoT.
Only create evidence and canonical task artifact.

CANONICAL TASK ARTIFACT:

Commit this exact task at:
docs/ai/tasks/TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001.task.md

EVIDENCE:

Create:
docs/evidence/TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001/addressbook-host-inventory.md

Evidence must include:

exact two-file inventory
member-level Content Disposition Map
six-route semantic parity table
actor authority classification
dependency-edge audit
module readiness audit
Program/bootstrap call-site inventory
recommended bounded follow-up sequence
explicit statement: production code changes = NONE

VALIDATION:

Audit-only.
No dotnet build required.
No tests required.
No solution build.
No broad search loop.
Use only the repository reads needed to complete the evidence.

PASS ONLY IF:

both Host files were read completely
every live member is classified
all six routes are inventoried
actor fallback authority is classified
seed ownership is classified
module readiness is documented
no production/test code changed
canonical task/evidence committed
hard timebox respected

If audit cannot safely finish inside 8 minutes:
Status = INCOMPLETE
STOP.
Do not start implementation.
Do not loop.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001
Parent-Task: TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Host-File-Inventory:
Endpoint-Route-Count:
Endpoint-Member-Disposition-State:
Seed-Member-Disposition-State:
Actor-Authority-State:
Host-Dependency-Audit:
Module-Readiness-State:
CQRS-State:
Endpoints-Project-State:
Validator-State:
Root-Structure-State:
Program-Map-Call-State:
Program-Seed-Call-State:
Semantic-Parity-State:
Recommended-Followup-Sequence:
Production-Code-Changes:
Test-Code-Changes:
Canonical-Task-State:
Evidence:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP
Do not auto-start implementation.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
