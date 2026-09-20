PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-OFFER-REFERENCE-W1

Parent-Task:
TB-TMAR-CHECKOUT-IMPL-W5

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
TMAR Offer Reference Module — Complete Golden Module Structure and Architecture Pattern

Task Type:
IMPLEMENTATION — BACKEND-ONLY COMPLETE MODULE RECOVERY

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Target-Module:
Offer

Module-Completion-Requirement:
COMPLETE_REFERENCE_PATTERN

0. Architect Intent

TB-TMAR-CHECKOUT-IMPL-W5 is accepted as a safe checkpoint.

Checkout is intentionally PAUSED after W5.
Do NOT continue to CHECKOUT-IMPL-W6 in this task.

Current safe checkout checkpoint:

Order-owned CheckoutProcessManager active

Inventory checkout seam uses Inventory.Contracts

Cart conversion seam uses Cart.Contracts

Promotion checkout seam uses Promotion.Contracts

shared TransactionScope preserved

Order.Application foreign Application debt materially reduced

residual Order.Infrastructure foreign Application edges remain documented

no async Saga/compensation runtime

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

The next architectural strategy is now:

choose ONE small backend module

complete its physical + logical architecture fully

make it the canonical Golden/Reference Module

only then apply that pattern to larger modules

Chosen module:
Offer

This task MUST NOT leave Offer as a half-migrated example.
The acceptance state is:

Module-Recovery-State: COMPLETE_REFERENCE_PATTERN

If Offer cannot safely reach that state because of a discovered architectural blocker, return BLOCKED with exact blocker; do not silently stop halfway and call PASS.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Expected previous accepted tip:
d73d2aae2c932aaf68c105f6ab744b44d7e39119

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/recovery-sot.md

all prior Offer/Contracts evidence from TMAR

live Offer project structure and references

Verify:

branch main

HEAD == origin/main

18ca10c9 ancestor

git status --short

git diff --name-only

git diff --cached --name-only

user work preserved

no frontend production changes

If tracked user work conflicts:
STOP with RECOVERY_CONFLICT.

Never:

git reset

git clean

destructive checkout/restore

unsafe rebase

blind stash manipulation

broad git add .

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/recovery-start.md

2. Frontend Freeze

ARCH-FE-FREEZE-001 remains ACTIVE.

No production changes under:
src/frontend/**

Required:
Frontend-Production-Changes: NONE

3. Offer Baseline Inventory

Before moving anything, inventory the live Offer module completely.

Record:

all Offer projects

all source files

LOC by file

namespaces

project references

package references

DbContext(s)

migrations

EF configurations

repositories/directories

Application handlers

commands/queries

Contracts types

endpoints currently living in Host

tests

Outbox/events

background jobs if any

cross-module dependencies inbound/outbound

Host direct references

any oversized or mixed-responsibility files

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/offer-baseline.md
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/offer-baseline.json

Also report:
Offer-Reference-Suitability: CONFIRMED
or
Offer-Reference-Suitability: BLOCKED

If BLOCKED due to fundamental ownership ambiguity, STOP and report.

4. Canonical Target Structure

Target top-level module shape:

src/backend/Modules/Offer/
Tooba.Offer.Domain/
Tooba.Offer.Application/
Tooba.Offer.Contracts/
Tooba.Offer.Infrastructure/
Tooba.Offer.Endpoints/
Tooba.Offer.Tests/

If repo currently uses a different backend root, follow actual repository root while preserving this logical shape.

Inside each project, organize by actual responsibility.

Expected pattern:

Tooba.Offer.Domain/
Aggregates/
Entities/
ValueObjects/
Policies/
Events/
Errors/

Tooba.Offer.Application/
UseCases/
<UseCaseName>/
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
OfferDbContext.cs
Configurations/
Migrations/
Repositories/
Adapters/
Outbox/
DependencyInjection/

Tooba.Offer.Endpoints/
Admin/
Storefront/
OfferEndpointModule.cs

Tooba.Offer.Tests/
Domain/
Application/
Contracts/
Infrastructure/
Endpoints/
Architecture/

Do NOT create empty ceremonial folders.
Only create folders backed by actual files/responsibility.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/target-structure.md

5. File / Type Cohesion Rule

Apply pragmatic file cohesion:

Preferred:

one aggregate root per file

one command/query + handler pair may share a feature folder, not a giant file

one EF configuration per entity

one public boundary contract per coherent responsibility

one endpoint class per coherent endpoint/use case

Do NOT enforce artificial one-record-per-file for tiny tightly-coupled records.

Durable rule to add if justified:
ARCH-MODULE-FILE-001

Meaning:
No new multi-responsibility module god-files; new production files must have one cohesive responsibility and obey source-size guards.

No file >800 LOC.
Existing oversized files shrink-only.
Watch threshold >500 LOC.
Prefer split before 800.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/file-cohesion.md

6. Domain Recovery

Normalize Offer.Domain structure.

Requirements:

aggregate roots explicit

entities/value objects separated by role

domain invariants remain in Domain

no Infrastructure dependency

no foreign module Domain dependency

no localized user-facing messages

semantic errors/events only

no DateTime.UtcNow / Guid.NewGuid in new protected code

pure domain code may receive now/IDs from caller

Do NOT rewrite working domain behavior.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/domain-recovery.md

7. Application Recovery

Normalize Offer.Application around use cases.

Requirements:

commands/queries/handlers grouped by use case

business IRequestHandler implementations live in Application

validation via FluentValidation where appropriate

no DbContext usage in Application

no foreign Application coupling where Contracts exist

outbound dependencies expressed as Ports/Contracts

no Host dependency

no giant Directory acting as all business logic if it can be safely split by use case

If existing Directory is still a valid strangler seam, preserve temporarily only where splitting would be risky; document residual.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/application-recovery.md

8. Contracts Recovery

Tooba.Offer.Contracts already exists from prior TMAR waves.

Complete it as the canonical cross-module boundary.

Requirements:

only stable cross-module contracts

no Domain entity leakage

no Application implementation leakage

no EF types

ports and DTOs organized by responsibility

SalesChannel and Offer lookup/return-policy boundary types placed coherently

version-friendly naming

future remote transport adapter friendly

Inbound consumers must depend on Contracts, not Offer.Application/Domain.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/contracts-recovery.md

9. Infrastructure Recovery

Normalize Offer.Infrastructure.

Requirements:

OfferDbContext under Persistence/

IEntityTypeConfiguration types under Persistence/Configurations/

migrations under Persistence/Migrations/

repositories/adapters separated by responsibility

DependencyInjection/OfferModule registration isolated

no business handlers in Infrastructure

no foreign Domain dependencies

no foreign Application dependency unless explicitly grandfathered and documented

Outbox integration located coherently if Offer emits integration events

Do NOT create shared mega-DbContext.
No cross-module FK.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/infrastructure-recovery.md

10. DbContext / Migration Ownership

Offer must own its persistence boundary.

Verify:

Offer schema/table ownership

OfferDbContext only owns Offer data

migrations are Offer-owned

no foreign module entity mapping

no cross-module DB foreign key

no shared migration ownership

If current physical DB is shared, logical schema ownership must still be explicit.

ARCH-DATA-001 remains active.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/persistence-ownership.md

11. Endpoints Project Extraction

Create/complete:
Tooba.Offer.Endpoints

Move Offer-owned HTTP endpoint/composer code OUT of Tooba.Host.

Host should only compose/map the module.

Target:
Host Program/Composition
→ MapOfferModule() / equivalent
→ Tooba.Offer.Endpoints
→ ISender / Application

Requirements:

endpoint project contains transport mapping only

no business persistence

no business decisions

no DbContext writes

no SaveChanges

no foreign module business logic

auth/policy metadata may remain at endpoint edge

ProblemDetails/localization stays boundary-oriented

Do NOT leave duplicate Offer endpoints in Host.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/endpoints-extraction.md

12. Thin Host Integration

After endpoint extraction:

Host must only:

register Offer module

map Offer endpoints

provide cross-cutting middleware/platform services

No Offer business implementation in Host.

Add guard:
HOST-MODULE-ENDPOINT-001

Meaning:
New module-owned endpoints must live in the module Endpoints project, not Tooba.Host, except truly cross-cutting Host endpoints (health/readiness/platform).

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/host-integration.md

13. Tests Project

Create/complete:
Tooba.Offer.Tests

Tests must be module-owned.

Cover:

Domain invariants

Application use cases

Contracts shape/serialization where relevant

Infrastructure persistence behavior

endpoint characterization

architecture rules

Do not create one giant test file.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/tests-structure.md

14. Offer Architecture Guards

Add module-level architecture guards that become the reference pattern.

At minimum enforce:

Domain depends on no Application/Infrastructure/Endpoints

Application depends on Domain + Contracts/BuildingBlocks as justified

Infrastructure implements Application/Contracts ports

Endpoints depends on Application/Contracts, not Infrastructure internals

Host cannot contain Offer business endpoints

no foreign Domain dependency

no foreign Application dependency when Contracts boundary exists

no new root-dumped production source

source-size thresholds

module tests remain module-local

If useful, create reusable guard helper for future modules, but do not over-generalize prematurely.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/architecture-guards.md

15. Dependency Graph

Produce final dependency graph.

Expected conceptual direction:

Tooba.Offer.Domain
↑
Tooba.Offer.Application
↑
Tooba.Offer.Endpoints

Tooba.Offer.Contracts
← external module consumers

Tooba.Offer.Infrastructure
→ implements Offer-owned ports/contracts

Host
→ composition only

No cycles.

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/dependency-graph.md

16. Source-Size / God-File Cleanup

Inspect all Offer source files.

For any >500 LOC:

classify reason

split if safe and responsibility boundaries are clear

No Offer production file may remain >800 LOC unless there is a documented blocker and Architect approval.
For this Reference Module task, expectation is ZERO >800 LOC files.

Return:
Offer-Oversized-Files: 0
or BLOCKED.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/source-size.md

17. Namespace / Folder Alignment

Because this becomes the Golden Module, namespaces should align cleanly with the final module structure where mechanically safe.

Prefer:
Tooba.Offer.Domain.*
Tooba.Offer.Application.*
Tooba.Offer.Contracts.*
Tooba.Offer.Infrastructure.*
Tooba.Offer.Endpoints.*

Do not perform gratuitous namespace churn outside Offer.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/namespace-alignment.md

18. Build / Test / Behavior Preservation

Required:

full Offer projects build

Host build

Offer tests

architecture tests

TMAR foundation tests

contracts guards

source-size guards

endpoint characterization

persistence tests

no frontend production changes

Required:
NEW_FAILURES=0

No product behavior change.
No public route break unless route is preserved exactly.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/tests.md

19. Golden Pattern Documentation

Create:
docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md

This document must describe the proven Offer pattern:

module top-level projects

purpose of each layer/project

folder conventions

endpoint ownership

test ownership

dependency rules

persistence ownership

contracts boundary

source-size/file-cohesion rules

Host composition rule

migration checklist for next module

anti-patterns to avoid

This becomes the canonical template for subsequent modules.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/reference-pattern.md

20. Module Completion Gate

PASS only if Offer is genuinely complete as the reference module.

Return exactly:

Module-Recovery-State: COMPLETE_REFERENCE_PATTERN

Anything else means task is NOT PASS.

Completion requires:

final physical folder/project structure complete

Domain ownership clean

Application use cases clean

Contracts canonical

Infrastructure ownership clean

DbContext/migrations owned

Endpoints extracted from Host

Host integration thin

tests module-owned

architecture guards active

no Offer >800 LOC production files

dependency graph clean

no frontend changes

no behavior change

no workaround

user work preserved

21. Checkout Pause State

Do not resume Checkout W6.

Record:
Checkout-Recovery-State: PAUSED_AT_SAFE_W5_CHECKPOINT

Also preserve W6 candidate:
Order.Infrastructure → Cart.Contracts cleanup

This is intentionally deferred until the Offer reference module is complete and accepted.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/checkout-pause.md

22. Next Module Decision

Do NOT start another module in this task.

At completion, identify 2–3 candidate modules for the next migration using:

size

coupling

Host endpoint debt

source-size debt

persistence ownership clarity

But only recommend; do not execute.

Return:
Next-Module-Candidates: ...

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/next-module-candidates.md

23. Recovery Updates

Update:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md

docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/recovery-sot.md

Must preserve:
TMAR-Execution-Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

24. Acceptance Criteria

PASS only if:

Offer suitability confirmed

Offer final project/folder structure complete

endpoint project exists and Offer endpoints removed from Host

Host is thin for Offer

Domain/Application/Contracts/Infrastructure boundaries clean

DbContext/migrations Offer-owned

Contracts are canonical

tests module-owned

architecture guards active

no Offer production file >800 LOC

NEW_FAILURES=0

frontend production untouched

no behavior/API route regression

Checkout remains paused at W5

reference pattern document created

Module-Recovery-State = COMPLETE_REFERENCE_PATTERN

user work preserved

canonical Result delivered

Worker stops completely

25. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
TMAR-Execution-Mode
Frontend-Production-Changes
Offer-Reference-Suitability
Offer-Baseline
Target-Structure
Domain-Recovery
Application-Recovery
Contracts-Recovery
Infrastructure-Recovery
Persistence-Ownership
Endpoints-Extraction
Host-Integration
Tests-Structure
Architecture-Guards
Dependency-Graph
Source-Size
Namespace-Alignment
Tests
Reference-Module-Pattern
Module-Recovery-State
Checkout-Recovery-State
Next-Module-Candidates
Capability-Map
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
Offer-Reference-Suitability: CONFIRMED
Offer-Oversized-Files: 0
Module-Recovery-State: COMPLETE_REFERENCE_PATTERN
Checkout-Recovery-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

After canonical Result:
STOP completely.

Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK
