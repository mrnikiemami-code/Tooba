PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-HOST-W6

Parent-Task:
TB-TMAR-HOST-W5

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
TMAR Host Wave 6 — Remove One More High-Value Host Write Slice and Force a Pivot Decision

Task Type:
IMPLEMENTATION — EVIDENCE-SELECTED HOST DEBT REDUCTION + PIVOT CHECKPOINT

0. Architect Intent

HOST-W5 is accepted.

Verified state:

UnitOfMeasure Create/Update/Deactivate Host writes removed

Host endpoints no longer own SaveChanges/Add for UoM writes

Host-Exit-State = NOT_READY

Host-Recovery-State = CONTINUE_HOST

Architecture-Priority = HOST

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

Orders-Frontend-Readiness = UNCHANGED

residual Host write areas include Shipping / Seller / ProductWorkspace / Order-adjacent

UoM List/Get remains Host read composition via CatalogDbContext; this is READ debt, not the write target for this task

Checkout shared-ACID remains deferred

new Host writes are already frozen

Primary objectives:

refresh live remaining Host write inventory

remove exactly ONE more high-value SAFE Host write slice

shrink Host-write baseline again

separate remaining WRITE debt from READ-composition debt explicitly

force an architecture pivot decision after this wave:

continue Host only if another genuinely high-value safe write slice remains

otherwise pivot to Checkout consistency design or Contracts

do not chase read-only Host composition merely to make Host look cleaner

No broad Host rewrite.
No Checkout/Saga implementation.
No cosmetic migration.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-HOST-W5/recovery-sot.md

docs/evidence/TB-TMAR-HOST-W5/host-write-inventory.md

docs/evidence/TB-TMAR-HOST-W5/host-exit-criteria.md

docs/evidence/TB-TMAR-HOST-W5/architecture-priority.md

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
221d35d2e0704c57f93bf7b85c98c3fa766408b7

If conflicting tracked user work exists:
STOP with RECOVERY_CONFLICT.

Never:

git reset

git clean

destructive checkout/restore

unsafe rebase

blind stash manipulation

broad git add .

Evidence:
docs/evidence/TB-TMAR-HOST-W6/recovery-start.md

2. Refresh and Split Host Debt Inventory

Re-scan live Host production code.

Classify each candidate into TWO axes:

Write/behavior axis:

DIRECT_DB_WRITE

DIRECT_TRANSACTION

BUSINESS_DECISION

READ_COMPOSITION_ONLY

PRESENTATION_ONLY

FALSE_POSITIVE

NEEDS_DESIGN

Migration-risk axis:

SAFE_LOCAL_SLICE

OWNERSHIP_AMBIGUOUS

CROSS_CONTEXT_CONSISTENCY

ORDER_WORKFLOW_ADJACENT

READ_MODEL_DEBT

Explicitly separate:
A. write debt that blocks modular/microservice readiness
B. read-composition debt that may later move behind read gateways but does not justify unsafe migration now

Produce:
docs/evidence/TB-TMAR-HOST-W6/host-debt-inventory.md
docs/evidence/TB-TMAR-HOST-W6/host-debt-inventory.json

3. Select Exactly ONE SAFE Write Slice

Known residual candidates include Shipping / Seller / ProductWorkspace / Order-adjacent.

Selection priority:

direct Host persistence

clear bounded-context owner

local transaction/no transaction

no cross-context consistency semantics

low-to-moderate regression radius

meaningful extraction value

no ownership ambiguity

Preferred if evidence supports:

Shipping configuration

narrow Seller administration

narrow ProductWorkspace write

Reject for this task:

Order-adjacent workflow

checkout/payment/fulfillment lifecycle

anything classified CROSS_CONTEXT_CONSISTENCY

anything requiring Saga/process-manager redesign

anything whose owner is unclear

Before coding document:

Host file/member

owner module

current DbContext/repository

exact write effects

business decisions

transaction behavior

callers

target CQRS path

expected Host baseline shrink

why safer than alternatives

Evidence:
docs/evidence/TB-TMAR-HOST-W6/slice-selection.md

4. Characterization Before Migration

Before implementation, lock current behavior.

Cover as applicable:

request/input mapping

validation

authorization/store/tenant

write effects

ordering

error semantics

transaction behavior

cache invalidation

response/status shape

idempotency where applicable

No broad snapshots.
No workaround logic.

Evidence:
docs/evidence/TB-TMAR-HOST-W6/characterization-tests.md

5. Target CQRS/Application Shape

Target:

Host endpoint/composer
→ ISender
→ Command
→ Handler in owning module Application
→ module-owned abstraction/Directory
→ Infrastructure persistence

Rules:

business Handler in Application

DbContext/EF/transaction in Infrastructure

Host may only retain transport/auth/presentation concerns

no pass-through back into Host

existing Directory may remain as strangler seam only if semantically valid

do not create a new god-Directory

Evidence:
docs/evidence/TB-TMAR-HOST-W6/target-shape.md

6. Remove Direct Host Persistence

For selected slice:

remove direct DbContext/repository write dependency from Host

remove Host SaveChanges/transaction ownership

move persistence to Infrastructure

move business orchestration to Application/Domain

preserve behavior

If ownership ambiguity is found:

classify NEEDS_DESIGN

abandon that candidate

select another SAFE_LOCAL_SLICE

do not cement wrong ownership

Evidence:
docs/evidence/TB-TMAR-HOST-W6/host-write-removal.md

7. Business Decision Ownership

Remove business truth from Host where present.

Examples:

shipping rule/configuration validity

seller state transitions

workspace save invariants

default/fallback business truth

Host may retain:

request parsing

auth/session edge

route binding

response mapping

presentation composition

Evidence:
docs/evidence/TB-TMAR-HOST-W6/business-decision-removal.md

8. Transaction Semantics

If selected slice is module-local:

preserve exact transaction semantics

move transaction implementation to Infrastructure

If transaction is cross-context:

reject slice

choose another candidate

ARCH-TX-001 remains active.

Evidence:
docs/evidence/TB-TMAR-HOST-W6/transaction-semantics.md

9. Cache / Foundation Compliance

If cache is touched:

use ICache / ICacheKeyBuilder / ICacheInvalidator

no direct IMemoryCache

no Redis

no polling/magic timeout workaround

New code must follow:

MediatR 12.5.0

Handler in Application

FluentValidation where applicable

IClock

IIdGenerator

semantic errors

no hardcoded localized user-facing Domain/Application messages

unlimited-locale-safe

Evidence:
docs/evidence/TB-TMAR-HOST-W6/foundation-compliance.md

10. Host Write Baseline Shrink

Required:

selected Host write entry removed

no new Host write entries

no baseline widening

no wildcard suppression

Also verify:

App→App baseline does not grow

Infra→foreign App does not grow

Domain→foreign Domain remains clean

Infra→foreign Domain remains clean

cross-context transaction baseline does not grow

frontend guards remain green

source-size baseline does not grow

Evidence:
docs/evidence/TB-TMAR-HOST-W6/architecture-guards.md

11. Read-Debt Separation

Do NOT migrate UoM List/Get or other read-only Host composition merely because it is still direct DbContext access.

Instead classify remaining read debt into:

acceptable temporary composition

candidate for future read gateway

cross-module read-model debt

presentation-only query

Create:
docs/evidence/TB-TMAR-HOST-W6/host-read-debt.md

This is planning/evidence only.
No broad read-layer refactor in this task.

12. Source-Size Safety

No new hand-written source >800 LOC.

Existing oversized files:

shrink-only

no new giant handlers/directories

no broad decomposition

If target Application file would exceed 800:
split by use-case/capability.

Evidence:
docs/evidence/TB-TMAR-HOST-W6/source-size-compliance.md

13. Tests

Required:

selected characterization tests

affected module tests

Host endpoint/integration tests if present

ArchitectureBoundaryTests

TMAR foundation tests

Host-write guard

source-size guard

transaction guard

Contracts guard

No unrelated broad test-project creation.

Evidence:
docs/evidence/TB-TMAR-HOST-W6/tests.md

14. Host Exit State — Stronger Decision

Return exactly:
Host-Exit-State: NOT_READY
or
Host-Exit-State: READY_TO_PIVOT

READY_TO_PIVOT when:

new Host writes are frozen

multiple high-value direct write slices have been removed

remaining write debt is mostly:

order/checkout-adjacent

ownership-ambiguous

lower-value residual

or requires a distinct design task

remaining read debt can be handled later via read gateways

new features have clear CQRS/Application path

continuing repetitive Host waves is no longer best use of TMAR effort

Evidence:
docs/evidence/TB-TMAR-HOST-W6/host-exit-state.md

15. Checkout Design Readiness

Assess whether TMAR has enough evidence to start:

TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN

Return:
Checkout-Consistency-Design-Readiness: READY
or
Checkout-Consistency-Design-Readiness: DEFER

READY requires:

cross-context TransactionScope participant inventory exists

Order/Cart/Inventory/Payment dependencies are known enough

no implementation is needed yet

design-first can proceed without guessing

Evidence:
docs/evidence/TB-TMAR-HOST-W6/checkout-design-readiness.md

16. Orders Frontend Readiness

Return:
Orders-Frontend-Readiness: IMPROVED
or
Orders-Frontend-Readiness: UNCHANGED

Do not modify frontend Orders.

Evidence:
docs/evidence/TB-TMAR-HOST-W6/orders-frontend-readiness.md

17. Architecture Priority

Return exactly one:
Architecture-Priority: HOST
Architecture-Priority: CHECKOUT_DESIGN
Architecture-Priority: CONTRACTS
Architecture-Priority: FE_GODFILE

Guidance:

HOST only if another SAFE_LOCAL_SLICE clearly remains high-value

CHECKOUT_DESIGN if remaining Host write debt is increasingly order/checkout-adjacent and design readiness is READY

CONTRACTS if remaining App→App / Infra→App edges now dominate

FE_GODFILE only if frontend characterization readiness has become READY

Evidence:
docs/evidence/TB-TMAR-HOST-W6/architecture-priority.md

18. Next Task Decision

Choose automatically:

A. TB-TMAR-HOST-W7
only if Host-Exit-State = NOT_READY AND HOST is still clearly highest value.

B. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if Checkout-Consistency-Design-Readiness = READY and CHECKOUT_DESIGN is highest value.

C. TB-TMAR-CONTRACTS-W7
if CONTRACTS is highest value.

D. TB-TMAR-FE-GODFILE-W1
only if FE_GODFILE is highest value and readiness is READY.

Do not ask the user.

19. Product Safety

No product behavior change.

Do NOT:

change public API semantics

change routes

alter pricing/inventory/seller/order truth

alter UI/storefront

redesign checkout

implement Saga

alter order lifecycle semantics

20. Recovery Updates

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md only if durable lock changes

docs/architecture/TOOBA-CAPABILITY-MAP.md only if material ownership evidence changes

docs/evidence/TB-TMAR-HOST-W6/recovery-sot.md

Record:

selected Host slice

Host baseline before/after

write-vs-read debt split

Host-Exit-State

Checkout-Consistency-Design-Readiness

Orders-Frontend-Readiness

Architecture-Priority

next task

21. Acceptance Criteria

PASS only if:

live Host debt inventory refreshed and split by write/read risk

exactly one safe Host write slice migrated

behavior characterized before migration

Host persistence removed for selected slice

Application owns orchestration

Infrastructure owns persistence/transaction

Host baseline shrinks

no bad dependency expansion

no new cross-context transaction

read-only debt is not cosmetically migrated

tests/guards pass

Host-Exit-State returned

Checkout design readiness assessed

Orders frontend readiness assessed

Architecture-Priority returned

user work preserved

next task selected automatically

canonical Result delivered

Worker stops completely

22. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Host-Debt-Inventory
Slice-Selection
Characterization-Tests
Target-Shape
Host-Write-Removal
Business-Decision-Removal
Transaction-Semantics
Foundation-Compliance
Architecture-Guards
Host-Read-Debt
Source-Size-Compliance
Tests
Host-Exit-State
Checkout-Consistency-Design-Readiness
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