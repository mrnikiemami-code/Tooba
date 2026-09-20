PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-HOST-W1

Parent-Task:
TB-TMAR-FND-001

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Status:
ISSUED

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Title:
TMAR Host Wave 1 — Remove Dangerous Direct Host Writes, Slice 1

Task Type:
IMPLEMENTATION — NARROW REFACTOR

0. Architect Intent

Foundation is accepted.

This task begins Host cleanup, but only with the FIRST narrow write-removal slice.

Primary goal:
Remove direct business writes from Host for the Store Landing/Page Composition path without changing product behavior and without prematurely moving bounded-context ownership.

This is NOT:

full Host rewrite

full CQRS migration

Contracts extraction wave

Domain ownership move

folder reorganization

Redis work

broad cleanup

Preserve all current behavior.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

External architecture bootstrap reference provided by user:
D:\Users\User\source\repos\SarvNewVerRequirment\reference\Tooba-Architect-Bootstrap.md

Before modifying anything record:

branch

exact HEAD SHA

exact origin/main SHA

HEAD==origin/main

git status --short

git diff --name-only

git diff --cached --name-only

confirm 18ca10c9 is ancestor of HEAD

If conflicting tracked user work exists:
STOP with RECOVERY_CONFLICT.

Forbidden:

git reset

git clean

destructive checkout/restore

unsafe rebase

blind stash manipulation

broad git add .

Evidence:
docs/evidence/TB-TMAR-HOST-W1/recovery-start.md

2. Bootstrap Persistence

Read the user-provided file:

D:\Users\User\source\repos\SarvNewVerRequirment\reference\Tooba-Architect-Bootstrap.md

Copy it into the repository as:

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

Do not rewrite its intent.

If repository TMAR locks / Recovery SoT contain newer factual state, update ONLY the current-state section of the repository bootstrap so it reflects:

Last Product Task = TB-P10-T022-R21

Architecture Baseline = TB-TMAR-ARCH-BASELINE

Foundation = TB-TMAR-FND-001 ACCEPTED

Current Task = TB-TMAR-HOST-W1

primary goal = painless future Microservice migration

The repository copy becomes the durable recovery entry point.

Evidence:
docs/evidence/TB-TMAR-HOST-W1/bootstrap.md

3. Read Current Architecture Locks

Read:

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/evidence/TB-TMAR-ARCH-BASELINE/host-audit.md

docs/evidence/TB-TMAR-ARCH-BASELINE/host-cleanup-waves.md

docs/evidence/TB-TMAR-ARCH-BASELINE/domain-ownership-map.md

docs/evidence/TB-TMAR-FND-001/recovery-sot.md

Do NOT repeat the full architecture audit.

4. Exact Scope — Store Landing/Page Composition Direct Writes Only

Start from the known high-risk Host write path around:

StoreLandingPageComposer

direct CatalogDbContext writes

Add / Update / Delete

SaveChangesAsync

BeginTransactionAsync

Inspect the exact current implementation and enumerate every write method in this slice.

The final refactor must remove Host ownership of those writes.

Do NOT automatically include other Host areas unless they are strictly required by this slice.

Evidence:
docs/evidence/TB-TMAR-HOST-W1/scope.md

5. Ownership Safety

IMPORTANT:

StoreLandingPage* ownership was flagged as potentially misplaced in Catalog.Domain.

Therefore:

do NOT move StoreLandingPage* Domain types in this task

do NOT create a new bounded context without approval

do NOT rename namespaces/projects

do NOT deepen Host→Catalog persistence coupling

do NOT introduce new Catalog-only assumptions that would make later ownership migration harder

This task is a boundary cleanup, not the final bounded-context relocation.

Any transitional Application handler must be explicitly documented as temporary/current-owner orchestration.

6. CQRS Write Boundary

For each Store Landing/Page Composition write use-case in scope:

Target flow:

HTTP Endpoint / Host Adapter
→ ISender
→ Command
→ Handler
→ current owning module persistence/repository abstraction
→ module DbContext

Host must no longer call:

Add/Update/Remove on business DbContext

SaveChangesAsync for this business write

BeginTransactionAsync for this business write

Use MediatR 12.5.0 foundation from TB-TMAR-FND-001.

Use FluentValidation for NEW request validation where applicable.

Do not duplicate existing validation logic blindly.
Move/centralize only what is required to establish the command boundary safely.

7. Transaction Boundary

If the current Host path has an explicit transaction:

preserve the same atomic behavior

move transaction responsibility into the correct Application/Infrastructure boundary

do NOT add a generic/global TransactionBehavior unless the evidence proves it is safe

do NOT change isolation level or commit semantics without explicit justification

Document before/after transaction ownership.

Evidence:
docs/evidence/TB-TMAR-HOST-W1/transaction-boundary.md

8. Error / Localization Compliance

Any NEW code introduced in this task must follow TMAR error rules:

semantic error code

no new hardcoded Persian/English user-facing Domain messages

Host/API maps errors to existing HTTP/ProblemDetails boundary

unlimited-locale-safe

Do not mass-migrate old exception strings outside this slice.

9. Time / ID Compliance

Any NEW code introduced in this task must use:

IClock for orchestration time

IIdGenerator for new IDs where needed

Do not introduce new:

DateTime.UtcNow

DateTimeOffset.UtcNow

Guid.NewGuid()

UuidV7.New()

in protected Application/Domain paths.

Existing legacy calls outside this slice remain for later migration.

10. Host Result

After refactor, for this slice:

Host may:

bind HTTP input

authorize/authenticate

call ISender

map result to HTTP response

Host must NOT:

own persistence

own transaction

own business decision

call CatalogDbContext write APIs directly

The relevant Host composer should become either:

read-only composition, or

thin command dispatch adapter

No behavior redesign.

11. Architecture Guards

Extend existing TMAR freeze guards so this removed debt cannot reappear.

Specifically:

remove resolved StoreLandingPage write entries from Host legacy baseline

test that those Host files/members no longer directly write the DbContext

test no new direct Host write was introduced elsewhere

Baseline must SHRINK.

Do not replace specific baseline entries with wildcards.

Evidence:
docs/evidence/TB-TMAR-HOST-W1/architecture-guards.md

12. Tests

Required focused proof:

existing Store Landing/Page Composition write tests

create/update/delete or equivalent write scenarios in scope

validation failure scenario

transaction/atomicity scenario if applicable

authorization behavior unchanged

response/error behavior unchanged

architecture freeze tests

existing ArchitectureBoundaryTests

relevant MediatR/FluentValidation foundation tests

No unrelated broad test suite unless required.

Classify any failure as:

task-caused

pre-existing

environment

Do not suppress failures.

Evidence:
docs/evidence/TB-TMAR-HOST-W1/tests.md

13. No Scope Creep

Do NOT in this task:

clean all 53 Host write locations

migrate all Host composers

create all Contracts projects

move StoreLandingPage Domain ownership

move StoreAppearance / StoreMenu / StoreCheckout types

reorganize module folders

migrate all Directories

install Redis

perform broad locale/error cleanup

rewrite Storefront UI

change database schema unless absolutely required for existing behavior (expected: NO)

If unexpected coupling blocks this narrow slice:
STOP and report BLOCKER with exact paths.

14. Capability Map + Recovery SoT

Update only relevant architecture facts in:
docs/architecture/TOOBA-CAPABILITY-MAP.md

Record:

first Host dangerous-write slice migrated behind CQRS

direct Host persistence debt reduced

no bounded-context ownership move performed

Last Verified Task = TB-TMAR-HOST-W1

Update:
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

Current state should become:

Foundation accepted

Host Wave 1 Slice 1 accepted only after PASS

next task derived from actual result

Create:
docs/evidence/TB-TMAR-HOST-W1/recovery-sot.md

15. Acceptance Criteria

PASS only if ALL are true:

exact Git baseline recorded

external bootstrap copied into repository

scope is limited to Store Landing/Page Composition direct writes

Host no longer directly writes business DbContext for this slice

Host no longer owns SaveChanges/transaction for this slice

write path uses ISender → Command → Handler

FluentValidation used for new request validation where applicable

new code uses IClock/IIdGenerator where required

no new localized Domain error strings

transaction semantics preserved

user-visible behavior preserved

architecture legacy baseline SHRINKS

no Domain ownership move

no Contracts wave

no folder move

no Redis

focused tests PASS

user work preserved

canonical Result sent through Bridge

Worker stops completely

16. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Applicable-Locks
Recovery-Start
Bootstrap
Scope
Before-Write-Path
After-Write-Path
Commands
Handlers
Validation
Transaction-Boundary
Error-Localization
Clock-Id
Host-Debt-Removed
Architecture-Guards
Tests
Capability-Map
Recovery-SoT
Changed-Files
Git
Architectural-Concerns
Blockers
Next-Recommended-Task

Do not include Worker-IDLE.

After canonical Result through Bridge:
STOP completely.

Do NOT:

poll

fetch next task

continue to another Host slice

continue to Contracts extraction

continue to Domain ownership moves

write Worker IDLE

END_TOOBA_TASK