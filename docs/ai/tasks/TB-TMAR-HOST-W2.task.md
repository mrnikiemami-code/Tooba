PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-HOST-W2

Parent-Task:
TB-TMAR-HOST-W1-R1

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
TMAR Host Wave 2 — Remove Next Highest-Risk Direct Host Write Slice

Task Type:
IMPLEMENTATION — NARROW REFACTOR

0. Architect Intent

HOST-W1 is accepted.

Continue TMAR automatically without asking the user for target selection.

This task must select and migrate exactly ONE next Host dangerous-write slice from the approved baseline evidence.

The Worker must choose the next slice by evidence, using this priority order:

direct business DbContext write in Host

explicit SaveChanges / transaction ownership in Host

isolated bounded scope with clear current owning module

high extraction risk

low-to-moderate regression radius

Do NOT choose based on filename aesthetics.
Do NOT choose a broad multi-capability cleanup.

Goal:
Shrink Host write debt again while preserving behavior and keeping the migration incremental.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Read durable recovery source:
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

Before modifying anything record:

branch

exact HEAD SHA

exact origin/main SHA

HEAD==origin/main

git status --short

git diff --name-only

git diff --cached --name-only

confirm 18ca10c9 ancestor

Expected starting point:
TB-TMAR-HOST-W1-R1 accepted
Git tip from previous Result:
85cc584efc48acd19c7247caca2bea0b82c5e880

If repository state conflicts:
STOP with RECOVERY_CONFLICT.

Never:

git reset

git clean

destructive checkout/restore

unsafe rebase

blind stash manipulation

broad git add .

Evidence:
docs/evidence/TB-TMAR-HOST-W2/recovery-start.md

2. Read Only Required TMAR Evidence

Read:

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/evidence/TB-TMAR-ARCH-BASELINE/host-audit.md

docs/evidence/TB-TMAR-ARCH-BASELINE/host-cleanup-waves.md

docs/evidence/TB-TMAR-ARCH-BASELINE/domain-ownership-map.md

docs/evidence/TB-TMAR-FND-001/architecture-freeze-guards.md

docs/evidence/TB-TMAR-HOST-W1-R1/recovery-sot.md

Do NOT repeat the repository-wide audit.

3. Select Exactly One Next Write Slice

From the remaining Host write baseline, select the single highest-priority coherent slice.

Before implementation create:

docs/evidence/TB-TMAR-HOST-W2/slice-selection.md

It must contain:

exact Host file(s)

exact write methods/members

current owning DbContext

current owning module

write operations performed

transaction behavior

why this slice outranks the other remaining candidates

why it is safe enough to migrate now

bounded-context ownership caveat, if any

If the apparent owner is semantically questionable:

do NOT move Domain ownership here

use the current owner as a transitional persistence owner

document the debt for the Domain Ownership wave

The selected slice MUST be one coherent business capability.

4. Required Target Flow

For every write use-case in the selected slice:

HTTP Endpoint / thin Host adapter
→ ISender
→ Command
→ Handler in *.Application
→ Application abstraction / Domain
→ Infrastructure implementation
→ owning DbContext

Mandatory layering:

Handler lives in Application

Application does NOT reference Infrastructure

EF/DbContext/transaction implementation stays in Infrastructure

Host does not own business write persistence

Do NOT repeat the HOST-W1 mistake of placing business MediatR handlers in Infrastructure.

5. CQRS / Validation

Use existing:

MediatR 12.5.0

FluentValidation pipeline

IClock

IIdGenerator

SemanticError foundation

For NEW code:

command/query names must describe business intent

validators live in Application

business handlers live in Application

no endpoint-only duplicated validation when pipeline validation is appropriate

Do not mass-convert unrelated Directory/Application Services.

6. Transaction Semantics

If current Host code owns a transaction:

preserve atomicity

preserve relational/non-relational behavior

move EF-specific transaction mechanics into Infrastructure

Application orchestrates through abstraction if required

Do NOT:

introduce a global TransactionBehavior

change isolation level

widen transaction scope

add distributed transactions

Document before/after ownership.

Evidence:
docs/evidence/TB-TMAR-HOST-W2/transaction-boundary.md

7. Error / Localization Rule

All NEW code must be locale-agnostic.

Forbidden:
hardcoded user-facing localized Domain messages in ANY language.

Not only Persian/English.

Use:

semantic error code

structured metadata where needed

existing boundary mapping to HTTP/ProblemDetails

The architecture must support unlimited locales.

Do not mass-migrate unrelated old errors.

Evidence:
docs/evidence/TB-TMAR-HOST-W2/error-localization.md

8. Time / ID Rule

All NEW orchestration code:

uses IClock

uses IIdGenerator where new IDs are created

No new protected-layer:

DateTime.UtcNow

DateTimeOffset.UtcNow

DateTime.Now

Guid.NewGuid()

UuidV7.New()

Pure Domain methods may receive explicit now/ID values.

9. Host Result

After this task, for the selected slice:

Host may:

bind HTTP

authenticate/authorize

dispatch via ISender

map result to HTTP

perform presentation-only/read composition where currently permitted

Host must NOT:

Add/Update/Remove business entities

SaveChanges

BeginTransaction

own business decisions for the migrated write path

No UI redesign.
No API contract break unless unavoidable and explicitly evidenced.

10. Architecture Debt Baseline Must Shrink

Update the existing TMAR Host write baseline.

Requirements:

remove the resolved file/member entries

do not add wildcard allowances

no replacement debt

no new Host write elsewhere

Architecture tests must prove:

migrated slice cannot regress to direct Host writes

business MediatR handlers remain outside Infrastructure

no new App→App edge is introduced unnecessarily

existing core boundary tests still pass

Evidence:
docs/evidence/TB-TMAR-HOST-W2/architecture-guards.md

11. Cross-Module Dependency Rule

Do not create a NEW cross-module Application→Application project-reference edge.

If the selected write path requires another module:

use an already-existing boundary if available

otherwise STOP and report that the slice is blocked pending Contracts extraction

Do NOT solve a write cleanup by adding new architectural debt.

12. Domain Ownership Rule

Do not move bounded-context ownership in this task.

If the selected entity currently appears to live in the wrong Domain:

document it

keep transitional ownership

leave physical move for the Domain Ownership wave

No namespace/project moves.

13. Tests

Required:

focused happy-path write tests

validation failure

update/delete/status behavior as applicable

transaction/atomicity proof where applicable

authorization behavior unchanged

error/HTTP behavior unchanged

architecture freeze tests

ArchitectureBoundaryTests

relevant TMAR foundation tests

No unrelated broad suite unless required.

Evidence:
docs/evidence/TB-TMAR-HOST-W2/tests.md

14. Capability Map / Bootstrap / Recovery SoT

Update narrowly:

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/evidence/TB-TMAR-HOST-W2/recovery-sot.md

Bootstrap must record:

TB-TMAR-FND-001 accepted

TB-TMAR-HOST-W1-R1 accepted

current/last accepted task after PASS = TB-TMAR-HOST-W2

primary objective remains painless Microservice migration

Do not rewrite unrelated content.

15. Stop Condition for Product Development

At the end of this task, assess one additional field:

Product-Resume-Safety

Return exactly one of:

NOT_YET

SAFE_WITH_TMAR_PARALLEL

Criteria for SAFE_WITH_TMAR_PARALLEL:

architecture freeze guards are active

no new debt can expand via the known critical patterns

at least the highest-risk Host write slices are under control

new product features can be required to use CQRS/contracts/guards without depending on still-dangerous legacy Host write paths

If uncertain, return NOT_YET.

Do NOT ask the user.

16. No Scope Creep

Do NOT:

clean all remaining Host writes

migrate unrelated composers

start Contracts extraction

move Domain ownership

reorganize folders

install Redis

rewrite localization

mass-replace clocks/IDs

alter Storefront visual behavior

change product feature behavior

perform cosmetic cleanup

17. Acceptance Criteria

PASS only if:

one evidence-selected Host write slice migrated

handler(s) live in Application

Infrastructure retains EF/persistence

Host direct writes removed for that slice

transaction behavior preserved

no new App→App edge introduced

new code follows error/locale/clock/id locks

Host debt baseline shrinks

focused tests pass

no ownership/folder/Contracts/Redis scope creep

user work preserved

Product-Resume-Safety assessed

canonical Result sent through Bridge

Worker stops completely

18. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Applicable-Locks
Recovery-Start
Slice-Selection
Before-Write-Path
After-Write-Path
Commands
Handlers
Validation
Transaction-Boundary
Error-Localization
Clock-Id
Cross-Module-Dependencies
Host-Debt-Removed
Architecture-Guards
Tests
Capability-Map
Bootstrap
Recovery-SoT
Changed-Files
Git
Architectural-Concerns
Blockers
Product-Resume-Safety
Next-Recommended-Task

Do not include Worker-IDLE.

After canonical Result through Bridge:
STOP completely.

Do NOT:

poll

fetch next task

continue to another Host slice

write Worker IDLE

END_TOOBA_TASK