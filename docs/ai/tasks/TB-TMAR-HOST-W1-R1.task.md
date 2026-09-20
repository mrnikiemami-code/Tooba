PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-HOST-W1-R1

Parent-Task:
TB-TMAR-HOST-W1

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
Host Wave 1 Repair — Move CQRS Handlers to Application Layer and Preserve Infrastructure Boundary

Task Type:
NARROW ARCHITECTURE REPAIR

Reason

TB-TMAR-HOST-W1 removed direct Host writes successfully, but the reported implementation places:

StoreLandingPageWriteHandlers.cs

inside:

Catalog.Infrastructure

This is not the intended TMAR CQRS layering.

Canonical target:

Host/Endpoint
→ ISender
→ Command/Query + Handler in Application
→ Application abstraction / Domain
→ Infrastructure implementation / DbContext

Infrastructure must implement persistence and external adapters.
Application must own use-case handlers.

Do NOT proceed to HOST-W2 until this repair is complete.

0. Recovery Safety

Verify:

branch main

exact HEAD and origin/main

HEAD == origin/main

18ca10c9 ancestor

user work preserved

No destructive git operations.

Evidence:
docs/evidence/TB-TMAR-HOST-W1-R1/recovery-start.md

1. Exact Scope

Inspect only the Store Landing/Page Composition CQRS slice created by TB-TMAR-HOST-W1.

Primary target:

StoreLandingPageWriteHandlers.cs

related handler registration

Application/Infrastructure references needed for those handlers

Do not expand to other Host write slices.

2. Correct Layering

Required final shape:

Tooba.Catalog.Application

Commands

Validators

MediatR Handlers

use-case orchestration

persistence/gateway abstractions needed by handlers

Tooba.Catalog.Infrastructure

CatalogDbContext

EF/persistence implementations

transaction implementation

implementations of Application abstractions

module DI registration as needed

Handlers MUST NOT remain in Infrastructure merely because they call an Infrastructure implementation.

Application must not reference Infrastructure.

If current IStoreLandingPageDirectory is the Application abstraction, the handler should depend on that interface and live in Application.

If the current abstraction is not suitable, make the smallest safe adjustment required without redesigning the whole module.

3. Transaction Preservation

Preserve current SetHome atomicity and relational transaction semantics.

Do not move EF transaction code into Application.
Application may orchestrate through an abstraction; Infrastructure owns EF-specific transaction mechanics.

No generic/global transaction behavior in this repair.

4. Registration

Ensure MediatR scans/registers the Application assembly containing the handlers.

Do not rely on Infrastructure handler scanning after repair.

Avoid duplicate handler registration.

5. Architecture Guard

Add/strengthen an architecture test:

MediatR request handlers for module business use-cases must not live in *.Infrastructure

Infrastructure must not become the application use-case layer

Make the test general enough to prevent recurrence, without false positives for technical/infrastructure handlers that are not MediatR business request handlers.

Do not use wildcard suppressions.

Evidence:
docs/evidence/TB-TMAR-HOST-W1-R1/architecture-guard.md

6. Tests

Required:

Store Landing/Page Composition focused tests

MediatR handler resolution

validation behavior

SetHome transaction behavior

ArchitectureBoundaryTests

TMAR foundation tests

All must pass.

Evidence:
docs/evidence/TB-TMAR-HOST-W1-R1/tests.md

7. No Scope Creep

Do NOT:

start HOST-W2

move StoreLandingPage Domain ownership

create Contracts projects

reorganize folders

mass-migrate Directories

modify unrelated modules

install new packages

add Redis

change user-visible behavior

8. Capability / Recovery

Update narrowly:

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md if current task state is tracked there

docs/evidence/TB-TMAR-HOST-W1-R1/recovery-sot.md

Record that HOST-W1 is accepted only after this layering repair.

Acceptance

PASS only if:

StoreLandingPage MediatR business handlers live in Catalog.Application

Catalog.Application does NOT reference Catalog.Infrastructure

EF/DbContext/transaction implementation remains in Infrastructure

Host remains free of the removed direct writes

behavior and transaction semantics preserved

architecture guard prevents business MediatR handlers from drifting into Infrastructure

focused tests pass

user work preserved

canonical Result sent through Bridge

Worker stops completely

Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Recovery-Start
Before-Layering
After-Layering
Application-Handlers
Infrastructure-Persistence
Transaction-Preservation
Registration
Architecture-Guard
Tests
Changed-Files
Capability-Map
Recovery-SoT
Git
Architectural-Concerns
Blockers
Next-Recommended-Task

After canonical Result through Bridge:
STOP completely.

Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK