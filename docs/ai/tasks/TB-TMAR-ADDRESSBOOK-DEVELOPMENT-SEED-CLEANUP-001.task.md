PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ADDRESSBOOK-DEVELOPMENT-SEED-CLEANUP-001
Parent-Task: TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-002
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ADDRESSBOOK
Title: AddressBook development seed evacuation from Host
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-002

ACCEPTED-PARENT-COMMIT:
37f4d7507eb7e884f8f8394be9f985e6cdb75bf5

RECOVERY-SOT-CHECKPOINT:
bf57df9053b32e67efa27a13ea2fcdd16ddfbb57

TIMEBOX:
Target <= 6 minutes.
Hard maximum 8 minutes.
If safe completion exceeds hard limit, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.

ONE OBJECTIVE ONLY:

Move AddressBookDevelopmentSeed out of Host and into AddressBook.Infrastructure/Development.

CURRENT HOST RESIDUE:

src/backend/Host/Tooba.Host/AddressBook/AddressBookDevelopmentSeed.cs

This is now the only Host AddressBook file.

MOVE TARGET:

Create:

src/backend/Modules/AddressBook/Tooba.AddressBook.Infrastructure/Development/AddressBookDevelopmentSeed.cs

Namespace:
Tooba.AddressBook.Infrastructure.Development

Preserve the existing seed semantics:

DefaultAddressId unchanged
AlternateAddressId unchanged
deterministic createdAt unchanged
same two CustomerAddress.Create calls
same idempotency AnyAsync checks
same SaveChangesAsync behavior

GUEST ACTOR AUTHORITY:

Replace the old Order.Application dependency:

Tooba.Order.Application.Storefront.Services.StorefrontCheckoutService.StorefrontGuestActorId

with the canonical Contracts authority:

Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId

Do NOT reference Order.Application anywhere in the moved seed.

INFRASTRUCTURE REFERENCES:

The moved seed may use:

AddressBookDbContext
AddressBook.Domain.CustomerAddress
Order.Contracts guest actor constant
EF Core
IServiceProvider

If Tooba.AddressBook.Infrastructure.csproj lacks the Order.Contracts reference, add exactly that ProjectReference and nothing broader.

Do NOT add Host reference.

HOST BOOTSTRAP CALL SITES:

Update only the two existing call sites in:

src/backend/Host/Tooba.Host/Admin/ProductWorkspaceDevelopmentBootstrap.cs

Current calls are around:

development bootstrap path
reset/reseed path

Repoint both from Host AddressBookDevelopmentSeed to:

Tooba.AddressBook.Infrastructure.Development.AddressBookDevelopmentSeed.ApplyAsync(...)

Prefer one using if it keeps the file clean.

Preserve the exact call count and timing:

exactly 2 calls remain
same ordering relative to Reviews/Wishlist/CustomerProfile seed calls
same cancellation behavior

HOST CLEANUP:

Delete:
src/backend/Host/Tooba.Host/AddressBook/AddressBookDevelopmentSeed.cs

After this task:
src/backend/Host/Tooba.Host/AddressBook/
must contain ZERO files / effectively disappear.

Do NOT modify any HTTP endpoints; Host AddressBook HTTP ownership is already ZERO.

DO NOT TOUCH:

AddressBook CQRS requests/handlers/validators
AddressBook.Endpoints
AddressBookEndpointModule
AddressBook actor seam
AddressBook root structure
AddressBookContracts.cs
AddressBookDirectory.cs
AddressBookModule.cs
CustomerAddress.cs
final certification manifests
frontend
checkout logic

NO ROOT/CAPABILITY REFACTOR IN THIS TASK.

BASELINE / GUARD UPDATE:

If an existing focused source-text or host-write baseline directly lists Host/AddressBook/AddressBookDevelopmentSeed.cs, update only that exact focused baseline/guard to reflect the move.

Do NOT touch unrelated stale repository-wide TMAR baselines.

FOCUSED VALIDATION:

Build only:
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Infrastructure/Tooba.AddressBook.Infrastructure.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Run only directly relevant AddressBookFoundation tests if they cover seed placement/idempotency:
dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~AddressBookFoundationTests

No solution build.
No broad integration suite.
No broad architecture suite.
No retries.

CANONICAL TASK ARTIFACT:

Commit exact task at:
docs/ai/tasks/TB-TMAR-ADDRESSBOOK-DEVELOPMENT-SEED-CLEANUP-001.task.md

EVIDENCE:

Create:
docs/evidence/TB-TMAR-ADDRESSBOOK-DEVELOPMENT-SEED-CLEANUP-001/addressbook-development-seed-cleanup.md

Evidence must include:

old Host path
new Infrastructure/Development path
namespace
guest actor authority = Order.Contracts
proof of zero Order.Application reference in moved seed
exact two Host bootstrap call sites preserved
Host AddressBook folder residue = ZERO
idempotency semantics preserved
focused build/test results
any focused baseline/guard updated
exact next recommended task

RECOVERY HONESTY:

After PASS:

Host AddressBook residue should be ZERO
AddressBook still NOT yet STRUCTURE_CERTIFIED
root/capability cleanup may remain
final ARCH-COMPLETE-002 certification still pending

Expected next task after PASS:
a bounded AddressBook structure/pre-cert audit or cleanup task.
Do NOT start automatically.

PASS ONLY IF:

seed moved to Infrastructure/Development
Host seed file deleted
Host AddressBook folder residue ZERO
exactly two bootstrap call sites preserved
Order.Application guest actor dependency removed
Order.Contracts guest actor constant used
seed semantics/idempotency preserved
no endpoint/CQRS changes
focused builds pass
focused tests pass if applicable
canonical task/evidence committed
hard timebox respected

If safe completion exceeds 8 minutes:
Status = INCOMPLETE
STOP.
Do not start root refactor.
Do not start certification.
Do not loop.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ADDRESSBOOK-DEVELOPMENT-SEED-CLEANUP-001
Parent-Task: TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-002
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Seed-Old-Path-State:
Seed-New-Path-State:
Seed-Namespace-State:
Guest-Actor-Authority-State:
Order-Application-Dependency-State:
Bootstrap-Callsite-State:
Seed-Semantics-State:
Host-AddressBook-Residue-State:
Infrastructure-Project-Reference-State:
Focused-Baseline-Guard-State:
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
Do not auto-start structure cleanup.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK