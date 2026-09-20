PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-BOUNDARY-V1-R1

Parent-Task:
TB-TMAR-BOUNDARY-V1

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Status:
REPAIR

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Title:
Boundary Verification Repair — Complete Source-Size/God-File Freeze and Missing Structural Guard Coverage

Task Type:
NARROW ARCHITECTURE REPAIR

Reason

TB-TMAR-BOUNDARY-V1 verified the independent repository review successfully:

Cart.Domain → Offer.Domain CONFIRMED

Order.Domain → Offer.Domain CONFIRMED

Pricing.Domain → Offer.Domain CONFIRMED

Payment.Infrastructure → Wallet.Domain CONFIRMED

Order.Application confirmed as a synchronous hub

However the latest Architect version of TB-TMAR-BOUNDARY-V1 also required:

repository-wide hand-written source-size inventory

God-file/oversized-file baseline

guard against NEW >800 LOC hand-written source files

guard against growth of existing oversized legacy files

prioritized decomposition plan

characterization-test-first rule

Those outputs are missing from the Worker Result and therefore must be completed before the boundary task is fully accepted.

Additionally, the Result states:
Infra→foreign Application still unfrozen

This repair must freeze that structural category from NEW expansion as well.

NO broad refactor.
NO giant-file splitting yet.
NO Contracts extraction yet.

0. Recovery Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Read:

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/evidence/TB-TMAR-BOUNDARY-V1/dependency-verification.md

docs/evidence/TB-TMAR-BOUNDARY-V1/architecture-test-gap.md

docs/evidence/TB-TMAR-BOUNDARY-V1/contracts-sequence.md

docs/evidence/TB-TMAR-BOUNDARY-V1/recovery-sot.md

Verify:

branch main

exact HEAD SHA

exact origin/main SHA

HEAD==origin/main

18ca10c9 ancestor

user work preserved

Expected previous accepted tip:
199e9fd69bfc3d2bd3900d6049123f4d8d8e9a31

No destructive Git operations.

Evidence:
docs/evidence/TB-TMAR-BOUNDARY-V1-R1/recovery-start.md

1. Repository-Wide Hand-Written Source Size Inventory

Scan the entire repository for hand-written source files.

Include at minimum:

.cs

.ts

.tsx

other hand-written source languages if materially present

Exclude:

generated files

EF migrations/snapshots

lockfiles

vendored code

build output

minified files

machine-generated code with clear marker

artifacts

For each included file record:

path

language

module/layer or frontend area

physical LOC

non-blank/non-comment LOC if practical

classification

Default classifications:

Backend C#:

NORMAL <= 500

WATCH > 500

OVERSIZED_LEGACY > 800

CRITICAL_GOD_FILE > 1500

Frontend TS/TSX:

NORMAL <= 500

WATCH > 500

OVERSIZED_LEGACY > 800

CRITICAL_GOD_FILE > 1200

Evidence:
docs/evidence/TB-TMAR-BOUNDARY-V1-R1/source-size-inventory.md

Machine-readable:
docs/evidence/TB-TMAR-BOUNDARY-V1-R1/source-size-inventory.json

2. God-File Freeze Baseline

Create an explicit, shrink-only baseline for existing oversized files using the repository's current architecture-test baseline conventions.

Preferred location:
src/backend/Host/Tooba.Host.Tests/Baselines/tmar-source-size-baseline.json

or an equivalent existing architecture-baseline location if more appropriate.

Each entry must contain at least:

normalized path

language

current LOC

classification

No wildcards.

Generated/excluded files must not appear.

3. Source-Size Architecture Guard

Implement an automated guard that:

FAILS if a NEW hand-written source file exceeds 800 physical LOC.

FAILS if an existing OVERSIZED_LEGACY or CRITICAL_GOD_FILE grows above its baseline LOC.

Allows an existing oversized file to SHRINK.

Requires baseline entries to disappear/reduce when files are split or deleted.

Never automatically raises a baseline.

Reports:

exact file

current LOC

baseline LOC

threshold

violation type

Important:
Do not treat line count alone as a reason to split cohesive generated/technical files.
This guard applies only to hand-written source.

Evidence:
docs/evidence/TB-TMAR-BOUNDARY-V1-R1/god-file-guard.md

4. Characterization-Test-First Decomposition Rule

Canonicalize this engineering rule in TMAR architecture docs:

Before decomposing any CRITICAL_GOD_FILE:

identify its behaviors/use-cases

add focused characterization tests around the slice being changed

preserve public behavior

split incrementally by capability/use-case

keep architecture guards green

do not rely only on AI-generated diff inspection

Do NOT create unit-test projects for all modules now.

Testing follows touched/refactored slices.

Update:
docs/architecture/TMAR-architecture-locks.md

Add a lock equivalent to:

ARCH-SIZE-001:
No new hand-written source file may exceed the approved oversized-file threshold without explicit architecture approval.

ARCH-SIZE-002:
Existing oversized legacy files may only stay equal or shrink; growth above their recorded baseline is forbidden.

ARCH-REFACTOR-001:
Critical giant-file decomposition requires characterization tests before structural splitting.

Evidence:
docs/evidence/TB-TMAR-BOUNDARY-V1-R1/refactor-locks.md

5. Top Oversized File Queue

Produce a prioritized top-10 queue for later decomposition.

For each:

exact path

LOC

layer/module

risk

why it is hard to change

current automated coverage

missing characterization coverage

likely decomposition boundary

recommended order

Known examples from independent review must be verified, not blindly trusted:

CatalogDirectory.cs ~5k+

CatalogDomain.cs ~2k+

AdminOrderOperationsComposer.cs ~2k+

StorefrontEndpoints.cs ~1k+

large frontend admin screens/components

Evidence:
docs/evidence/TB-TMAR-BOUNDARY-V1-R1/god-file-plan.md

6. Freeze Infra → Foreign Application Expansion

The previous Result says:
Infra→foreign Application still unfrozen

Map current Infrastructure → foreign Application project-reference edges.

Create an exact legacy baseline.

Add architecture guard:

current exact legacy edges may remain temporarily

NO NEW Infrastructure → foreign Application edge may be added

baseline must shrink during Contracts migration

no wildcard suppression

Do NOT refactor these edges in this repair.

Evidence:
docs/evidence/TB-TMAR-BOUNDARY-V1-R1/infra-foreign-application-guard.md

7. Confirm Boundary V1 Acceptance State

After this repair, verify that TB-TMAR-BOUNDARY-V1 now fully satisfies:

claim verification

source coupling

architecture test gaps

structural guards

Contracts sequence

source-size inventory

God-file freeze

characterization-test-first plan

Product-Resume-Safety remains:
NOT_YET

unless this repair reveals that all critical structural expansion is now frozen AND the next Contracts wave is not required for safe feature continuation.

If uncertain:
NOT_YET

8. Next Task

Expected next task if repair passes:
TB-TMAR-CONTRACTS-W1

Do NOT execute it here.

9. No Scope Creep

Do NOT:

split CatalogDirectory.cs

split CatalogDomain.cs

split Host composers

refactor frontend giant files

create module test projects broadly

create Contracts projects

remove confirmed boundary leaks yet

move Domain ownership

start HOST-W3

install Redis

rewrite Git history

run git filter-repo

10. Tests

Required:

source-size inventory generation proof

source-size architecture guard test

synthetic/new >800 LOC hand-written source rejection proof

oversized legacy growth rejection proof

shrink behavior proof

Infrastructure→foreign Application guard test

ArchitectureBoundaryTests

TMAR foundation tests

Evidence:
docs/evidence/TB-TMAR-BOUNDARY-V1-R1/tests.md

11. Capability Map / Bootstrap / Recovery SoT

Update narrowly:

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/evidence/TB-TMAR-BOUNDARY-V1-R1/recovery-sot.md

Record:

Boundary V1 repaired/completed

God-file growth frozen

Infra→foreign Application growth frozen

next expected task = TB-TMAR-CONTRACTS-W1

Last Verified Task = TB-TMAR-BOUNDARY-V1-R1

12. Acceptance Criteria

PASS only if:

source-size inventory exists repository-wide

generated/build files excluded correctly

existing oversized files explicitly baselined

new >800 LOC hand-written files fail architecture guard

existing oversized files cannot grow above baseline

baseline can only shrink

top-10 giant-file queue exists

characterization-test-first rule canonicalized

current Infra→foreign Application edges baselined exactly

new Infra→foreign Application edges fail

no broad refactor performed

Product-Resume-Safety assessed

user work preserved

canonical Result delivered

Worker stops completely

13. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Source-Size-Inventory
God-File-Baseline
God-File-Guard
Refactor-Locks
God-File-Plan
Infra-Foreign-Application-Guard
Tests
Capability-Map
Bootstrap
Recovery-SoT
Git
Architectural-Concerns
Blockers
Product-Resume-Safety
Next-Recommended-Task

After canonical Result through Bridge:
STOP completely.

Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK