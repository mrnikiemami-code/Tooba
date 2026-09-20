PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-TAX-REFERENCE-W1

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
TMAR Tax Reference Module — Apply Proven Offer Golden Pattern Completely

Task Type:
IMPLEMENTATION — BACKEND-ONLY COMPLETE MODULE RECOVERY

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Target-Module:
Tax

Reference-Pattern:
docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md

Module-Completion-Requirement:
COMPLETE_REFERENCE_PATTERN

0. Architect Intent

TB-TMAR-OFFER-REFERENCE-W1 is accepted.

Offer is now the canonical backend reference module:

Domain/Application/Contracts/Infrastructure/Endpoints/Tests structure proven

module-owned endpoints extracted from Host

Host composition thin

namespaces aligned

architecture guards active

Offer oversized production files = 0

reference pattern published

Checkout remains paused at safe W5 checkpoint

Next module selected:
Tax

Reason:

Tax is one of the Worker-identified next candidates

Tooba.Tax.Contracts already exists from prior TMAR boundary recovery

Tax is expected to be smaller and safer than Pricing/Inventory

ideal for validating that the Offer pattern is reusable before moving to larger modules

This task must complete Tax fully.
Do NOT leave it half-migrated.

PASS requires:
Module-Recovery-State: COMPLETE_REFERENCE_PATTERN

If a real blocker prevents full completion:
return BLOCKED with exact blocker.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Expected previous accepted tip:
6b029e60e2939f6e85e5c412bc1c6ac24503d5b4

Read:

docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-OFFER-REFERENCE-W1/recovery-sot.md

live Tax project structure and references

prior Tax Contracts evidence from TMAR

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

3. Tax Baseline Inventory

Inventory all Tax-owned backend code:

projects

files

LOC

namespaces

project references

package refs

Domain types

Application handlers/use cases

Contracts

Infrastructure

DbContext

EF configurations

migrations

repositories/adapters

endpoints currently in Host

tests

events/outbox

inbound/outbound dependencies

oversized/mixed-responsibility files

Produce:
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/tax-baseline.md
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/tax-baseline.json

Return:
Tax-Reference-Suitability: CONFIRMED
or
Tax-Reference-Suitability: BLOCKED

If BLOCKED due to fundamental ownership ambiguity:
STOP.

4. Apply Canonical Module Structure

Apply the Offer-proven pattern to Tax.

Target logical structure:

src/backend/Modules/Tax/
Tooba.Tax.Domain/
Tooba.Tax.Application/
Tooba.Tax.Contracts/
Tooba.Tax.Infrastructure/
Tooba.Tax.Endpoints/
Tooba.Tax.Tests/

Only create folders/projects backed by real responsibility.
No ceremonial empty folders.

5. Domain Recovery

Requirements:

aggregates/entities/value objects/policies/events/errors placed coherently

invariants remain in Domain

no Infrastructure dependency

no foreign Domain dependency

semantic errors only

no localized user-facing Domain messages

IClock/IIdGenerator rules obeyed by new code

No business rewrite.

Evidence:
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/domain-recovery.md

6. Application Recovery

Requirements:

use cases grouped coherently

handlers in Application

validation where appropriate

no DbContext in Application

outbound dependencies via Contracts/Ports

no Host dependency

no foreign Application dependency when Contracts exist

no giant Directory growth

Evidence:
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/application-recovery.md

7. Contracts Recovery

Tooba.Tax.Contracts already exists and must become canonical.

Verify/recover:

ITaxCalculator

Tax calculation request/result/outcome types

any other stable cross-module Tax contracts

Rules:

no Domain leakage

no Application implementation leakage

no EF types

future remote-adapter friendly

external modules depend on Tax.Contracts, not Tax.Application/Domain

Evidence:
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/contracts-recovery.md

8. Infrastructure Recovery

Requirements:

TaxDbContext under Persistence

EF configurations under Persistence/Configurations

migrations under Persistence/Migrations

repositories/adapters separated

DI registration isolated

no business handlers in Infrastructure

no foreign Domain dependency

no inappropriate foreign Application dependency

Evidence:
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/infrastructure-recovery.md

9. Persistence Ownership

Verify:

Tax owns its schema/tables

TaxDbContext owns only Tax data

migrations Tax-owned

no cross-module FK

no shared mega-DbContext expansion

additive safe migration model

Evidence:
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/persistence-ownership.md

10. Endpoints Extraction

Create/complete:
Tooba.Tax.Endpoints

Move Tax-owned HTTP endpoints/composers out of Host.

Host target:
composition only
→ map/register Tax module

Endpoint project:

transport mapping only

no DbContext writes

no SaveChanges

no business truth

no foreign module business logic

Preserve routes exactly.

Evidence:
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/endpoints-extraction.md

11. Thin Host Integration

Host may only:

register Tax

map Tax endpoints

provide cross-cutting middleware/platform

No Tax business implementation in Host.

HOST-MODULE-ENDPOINT-001 remains active.

Evidence:
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/host-integration.md

12. Tests Project

Create/complete:
Tooba.Tax.Tests

Cover:

Domain

Application

Contracts

Infrastructure

Endpoints

Architecture

No giant test files.

Evidence:
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/tests-structure.md

13. Namespace Alignment

Align namespaces with final Tax structure where mechanically safe:

Tooba.Tax.Domain.*

Tooba.Tax.Application.*

Tooba.Tax.Contracts.*

Tooba.Tax.Infrastructure.*

Tooba.Tax.Endpoints.*

Update usings/references.
Verify DI/reflection/EF discovery.

No namespace churn outside Tax.

Evidence:
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/namespace-alignment.md

14. Source-Size / File Cohesion

ARCH-MODULE-FILE-001 remains active.

Requirements:

no new >800 LOC production files

watch >500 LOC

Tax production oversized files expected = 0

split only where responsibility is clear

no rewrite for cosmetic splitting

Return:
Tax-Oversized-Files: 0
or BLOCKED.

Evidence:
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/source-size.md

15. Architecture Guards

Apply reusable reference-module guards to Tax:

Domain dependency direction

Application dependency direction

Contracts cleanliness

Infrastructure ownership

Endpoints isolation

Host endpoint prohibition

no foreign Domain dependency

no foreign Application dependency where Contracts exists

no root dumping

source-size guard

module-local tests

Evidence:
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/architecture-guards.md

16. Dependency Graph

Produce final Tax dependency graph.

No cycles.
External consumers use Tax.Contracts.

Evidence:
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/dependency-graph.md

17. Behavior Preservation / Validation

Required:

Tax projects build

Host build

Tax tests

endpoint characterization

persistence tests

architecture tests

TMAR foundation tests

durable guard tests

no frontend changes

no route/API regression

Required:
NEW_FAILURES=0

Evidence:
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/tests.md

18. No-Rewrite / No-Workaround

ARCH-RECOVERY-001 and ARCH-NOWORKAROUND-001 remain active.

No:

Big Bang rewrite

behavior redesign

service locator

reflection workaround

magic retry/polling

catch-and-ignore

baseline widening

test-only branches

19. Module Completion Gate

PASS only if:

Module-Recovery-State: COMPLETE_REFERENCE_PATTERN

Requires:

Tax structure complete

Domain/Application/Contracts/Infrastructure clean

persistence ownership clean

endpoints out of Host

Host thin

tests module-owned

guards active

Tax production oversized files = 0

namespaces aligned

dependency graph clean

no frontend changes

no behavior regression

user work preserved

Anything else:
BLOCKED, not PASS.

20. Checkout Pause State

Keep:
Checkout-Recovery-State: PAUSED_AT_SAFE_W5_CHECKPOINT

Do not resume Checkout W6.

21. Reference Pattern Reuse Validation

Update:
docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md

Only if Tax reveals reusable refinements to the Offer pattern.

Do NOT rewrite the pattern arbitrarily.
Record:

what reused unchanged

what Tax exposed that Offer did not

whether pattern is now proven across 2 modules

Return:
Reference-Pattern-Reuse-State: PROVEN_ON_2_MODULES

only if supported.

Evidence:
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/reference-pattern-reuse.md

22. Next Module Candidates

After Tax completion, rank only by architectural effort/evidence, not preference:

Pricing

Inventory

one other small/medium candidate if discovered

Do not start next module.

Evidence:
docs/evidence/TB-TMAR-TAX-REFERENCE-W1/next-module-candidates.md

23. Recovery Updates

Update:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md only if new durable rule justified

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md if evidence warrants

docs/evidence/TB-TMAR-TAX-REFERENCE-W1/recovery-sot.md

Preserve:
TMAR-Execution-Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

24. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
TMAR-Execution-Mode
Frontend-Production-Changes
Tax-Reference-Suitability
Tax-Baseline
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
Tax-Reference-Suitability: CONFIRMED
Tax-Oversized-Files: 0
Module-Recovery-State: COMPLETE_REFERENCE_PATTERN
Checkout-Recovery-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

After canonical Result:
STOP completely.

Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK
