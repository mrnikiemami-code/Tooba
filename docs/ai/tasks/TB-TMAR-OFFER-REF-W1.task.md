PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-OFFER-REF-W1

Parent-Task:
TB-TMAR-CHECKOUT-IMPL-W4

Supersedes-Unexecuted-Task:
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

Track:
OFFER_REFERENCE_MODULE

Title:
Offer Reference Module Wave 1 — Baseline, Ownership Map, Target Structure, and Safe Migration Plan

Task Type:
DISCOVERY + CHARACTERIZATION + MIGRATION PLAN

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

0. Architect Intent

We are intentionally pausing Checkout at the accepted TB-TMAR-CHECKOUT-IMPL-W4 checkpoint.

TB-TMAR-CHECKOUT-IMPL-W5 was issued earlier but no canonical Worker Result was accepted. It is superseded by this task and MUST NOT be executed unless the Architect explicitly reactivates it later.

From this point, TMAR focuses on ONE backend module only:

Offer

The Offer track must continue task-by-task until:

Module-Recovery-State: COMPLETE_REFERENCE_PATTERN

Only after that state is reached will the Architect stop and wait for user inspection.

Do NOT switch to another module.
Do NOT resume Checkout.
Do NOT touch frontend.

This first wave is evidence/discovery only. It must not perform broad physical moves yet.

Primary goals:

reconstruct the complete current Offer module shape

identify all Offer-owned code currently living outside Offer

identify all code inside Offer that is not Offer-owned

inventory Domain/Application/Contracts/Infrastructure/Host endpoint/test/persistence structure

define the exact target physical structure

identify migrations, DbContext ownership, endpoints, tests, CQRS seams, Contracts, source-size debt

produce a staged migration plan to make Offer the reference/golden module

define objective completion criteria for COMPLETE_REFERENCE_PATTERN

1. Git / Recovery Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Expected current clean HEAD:
9c2251a0cc7daf60102299d2940c6911f0d562bd

Before work verify:

branch main

HEAD == origin/main

git status --short is clean

staged = 0

untracked = 0

18ca10c9 remains ancestor

stashes untouched

user work preserved

If current HEAD differs because of a legitimate user commit after the known clean checkpoint:

record exact SHA

continue only if worktree is clean and no conflicting tracked changes exist

If TB-TMAR-CHECKOUT-IMPL-W5 has already been claimed/executed or its changes exist:
STOP with RECOVERY_CONFLICT and report exact state.

Never:

git reset

git clean

destructive checkout/restore

unsafe rebase

blind stash manipulation

broad git add .

Evidence:
docs/evidence/TB-TMAR-OFFER-REF-W1/recovery-start.md

2. Durable Locks

All existing locks remain active, especially:

ARCH-FE-FREEZE-001

ARCH-FOLDER-OWNERSHIP-001

ARCH-RECOVERY-001

ARCH-USERWORK-001

ARCH-BASELINE-001

ARCH-NOWORKAROUND-001

ARCH-DATA-001

ARCH-SIZE-001

ARCH-SIZE-002

ARCH-TX-001

ARCH-CHECKOUT-001..005 (unchanged, checkout paused)

HOST-FOLDER-001

HOST-HYGIENE-001

No frontend production changes.

Required:
Frontend-Production-Changes: NONE

3. Locate the Entire Current Offer Surface

Inventory ALL current Offer-related production code across the repository.

Search for:

Tooba.Offer.* projects

Offer domain types

Offer application services/directories/handlers

Offer.Contracts types

Offer Infrastructure

Offer DbContext / EF configurations / migrations

Host endpoints/composers for Offer

admin/storefront Offer endpoints

Offer tests

Offer background jobs

Offer integration events

external adapters that implement Offer Contracts

foreign module references into Offer

Offer references into foreign modules

Do not assume code is located only under one directory.

Produce:
docs/evidence/TB-TMAR-OFFER-REF-W1/offer-surface-inventory.md
docs/evidence/TB-TMAR-OFFER-REF-W1/offer-surface-inventory.json

4. Current Project / Folder Topology

Document exact current physical structure of:

Tooba.Offer.Domain

Tooba.Offer.Application

Tooba.Offer.Contracts

Tooba.Offer.Infrastructure

Offer-related Host files

Offer-related tests

Offer-related migrations

For every project record:

path

csproj

project references

source file count

total LOC

files >300 LOC

files >500 LOC

files >800 LOC

largest files

root-level .cs count

existing folders

suspicious dumping-ground files

Evidence:
docs/evidence/TB-TMAR-OFFER-REF-W1/current-topology.md

5. Ownership Classification

Classify every significant Offer-related type/file as:

OFFER_DOMAIN

OFFER_APPLICATION

OFFER_CONTRACT

OFFER_INFRASTRUCTURE

OFFER_ENDPOINT

OFFER_TEST

HOST_PLATFORM

FOREIGN_MODULE_OWNED

SHARED_BUILDING_BLOCK

NEEDS_OWNERSHIP_DECISION

For suspicious types, document:

current location

actual invariant/lifecycle owner

recommended destination

confidence

evidence

Do NOT move ambiguous ownership in W1.

Evidence:
docs/evidence/TB-TMAR-OFFER-REF-W1/ownership-map.md
docs/evidence/TB-TMAR-OFFER-REF-W1/ownership-map.json

6. Domain Shape Audit

Inspect Offer.Domain.

Document:

aggregate roots

entities

value objects

policies

domain events

semantic errors/exceptions

enums/status types

services

public APIs

cross-module references

Identify:

god files

multiple unrelated types per file

anemic/utility dumping

domain types that belong elsewhere

direct dependencies on foreign modules

Do NOT split yet unless a minimal characterization helper is required.

Evidence:
docs/evidence/TB-TMAR-OFFER-REF-W1/domain-audit.md

7. Application Shape Audit

Inspect Offer.Application.

Document:

commands

queries

handlers

directories/services

validators

ports/gateways

DTOs

cross-module dependencies

handlers living outside Application

direct EF/DbContext usage

business logic in Infrastructure or Host

Map each use case to its intended feature folder.

Evidence:
docs/evidence/TB-TMAR-OFFER-REF-W1/application-audit.md

8. Contracts Audit

Inspect Tooba.Offer.Contracts.

Known historical contracts include:

SalesChannel boundary

IOfferLookupGateway / OfferReference / OfferStatus

return-policy boundary

Verify current reality.

Classify each contract:

inbound contract

outbound port

shared boundary DTO

integration event

accidental internal type

Detect:

Domain leakage

Application implementation leakage

overly broad interfaces

duplicate DTOs

unstable contract naming

Evidence:
docs/evidence/TB-TMAR-OFFER-REF-W1/contracts-audit.md

9. Infrastructure / Persistence Audit

Inspect Offer.Infrastructure.

Document:

OfferDbContext

DbSets

EF configurations

migrations

repositories/directories/adapters

event/outbox usage

foreign Application/Domain references

direct SQL

transaction usage

caching

Verify:

Offer owns its own persistence schema/migrations

no cross-module DB FK

no shared mega-DbContext dependency introduced

migrations location is module-owned

Evidence:
docs/evidence/TB-TMAR-OFFER-REF-W1/infrastructure-audit.md

10. Endpoint / Host Audit

Find ALL Offer HTTP/admin/storefront endpoint code currently in Host.

For each endpoint/composer:

path

route

auth policy

request/response DTOs

direct DbContext/repository usage

ISender usage

business decisions

target ownership

safe extraction candidate?

Target future:
Tooba.Offer.Endpoints

But W1 is discovery only.

Evidence:
docs/evidence/TB-TMAR-OFFER-REF-W1/endpoints-audit.md

11. Tests Audit

Inventory Offer-related tests.

Document:

existing test projects

domain tests

application tests

infrastructure tests

endpoint/integration tests

architecture tests

characterization gaps

Determine whether a dedicated Tooba.Offer.Tests project already exists or should be created later.

Do not create a new test project in W1 unless required for discovery validation.

Evidence:
docs/evidence/TB-TMAR-OFFER-REF-W1/tests-audit.md

12. Dependency Graph

Produce exact Offer dependency graph.

At minimum capture:

Offer.Domain outgoing references

Offer.Application outgoing references

Offer.Contracts outgoing references

Offer.Infrastructure outgoing references

incoming references from Cart/Order/Pricing/Promotion/etc.

Host references

Classify violations:

MUST_REMOVE

ACCEPTED_CONTRACT_EDGE

TEMPORARY_ALLOWED

NEEDS_DESIGN

Evidence:
docs/evidence/TB-TMAR-OFFER-REF-W1/dependency-graph.md
docs/evidence/TB-TMAR-OFFER-REF-W1/dependency-graph.json

13. Target Reference Structure

Define the exact Offer target structure based on live evidence.

Target SHOULD converge toward:

src/backend/
└─ Modules/
└─ Offer/
├─ Tooba.Offer.Domain/
│ ├─ Aggregates/
│ ├─ Entities/
│ ├─ ValueObjects/
│ ├─ Policies/
│ ├─ Events/
│ └─ Errors/
│
├─ Tooba.Offer.Application/
│ ├─ UseCases/
│ ├─ Ports/
│ ├─ Validators/
│ └─ Dtos/
│
├─ Tooba.Offer.Contracts/
│ ├─ Ports/
│ ├─ Commands/
│ ├─ Events/
│ └─ Dtos/
│
├─ Tooba.Offer.Infrastructure/
│ ├─ Persistence/
│ │ ├─ Configurations/
│ │ └─ Migrations/
│ ├─ Repositories/
│ ├─ Adapters/
│ ├─ Outbox/
│ └─ DependencyInjection/
│
├─ Tooba.Offer.Endpoints/
│ ├─ Admin/
│ ├─ Storefront/
│ └─ OfferEndpointModule.cs
│
└─ Tooba.Offer.Tests/
├─ Domain/
├─ Application/
├─ Contracts/
├─ Infrastructure/
├─ Endpoints/
└─ Architecture/

Adjust based on actual code.

Important:

do NOT create empty ceremonial folders

folders must correspond to real responsibilities

no Common, Misc, Helpers

physical structure must follow ownership

Evidence:
docs/evidence/TB-TMAR-OFFER-REF-W1/target-structure.md

14. File Granularity Rule

Define Offer reference policy:

Preferred:

one major public production type per file

one handler per file

one EF configuration per file

one aggregate root per file

one endpoint type per file

Small tightly-coupled private/internal records MAY coexist when cohesion is high.

Do NOT enforce a blind one-type-per-file rule for every trivial nested/private type.

Evidence:
docs/evidence/TB-TMAR-OFFER-REF-W1/file-granularity.md

15. Completion Criteria for Golden Module

Define objective criteria for:

Module-Recovery-State: COMPLETE_REFERENCE_PATTERN

At minimum all must eventually be true:

A. Physical structure

module under one coherent Modules/Offer boundary

no arbitrary root dumping

Endpoints project exists and owns Offer endpoints

tests project/layout is coherent

B. Ownership

Offer-owned business logic inside Offer

foreign-owned logic removed/deferred explicitly

no Host-owned Offer business truth

C. Domain

domain files cohesive

no foreign Domain dependency

invariants explicit

D. Application

CQRS/use cases explicit

handlers in Application

no direct DbContext

no foreign Application coupling except explicitly approved temporary debt

E. Contracts

cross-module boundaries only via Contracts

no Domain/Application implementation leakage

F. Infrastructure

persistence module-owned

EF configurations separated

migrations module-owned

adapters/repositories explicit

G. Endpoints

Host no longer contains Offer business endpoints

Host only maps Offer endpoint module

H. Tests

characterization + domain + application + endpoint/infrastructure coverage sufficient

architecture guards protect pattern

I. Hygiene

no file >800 LOC

large files reduced or explicitly characterized with follow-up completed before final state

no baselines widened

git clean after task

J. Documentation

Capability Map / Master Recovery / Bootstrap reflect final ownership

Evidence:
docs/evidence/TB-TMAR-OFFER-REF-W1/completion-criteria.md

16. Migration Waves Plan

Create an evidence-based sequence to complete Offer.

Expected categories, but derive exact waves from repo:

W2 — physical module root + project/folder structure + guards
W3 — Domain decomposition/placement
W4 — Application use-case/CQRS organization
W5 — Infrastructure/persistence/configurations/migrations
W6 — Offer.Endpoints extraction from Host
W7 — Tests consolidation + architecture guards
W8 — final residual cleanup and COMPLETE_REFERENCE_PATTERN

Do NOT mechanically force exactly eight waves.
Choose the smallest safe sequence that still finishes Offer completely.

Each planned wave must have:

scope

preconditions

expected files/projects

tests

exit criteria

rollback/safety notes

Evidence:
docs/evidence/TB-TMAR-OFFER-REF-W1/migration-plan.md

17. Git Hygiene

At end:

no build/test artifacts left visible

no new noisy untracked files

git status summarized

generated temp outputs must be ignored by existing policy

do not modify .gitignore unless a genuinely new artifact family is discovered

Evidence:
docs/evidence/TB-TMAR-OFFER-REF-W1/git-hygiene.md

18. W2 Selection

Return exact next task:

Next-Recommended-Task: TB-TMAR-OFFER-REF-W2

Only if discovery is complete enough to begin implementation.

Also return:
Offer-Reference-Readiness: READY_FOR_W2
or
Offer-Reference-Readiness: NEEDS_MORE_DISCOVERY

Do not ask the user.

19. Recovery Updates

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-CAPABILITY-MAP.md only with evidence-backed Offer facts

docs/evidence/TB-TMAR-OFFER-REF-W1/recovery-sot.md

Record:

Checkout paused at accepted W4 checkpoint

Offer is active reference-module track

frontend remains frozen

next Offer task

20. Acceptance Criteria

PASS only if:

repository clean/recoverable

W5 checkout not accidentally executed

complete Offer surface inventory exists

ownership map exists

Domain/Application/Contracts/Infrastructure/Endpoints/Tests audited

dependency graph exists

target structure is exact and evidence-based

completion criteria for COMPLETE_REFERENCE_PATTERN defined

staged migration plan exists

no production frontend changes

no broad Offer refactor performed in discovery wave

user work preserved

git hygiene clean

canonical Result delivered

Worker stops completely

21. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
TMAR-Execution-Mode
Frontend-Production-Changes
Offer-Surface-Inventory
Current-Topology
Ownership-Map
Domain-Audit
Application-Audit
Contracts-Audit
Infrastructure-Audit
Endpoints-Audit
Tests-Audit
Dependency-Graph
Target-Structure
File-Granularity
Completion-Criteria
Migration-Plan
Git-Hygiene
Offer-Reference-Readiness
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
Track: OFFER_REFERENCE_MODULE
TMAR-Execution-Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Frontend-Production-Changes: NONE
Next-Recommended-Task: TB-TMAR-OFFER-REF-W2

After canonical Result:
STOP completely.

Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK