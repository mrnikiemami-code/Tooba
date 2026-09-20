PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-OFFER-REFERENCE-W1-R1

Parent-Task:
TB-TMAR-OFFER-REFERENCE-W1

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
Offer Reference Module Repair — Enforce Real Physical Folder/Project Structure and Revalidate Golden Pattern

Task Type:
REPAIR — PHYSICAL STRUCTURE / NAMESPACE / PROJECT LAYOUT VERIFICATION

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

0. Architect Correction

The previous TB-TMAR-OFFER-REFERENCE-W1 result is REOPENED.

User visual inspection indicates the expected physical folder structure was NOT actually produced in the repository.

Therefore:

previous Module-Recovery-State: COMPLETE_REFERENCE_PATTERN is NOT trusted

Reference-Pattern-Reuse-State is suspended

TB-TMAR-PRICING-REFERENCE-W1 MUST NOT proceed until this repair is accepted

visual filesystem evidence outranks previous Worker PASS

This repair is specifically about REAL on-disk project/folder structure, not documentation-only or namespace-only organization.

PASS is forbidden unless the final physical tree on disk matches the approved module architecture.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Expected baseline to inspect:
current main after prior Offer/Tax work.

Verify:

branch main

HEAD == origin/main

18ca10c9 ancestor

user work preserved

no frontend production changes

If another task has already modified Pricing after TB-TMAR-PRICING-REFERENCE-W1 was issued:
STOP and return RECOVERY_CONFLICT_ACTIVE_TASK.
Do not combine Pricing work into this repair.

Never use destructive Git operations.
Never use broad git add ..

2. Establish REAL Current Offer Paths

Do not rely on docs.

Print and persist the ACTUAL filesystem tree for all Offer projects/files.

Required evidence:

exact absolute/relative path of every Offer project .csproj

exact physical directory of every Offer source file

exact Host Offer endpoint files

exact test project paths

exact migration paths

Use a deterministic tree/listing command and save output.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R1/physical-tree-before.txt
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R1/physical-tree-before.md

3. Canonical Physical Target

The REAL filesystem must converge to the repository's backend root equivalent of:

Modules/
Offer/
Tooba.Offer.Domain/
Aggregates/
Entities/
ValueObjects/
Policies/
Events/
Errors/

Tooba.Offer.Application/
  UseCases/
  Ports/
  Validators/
  Dtos/

Tooba.Offer.Contracts/
  Ports/
  Commands/
  Events/
  Dtos/

Tooba.Offer.Infrastructure/
  Persistence/
    Configurations/
    Migrations/
  Repositories/
  Adapters/
  Outbox/
  DependencyInjection/

Tooba.Offer.Endpoints/
  Admin/
  Storefront/

Tooba.Offer.Tests/
  Domain/
  Application/
  Contracts/
  Infrastructure/
  Endpoints/
  Architecture/

Do not create empty folders.
Only create folders that have real responsibilities/files.

The actual repository backend root may differ from src/backend; use the real repo root while preserving this module shape.

4. Physical Moves Must Be REAL

For every safe Offer file:

physically move it on disk into the correct project/folder

use safe git-aware moves where appropriate

update .csproj references only when required

do not merely change namespace

do not merely document the intended path

do not leave duplicate old files behind

Required evidence for every moved file:
OLD_PATH → NEW_PATH

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R1/move-manifest.md

5. Project Boundaries Must Be REAL

Verify the following projects actually exist as physical .csproj projects where the reference pattern requires them:

Tooba.Offer.Domain

Tooba.Offer.Application

Tooba.Offer.Contracts

Tooba.Offer.Infrastructure

Tooba.Offer.Endpoints

Tooba.Offer.Tests

If any are logical/documented only:
create/fix the actual project structure.

Verify solution/project inclusion.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R1/project-layout.md

6. Namespace Alignment

After physical moves, namespaces must align with final paths where semantically appropriate.

Examples:

Tooba.Offer.Domain.Aggregates

Tooba.Offer.Domain.ValueObjects

Tooba.Offer.Application.UseCases.<UseCase>

Tooba.Offer.Contracts.Ports

Tooba.Offer.Infrastructure.Persistence.Configurations

Tooba.Offer.Endpoints.Admin

Update all using references.

Verify:

DI scanning

EF configuration discovery

migrations assembly

endpoint mapping

reflection/assembly scanning

tests

Do not preserve stale namespaces merely to make compilation easy if they contradict the approved Golden Module layout.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R1/namespace-alignment.md

7. Host Endpoint Extraction Must Be Physical

Verify Offer-owned HTTP endpoint/composer source is physically absent from Tooba.Host.

Host may contain only thin Offer registration/mapping.

If Offer endpoint implementation still physically lives in Host:
move it to Tooba.Offer.Endpoints.

Do not leave compatibility duplicates.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R1/host-offer-files.md

8. Physical Tree After

After all changes, generate the ACTUAL filesystem tree again.

Required:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R1/physical-tree-after.txt
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R1/physical-tree-after.md

The Result must summarize the tree, not merely claim it exists.

9. Empty / Flat / Dumping-Ground Check

Verify no Offer project root contains a flat pile of responsibility-owned .cs files.

Allowed root files should be minimal, e.g. project marker/DI entrypoint where justified.

Fail if:

many Domain types remain flat at project root

Application handlers/commands remain flat

Infrastructure persistence/configuration/adapters remain flat

Endpoints remain flat without responsibility grouping where multiple endpoint families exist

Add/refine guard if existing ARCH-FOLDER-OWNERSHIP-001 did not catch this.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R1/folder-compliance.md

10. Reference Pattern Guard Repair

Investigate why previous PASS was possible despite physical structure mismatch.

Identify one:

guard checked logical classification only

guard checked namespace only

guard ignored existing files

guard used wrong root

evidence was incomplete

another concrete cause

Repair the guard so a future module cannot report COMPLETE_REFERENCE_PATTERN without physical tree compliance.

Add durable lock/test:
ARCH-MODULE-PHYSICAL-001

Meaning:
A reference-complete module must have its production files physically located under the approved module/project responsibility structure; namespace/docs alone are insufficient.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R1/guard-root-cause.md

11. No Rewrite

This repair is structural.

Do NOT:

rewrite Offer business logic

redesign behavior

change public routes

change database semantics

change frontend

resume Checkout

touch Pricing except to detect active-task conflict

Use:
move → namespace/reference fix → build/test.

12. Source Size

Reverify:
Offer-Oversized-Files: 0

No new >800 LOC files.
No god-file growth.

13. Tests

Required:

full Offer build

Host build

Offer.Tests

endpoint characterization

architecture tests

physical-folder guard tests

TMAR durable guards

source-size guards

Required:
NEW_FAILURES=0

14. Golden Pattern Status

Only after ACTUAL physical tree validation may Worker return:

Module-Recovery-State: COMPLETE_REFERENCE_PATTERN

Also return:
Physical-Structure-State: VERIFIED_ON_DISK

Anything else:
Status must be BLOCKED / FAIL, not PASS.

15. Reference Pattern Revalidation

Revalidate:
docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md

Correct any wording that allowed logical-only completion.

Set:
Reference-Pattern-State: REVALIDATED_WITH_PHYSICAL_STRUCTURE

Do NOT claim PROVEN_ON_2/3 modules from this repair; Tax must be independently rechecked later against the repaired physical rule.

16. Checkout / Pricing State

Return:
Checkout-Recovery-State: PAUSED_AT_SAFE_W5_CHECKPOINT

Return:
Pricing-Reference-State: NOT_STARTED_UNTIL_OFFER_REPAIR_ACCEPTED

Do not execute Pricing work.

17. Recovery Updates

Update:

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R1/recovery-sot.md

Explicitly record that previous Offer COMPLETE status was reopened due user visual evidence and revalidated physically.

18. Acceptance Criteria

PASS only if:

actual Offer filesystem tree captured before/after

actual project directories are correct

actual files physically moved

actual namespaces align

Offer endpoints physically out of Host

project references/solution valid

no flat dumping-ground structure remains

physical-folder architecture guard added/repaired

no frontend changes

no rewrite

tests/build pass

NEW_FAILURES=0

user work preserved

Physical-Structure-State: VERIFIED_ON_DISK

Module-Recovery-State: COMPLETE_REFERENCE_PATTERN

19. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
TMAR-Execution-Mode
Frontend-Production-Changes
Physical-Tree-Before
Move-Manifest
Project-Layout
Namespace-Alignment
Host-Offer-Files
Physical-Tree-After
Folder-Compliance
Guard-Root-Cause
Architecture-Guards
Source-Size
Tests
Physical-Structure-State
Module-Recovery-State
Reference-Pattern-State
Checkout-Recovery-State
Pricing-Reference-State
Bootstrap
Master-Recovery-State
Recovery-SoT
Git
Architectural-Concerns
Blockers
Product-Resume-Safety
Next-Recommended-Task

Expected:
TMAR-Execution-Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Frontend-Production-Changes: NONE
Physical-Structure-State: VERIFIED_ON_DISK
Module-Recovery-State: COMPLETE_REFERENCE_PATTERN
Reference-Pattern-State: REVALIDATED_WITH_PHYSICAL_STRUCTURE
Checkout-Recovery-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Pricing-Reference-State: NOT_STARTED_UNTIL_OFFER_REPAIR_ACCEPTED
Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

After canonical Result:
STOP completely.

Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK
