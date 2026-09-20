PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-PRICING-REFERENCE-W1

Parent-Task:
TB-TMAR-TAX-REFERENCE-W1

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
TMAR Pricing Reference Module — Apply Proven Golden Module Pattern Completely

Task Type:
IMPLEMENTATION — BACKEND-ONLY COMPLETE MODULE RECOVERY

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Target-Module:
Pricing

Reference-Pattern:
docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md

Module-Completion-Requirement:
COMPLETE_REFERENCE_PATTERN

0. Architect Intent

TB-TMAR-TAX-REFERENCE-W1 is accepted.

Reference-module pattern is now proven on TWO real modules:

Offer

Tax

Verified:

Module-Recovery-State = COMPLETE_REFERENCE_PATTERN

Reference-Pattern-Reuse-State = PROVEN_ON_2_MODULES

frontend production untouched

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

Next module selected:
Pricing

Reason:

Pricing was identified by Worker as one of the next candidates

it is architecturally more significant than Tax but still safer than Inventory/Order/Catalog

Pricing.Contracts already exists from prior TMAR recovery

it is the right next step to prove the Golden Pattern on a medium-complexity module before moving to larger modules

This task MUST complete Pricing fully.
No partial migration.
No “good enough” PASS.

PASS requires:
Module-Recovery-State: COMPLETE_REFERENCE_PATTERN

If a real blocker prevents full completion:
return BLOCKED with exact blocker.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Expected previous accepted tip:
400bed8c84e5620ae049453a8bb9e5655573e9dc

Read:

docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-TAX-REFERENCE-W1/recovery-sot.md

prior Pricing-related TMAR evidence

live Pricing project structure and references

Verify:

branch main

HEAD == origin/main

18ca10c9 ancestor

git status/diffs known

user work preserved

no frontend production changes

If conflict:
STOP with RECOVERY_CONFLICT.

No destructive Git operations.
No broad git add ..

2. Frontend Freeze

ARCH-FE-FREEZE-001 remains ACTIVE.

No production changes under:
src/frontend/**

Required:
Frontend-Production-Changes: NONE

3. Pricing Baseline Inventory

Inventory ALL Pricing-owned backend code:

projects

source files

LOC per file

namespaces

project/package references

Domain types

Application handlers/use cases

Pricing.Contracts

Infrastructure

DbContext

EF configurations

migrations

repositories/directories/adapters

endpoints/composers currently in Host

tests

events/outbox

inbound/outbound dependencies

oversized/mixed-responsibility files

pricing authority / campaign pricing / quote responsibilities

any dependency on Offer/Campaign/Inventory/Catalog/Tax/etc.

Produce:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/pricing-baseline.md
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/pricing-baseline.json

Return:
Pricing-Reference-Suitability: CONFIRMED
or
Pricing-Reference-Suitability: BLOCKED

If fundamental ownership ambiguity exists:
STOP and report.

4. Apply Canonical Module Structure

Target logical structure:

src/backend/Modules/Pricing/
Tooba.Pricing.Domain/
Tooba.Pricing.Application/
Tooba.Pricing.Contracts/
Tooba.Pricing.Infrastructure/
Tooba.Pricing.Endpoints/
Tooba.Pricing.Tests/

Only create folders/projects backed by real responsibility.
No empty ceremonial structure.

Use Offer/Tax as proven reference.

5. Domain Recovery

Requirements:

explicit aggregates/entities/value objects/policies/events/errors

pricing invariants stay in Domain

no Infrastructure dependency

no foreign Domain dependency

no localized user-facing messages

semantic errors only

IClock/IIdGenerator rules for new code

no business rewrite

Pay special attention to:

canonical price authority

currency/value objects

price resolution rules

campaign/cart price authority boundaries

any duplicated Offer/Promotion logic that must remain external via Contracts

Evidence:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/domain-recovery.md

6. Application Recovery

Requirements:

use cases grouped coherently

handlers in Application

validation via FluentValidation where appropriate

no DbContext usage

outbound dependencies through Contracts/Ports

no Host dependency

no foreign Application dependency where Contracts exists

no god-directory growth

Explicitly inspect:

price lookup

cart price resolution

seller/store context pricing

campaign/cart pricing authority

any sync query boundary

Evidence:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/application-recovery.md

7. Contracts Recovery

Tooba.Pricing.Contracts already exists and must become canonical.

Verify/recover at least:

IPriceLookupGateway

PriceQuote

PriceResolutionQuery

ICampaignCartPriceAuthority

CurrencyCode

other stable Pricing cross-module contracts actually in use

Rules:

no Domain leakage

no Application implementation leakage

no EF types

stable, version-friendly contracts

future remote-adapter friendly

external modules depend on Pricing.Contracts, not Pricing.Application/Domain

Evidence:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/contracts-recovery.md

8. Infrastructure Recovery

Requirements:

PricingDbContext under Persistence/

EF configurations under Persistence/Configurations/

migrations under Persistence/Migrations/

repositories/adapters separated

DI registration isolated

no business handlers in Infrastructure

no foreign Domain dependency

foreign Application dependency must be removed where a Contracts seam exists

Outbox placed coherently if used

Evidence:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/infrastructure-recovery.md

9. Persistence Ownership

Verify:

Pricing owns its schema/tables

PricingDbContext owns only Pricing data

migrations Pricing-owned

no foreign entity mapping

no cross-module DB FK

no shared mega-DbContext expansion

additive-safe migration discipline

Evidence:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/persistence-ownership.md

10. Endpoints Extraction

Create/complete:
Tooba.Pricing.Endpoints

Move Pricing-owned HTTP endpoints/composers OUT of Tooba.Host.

Host target:
composition only
→ map/register Pricing module

Endpoint project:

transport mapping only

no DbContext writes

no SaveChanges

no pricing business truth

no foreign module business logic

Preserve public routes exactly.

If Pricing currently has no owned Host routes:
document that explicitly and create only the justified module composition point.

Evidence:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/endpoints-extraction.md

11. Thin Host Integration

Host may only:

register Pricing

map Pricing endpoints

provide cross-cutting platform services

No Pricing business implementation in Host.

HOST-MODULE-ENDPOINT-001 remains active.

Any Host BFF/composer using Pricing for cross-module enrichment must be classified:

genuine BFF/presentation composition

misplaced Pricing endpoint

business logic that should leave Host

Do not hide business logic under a Host folder.

Evidence:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/host-integration.md

12. Tests Project

Create/complete:
Tooba.Pricing.Tests

Cover:

Domain invariants

Application use cases

Contracts

Infrastructure persistence/adapters

Endpoints

Architecture

No giant test files.

Evidence:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/tests-structure.md

13. Namespace Alignment

Align namespaces with final structure where mechanically safe:

Tooba.Pricing.Domain.*

Tooba.Pricing.Application.*

Tooba.Pricing.Contracts.*

Tooba.Pricing.Infrastructure.*

Tooba.Pricing.Endpoints.*

Update usings/references.
Verify:

DI scanning

EF config discovery

migrations assembly

reflection if any

No namespace churn outside Pricing.

Evidence:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/namespace-alignment.md

14. Source-Size / File Cohesion

ARCH-MODULE-FILE-001 remains active.

Requirements:

no new production file >800 LOC

watch >500 LOC

split only on clear responsibility boundaries

no cosmetic rewrite

Expected final:
Pricing-Oversized-Files: 0

If not safely achievable:
BLOCKED with exact file/reason.

Evidence:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/source-size.md

15. Dependency Boundary Cleanup

Pricing must follow the proven module rule.

Audit all outbound edges and classify:

Contracts boundary

local BuildingBlock

temporary grandfathered dependency

invalid foreign Application/Domain dependency

For every invalid edge where a stable Contracts boundary already exists:
remove it.

Do NOT invent broad new facades merely to make the graph look clean.

Evidence:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/dependency-cleanup.md

16. Architecture Guards

Apply reference-module guards:

Domain dependency direction

Application dependency direction

Contracts cleanliness

Infrastructure ownership

Endpoints isolation

Host endpoint prohibition

no foreign Domain dependency

no foreign Application dependency where Contracts exists

no root dumping

source-size

module-local tests

Also ensure:

ARCH-FE-FREEZE-001

ARCH-FOLDER-OWNERSHIP-001

ARCH-RECOVERY-001

ARCH-BASELINE-001

ARCH-NOWORKAROUND-001

ARCH-DATA-001
remain green.

Evidence:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/architecture-guards.md

17. Dependency Graph

Produce final Pricing dependency graph.

No cycles.
External consumers use Pricing.Contracts.

Evidence:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/dependency-graph.md

18. Behavior Preservation / Validation

Required:

Pricing projects build

Host build

Pricing tests

endpoint characterization

persistence tests

architecture tests

TMAR foundation tests

durable guard tests

no frontend production changes

no public route/API regression

no pricing behavior change

Required:
NEW_FAILURES=0

Evidence:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/tests.md

19. No-Rewrite / No-Workaround

ARCH-RECOVERY-001 and ARCH-NOWORKAROUND-001 remain active.

No:

Big Bang rewrite

pricing algorithm redesign

service locator

reflection workaround

polling/magic retry

catch-and-ignore

baseline widening

test-only branches

duplicated Offer/Promotion/Tax logic

20. Module Completion Gate

PASS only if:

Module-Recovery-State: COMPLETE_REFERENCE_PATTERN

Requires:

final Pricing project/folder structure complete

Domain/Application/Contracts/Infrastructure clean

persistence ownership clean

endpoints out of Host or explicitly none-owned

Host thin

tests module-owned

architecture guards active

Pricing production oversized files = 0

namespaces aligned

dependency graph clean

no frontend changes

no behavior regression

user work preserved

Anything else:
BLOCKED, not PASS.

21. Checkout Pause State

Keep:
Checkout-Recovery-State: PAUSED_AT_SAFE_W5_CHECKPOINT

Do not resume Checkout W6.

22. Reference Pattern Reuse Validation

Update reference pattern only if Pricing reveals reusable refinements.

Return:
Reference-Pattern-Reuse-State: PROVEN_ON_3_MODULES

only if Pricing completes cleanly.

Evidence:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/reference-pattern-reuse.md

23. Next Module Candidates

After Pricing completion, assess:

Inventory

Cart

one other small/medium backend candidate

Do not start next module.

Evidence:
docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/next-module-candidates.md

24. Recovery Updates

Update:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md only if new durable rule justified

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md if evidence warrants

docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/recovery-sot.md

Preserve:
TMAR-Execution-Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

25. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
TMAR-Execution-Mode
Frontend-Production-Changes
Pricing-Reference-Suitability
Pricing-Baseline
Target-Structure
Domain-Recovery
Application-Recovery
Contracts-Recovery
Infrastructure-Recovery
Persistence-Ownership
Endpoints-Extraction
Host-Integration
Tests-Structure
Namespace-Alignment
Source-Size
Dependency-Cleanup
Architecture-Guards
Dependency-Graph
Tests
Module-Recovery-State
Checkout-Recovery-State
Reference-Pattern-Reuse-State
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
Pricing-Reference-Suitability: CONFIRMED
Pricing-Oversized-Files: 0
Module-Recovery-State: COMPLETE_REFERENCE_PATTERN
Checkout-Recovery-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

After canonical Result:
STOP completely.

Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK
