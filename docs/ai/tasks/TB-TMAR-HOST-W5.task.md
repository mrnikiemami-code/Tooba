PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-HOST-W5

Parent-Task:
TB-TMAR-HOST-W4

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
TMAR Host Wave 5 — Remove One More Safe Direct Host Write Slice and Establish Host Exit Criteria

Task Type:
IMPLEMENTATION — EVIDENCE-SELECTED HOST DEBT REDUCTION + EXIT CHECKPOINT

0. Architect Intent

HOST-W4 is accepted.

Verified state:

QuantitySettings direct Host write has been removed

Host → ISender → Application Handler → Directory → Infrastructure pattern is proven

Host-Recovery-State = CONTINUE_HOST

Architecture-Priority = HOST

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

Orders-Frontend-Readiness = UNCHANGED

residual known Host write areas include UoM / Shipping / Seller / ProductWorkspace / Order-adjacent

Checkout shared-ACID remains deferred

frontend low-risk flat-admin recovery is already READY_TO_PIVOT

Primary objectives:

refresh remaining live Host write inventory

select exactly ONE additional safe, high-value Host write slice

migrate it behind CQRS/Application ownership

shrink Host-write baseline

define objective Host exit criteria

decide whether Host cleanup should continue or TMAR should pivot to Checkout consistency design / Contracts / FE godfile

No broad Host rewrite.
No ownership invention.
No Order/Checkout redesign.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-HOST-W4/recovery-sot.md

docs/evidence/TB-TMAR-HOST-W4/host-write-inventory.md

docs/evidence/TB-TMAR-HOST-W4/host-recovery-state.md

docs/evidence/TB-TMAR-HOST-W4/architecture-priority.md

Verify:

branch main

exact HEAD SHA

exact origin/main SHA

HEAD == origin/main

18ca10c9 ancestor

git status --short

git diff --name-only

git diff --cached --name-only

user work preserved

Expected previous accepted tip:
6db06b24f283b3cc55833be95fae22f314933184

If conflicting tracked user work exists:
STOP with RECOVERY_CONFLICT.

Never use destructive Git operations.
Never use broad git add ..

Evidence:
docs/evidence/TB-TMAR-HOST-W5/recovery-start.md

2. Refresh Remaining Host Write Inventory

Re-scan live Host production code for:

SaveChanges / SaveChangesAsync

Add / AddAsync / Update / Remove

BeginTransaction / TransactionScope

DbContext injection

direct repository writes

direct domain state mutation

business decisions in Host

Classify:

DIRECT_DB_WRITE

DIRECT_TRANSACTION

BUSINESS_DECISION

PRESENTATION_ONLY

FALSE_POSITIVE

NEEDS_DESIGN

Produce:
docs/evidence/TB-TMAR-HOST-W5/host-write-inventory.md
docs/evidence/TB-TMAR-HOST-W5/host-write-inventory.json

Do not rely only on W4 inventory.

3. Select Exactly ONE Safe Host Slice

Use live evidence.

Priority:

clear bounded-context owner

direct Host write

module-local transaction or no transaction

easy characterization

no cross-context workflow

meaningful Host baseline shrink

no god-file expansion

Preferred if evidence supports:

UoM settings/configuration

Shipping configuration

narrow Seller administration

narrow ProductWorkspace write

Avoid:

Order-adjacent workflow

checkout/payment/fulfillment lifecycle

anything requiring Saga/process-manager design

ownership-ambiguous slice

If UoM is a simple module-local analog to Quantity, it is a strong candidate, but choose only from evidence.

Document:

Host file/member

current persistence

owner module

business decisions

transaction semantics

callers

target Command/Handler

expected baseline shrink

Evidence:
docs/evidence/TB-TMAR-HOST-W5/slice-selection.md

4. Characterization Before Migration

Before code movement, characterize:

input mapping

validation

authorization/store/tenant scope

write effects

transaction behavior

error semantics

cache invalidation

response shape/status

No broad snapshots.
No fake shortcuts.

Evidence:
docs/evidence/TB-TMAR-HOST-W5/characterization-tests.md

5. Target Shape

Required:

Host
→ ISender
→ Command
→ Handler in owning Application
→ module-owned abstraction/Directory
→ Infrastructure persistence

Rules:

business handler in Application

EF/DbContext/transaction in Infrastructure

Host retains only transport/auth/presentation responsibilities

no pass-through back into Host

existing Directory can remain as strangler seam if semantically correct

Evidence:
docs/evidence/TB-TMAR-HOST-W5/target-shape.md

6. Remove Host Persistence / Business Truth

For selected slice:

remove Host DbContext/repository write usage

remove SaveChanges from Host

move business orchestration/decision to Application/Domain

move persistence to Infrastructure

preserve behavior exactly

If bounded-context ownership is unclear:

mark NEEDS_DESIGN

abandon that slice

select another safe slice

do NOT cement wrong ownership

Evidence:
docs/evidence/TB-TMAR-HOST-W5/host-write-removal.md
docs/evidence/TB-TMAR-HOST-W5/business-decision-removal.md

7. Transaction Semantics

Preserve exact behavior.

If transaction is module-local:

move it to Infrastructure

preserve isolation/order/rollback semantics

If transaction spans multiple bounded contexts:

reject the slice for this task

ARCH-TX-001 remains active.

Evidence:
docs/evidence/TB-TMAR-HOST-W5/transaction-semantics.md

8. Cache / Foundation Compliance

If cache is involved:

use ICache / ICacheKeyBuilder / ICacheInvalidator

no direct IMemoryCache

no Redis

no magic polling/TTL workaround

New code must follow:

MediatR 12.5.0

handlers in Application

FluentValidation where relevant

IClock

IIdGenerator

semantic errors

no hardcoded localized Domain/Application messages

unlimited-locale safe

Evidence:
docs/evidence/TB-TMAR-HOST-W5/foundation-compliance.md

9. Architecture Baseline Shrink

Required:

selected Host write entry removed

no Host-write baseline widening

no wildcard suppression

Also verify:

App→App does not grow

Infra→foreign App does not grow

Domain→foreign Domain remains clean

Infra→foreign Domain remains clean

cross-context transaction baseline does not grow

source-size baseline does not grow

frontend guards remain unaffected

Evidence:
docs/evidence/TB-TMAR-HOST-W5/architecture-guards.md

10. Source-Size Safety

No new hand-written source >800 LOC.

Existing oversized files:

shrink only

no new giant Handler/Directory

no broad decomposition in this task

If destination file would exceed 800 LOC:
split by use-case/capability instead of growing it.

Evidence:
docs/evidence/TB-TMAR-HOST-W5/source-size-compliance.md

11. Tests

Required:

selected slice characterization tests

affected module tests

Host integration/endpoint tests if present

ArchitectureBoundaryTests

TMAR foundation tests

Host-write guard

source-size guard

transaction guard

Contracts guards

No unrelated broad test-project creation.

Evidence:
docs/evidence/TB-TMAR-HOST-W5/tests.md

12. Host Exit Criteria

Create objective criteria:
docs/evidence/TB-TMAR-HOST-W5/host-exit-criteria.md

Evaluate:

A. New Host write growth is frozen.

B. Remaining direct writes:

are low-risk residual debt, OR

require ownership redesign, OR

require Checkout/Order consistency design.

C. New feature path is clearly:
Host → CQRS → Application → Infrastructure.

D. Host no longer owns high-value business truth for the migrated areas.

E. Continuing repetitive Host waves is no longer highest-value architecture work.

Return exactly:
Host-Exit-State: NOT_READY
or
Host-Exit-State: READY_TO_PIVOT

Do not set READY merely because this is W5.

13. Orders Frontend Readiness

Return:
Orders-Frontend-Readiness: IMPROVED
or
Orders-Frontend-Readiness: UNCHANGED

Do not modify frontend Orders.

Evidence:
docs/evidence/TB-TMAR-HOST-W5/orders-frontend-readiness.md

14. Architecture Priority

Return exactly one:
Architecture-Priority: HOST
Architecture-Priority: CHECKOUT_DESIGN
Architecture-Priority: CONTRACTS
Architecture-Priority: FE_GODFILE

Guidance:

HOST only if another safe, high-value direct Host write slice clearly remains

CHECKOUT_DESIGN if remaining Host debt is increasingly Order/Checkout-adjacent and shared-ACID risk dominates

CONTRACTS if remaining boundary leakage now outranks Host

FE_GODFILE only if readiness is actually READY

Evidence:
docs/evidence/TB-TMAR-HOST-W5/architecture-priority.md

15. Next Task Decision

Choose automatically:

A. TB-TMAR-HOST-W6
only if Host-Exit-State = NOT_READY and HOST is still highest value.

B. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if Host-Exit-State = READY_TO_PIVOT or CHECKOUT_DESIGN is highest value.

C. TB-TMAR-CONTRACTS-W7
if CONTRACTS is highest value.

D. TB-TMAR-FE-GODFILE-W1
only if FE_GODFILE is highest value and readiness is READY.

Do not ask the user.

16. Product Safety

No product behavior change.

Do NOT:

change public API behavior

change routes

change pricing/inventory/seller semantics

change storefront/UI

redesign checkout

implement Saga

alter order lifecycle semantics

17. Recovery Updates

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md only if durable rule changes

docs/architecture/TOOBA-CAPABILITY-MAP.md only if material ownership knowledge changes

docs/evidence/TB-TMAR-HOST-W5/recovery-sot.md

Record:

selected Host slice

Host baseline before/after

Host-Exit-State

Orders-Frontend-Readiness

Architecture-Priority

next task

18. Acceptance Criteria

PASS only if:

live Host inventory refreshed

exactly one coherent safe Host write slice migrated

characterization exists before migration

Host persistence removed

Application owns business handler/orchestration

Infrastructure owns persistence/transaction

Host baseline shrinks

no bad dependency expansion

no new cross-context transaction

no product behavior change

tests/guards pass

Host-Exit-State returned

Orders frontend readiness assessed

Architecture-Priority returned

user work preserved

next task selected automatically

canonical Result delivered

Worker stops completely

19. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Host-Write-Inventory
Slice-Selection
Characterization-Tests
Target-Shape
Host-Write-Removal
Business-Decision-Removal
Transaction-Semantics
Foundation-Compliance
Architecture-Guards
Source-Size-Compliance
Tests
Host-Exit-State
Orders-Frontend-Readiness
Architecture-Priority
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
Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

After canonical Result through Bridge:
STOP completely.

Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK