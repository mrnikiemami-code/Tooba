PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-HOST-W4

Parent-Task:
TB-TMAR-HOST-W3

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
TMAR Host Wave 4 — Remove One More High-Value Host Write Slice and Reassess Pivot Readiness

Task Type:
IMPLEMENTATION — EVIDENCE-SELECTED HOST DEBT REDUCTION

0. Architect Intent

HOST-W3 is accepted.

Verified state:

StoreAppearanceSettings direct Host write removed

Host composer no longer owns CatalogDbContext/SaveChanges for that slice

CQRS path is now Host → ISender → Application Handler → Directory → Infrastructure

Host-Recovery-State = CONTINUE_HOST

Architecture-Priority = HOST

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

Orders-Frontend-Readiness = UNCHANGED

residual Host write candidates include Quantity / UoM / Shipping / Seller / ProductWorkspace / Order-adjacent

Checkout shared-ACID remains deferred

StoreAppearance ownership in Catalog is still transitional and MUST NOT be normalized by assumption

Primary objectives:

refresh remaining Host direct-write/business-decision inventory from live repo state

select exactly ONE coherent, high-value, low-to-moderate-risk Host write slice

move it behind CQRS/Application ownership

shrink Host-write baseline

preserve behavior/transaction semantics

reassess whether another Host wave still outranks Checkout design / Contracts / FE godfile

No broad Host rewrite.
No ownership invention.
No Checkout redesign.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-HOST-W3/recovery-sot.md

docs/evidence/TB-TMAR-HOST-W3/host-write-inventory.md

docs/evidence/TB-TMAR-HOST-W3/host-recovery-state.md

docs/evidence/TB-TMAR-HOST-W3/architecture-priority.md

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
10070277fe05f9defef25eb6f5d3620cd103be14

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
docs/evidence/TB-TMAR-HOST-W4/recovery-start.md

2. Refresh Remaining Host Write Inventory

Re-scan live Host production code.

Search at minimum for:

SaveChanges / SaveChangesAsync

Add / AddAsync / Update / Remove

BeginTransaction / TransactionScope

module DbContext injections

repository write abstractions

direct domain state mutation from Host

business decisions around quantity, UoM, shipping, seller, product workspace, order state, pricing, inventory, campaign, payment

Classify:

DIRECT_DB_WRITE

DIRECT_TRANSACTION

BUSINESS_DECISION

PRESENTATION_ONLY

FALSE_POSITIVE

NEEDS_DESIGN

Update:
docs/evidence/TB-TMAR-HOST-W4/host-write-inventory.md
docs/evidence/TB-TMAR-HOST-W4/host-write-inventory.json

Do not rely on stale W3 inventory alone.

3. Select Exactly ONE Host Slice

Selection priority:

clear bounded-context owner

direct Host write or transaction

behavior easy to characterize

no cross-context consistency redesign required

meaningful Host baseline shrink

no ownership ambiguity

no giant god-file expansion

Preferred classes of slices if evidence supports:

Quantity / UoM

Shipping configuration

Seller administration

narrow ProductWorkspace write

Avoid:

Order-adjacent workflow if it touches lifecycle/checkout/payment/fulfillment consistency

anything requiring Saga/process-manager redesign

anything whose Domain ownership is still unresolved

Before coding document:

exact Host file/member

current DbContext/repository usage

current business decisions

module owner

transaction semantics

callers

target Command/Handler

target Application abstraction/Directory

expected baseline shrink

Evidence:
docs/evidence/TB-TMAR-HOST-W4/slice-selection.md

4. Characterization Before Migration

Add/confirm focused tests for selected slice before changing structure.

Cover as applicable:

request/input mapping

validation

authorization/store/tenant boundary

state changes

transaction behavior

error semantics

idempotency if relevant

cache invalidation

response shape/status

No broad snapshots.
No fake workaround behavior.

Evidence:
docs/evidence/TB-TMAR-HOST-W4/characterization-tests.md

5. Target CQRS Shape

Required target:

Host endpoint/composer
→ ISender
→ Command
→ Handler in owning module Application
→ module-owned abstraction/Directory
→ Infrastructure persistence

Rules:

IRequestHandler business logic in Application only

EF/DbContext/transaction implementation in Infrastructure

Host cannot retain hidden write side-effects

Host cannot remain the source of business truth

existing Directory may remain as strangler seam if validated

do not create pass-through handler back into Host

Evidence:
docs/evidence/TB-TMAR-HOST-W4/target-shape.md

6. Remove Host Persistence

For selected slice:

remove Host DbContext/repository write dependency

remove Host SaveChanges/transaction ownership

move persistence to module Infrastructure

move orchestration/business decisions to Application/Domain owner

preserve existing behavior

If selected slice reveals wrong bounded-context ownership:

do NOT silently cement the wrong owner

classify as NEEDS_DESIGN and select another safe slice

report the deferred ownership issue

Evidence:
docs/evidence/TB-TMAR-HOST-W4/host-write-removal.md

7. Business Decision Ownership

Move business decisions out of Host where present.

Examples:

allowed status transitions

quantity/UoM rules

shipping eligibility/configuration truth

seller activation/state

product-workspace save semantics

price/inventory/offer truth

Host may retain only:

HTTP parsing

auth/session edge

route binding

response formatting

presentation composition

Evidence:
docs/evidence/TB-TMAR-HOST-W4/business-decision-removal.md

8. Transaction Semantics

Preserve exact current semantics.

If module-local transaction:

move transaction ownership to Infrastructure

preserve isolation/order/rollback behavior

If cross-context transaction:

do NOT redesign in this task

reject that slice and choose another safe slice

ARCH-TX-001 remains active.

Evidence:
docs/evidence/TB-TMAR-HOST-W4/transaction-semantics.md

9. Cache Compliance

If selected slice touches cache:

use ICache / ICacheKeyBuilder / ICacheInvalidator

no direct IMemoryCache

preserve invalidation behavior

no Redis install

no polling/magic TTL workaround

Evidence:
docs/evidence/TB-TMAR-HOST-W4/cache-compliance.md

10. Foundation Compliance

New code must obey:

MediatR 12.5.0

Application handlers

FluentValidation where appropriate

IClock

IIdGenerator

semantic errors

no localized user-facing Domain/Application messages

unlimited locale safety

Do not mass-migrate unrelated code.

Evidence:
docs/evidence/TB-TMAR-HOST-W4/foundation-compliance.md

11. Host Baseline Shrink

Required:

selected Host write entry removed from baseline

no new Host write entries

no baseline widening

no wildcard suppression

Also verify:

App→App baseline does not grow

Infra→foreign App baseline does not grow

Domain→foreign Domain remains clean

Infra→foreign Domain remains clean

cross-context transaction baseline does not grow

frontend guards remain untouched/green

source-size baseline does not grow

Evidence:
docs/evidence/TB-TMAR-HOST-W4/architecture-guards.md

12. Source-Size / God-File Safety

No new hand-written source >800 LOC.

If touching oversized legacy file:

shrink-only

no broad decomposition

keep diff narrow

do not create a new giant Directory/Handler

If selected migration would push a target file above 800:
split by use-case/capability instead of growing it.

Evidence:
docs/evidence/TB-TMAR-HOST-W4/source-size-compliance.md

13. Tests

Required:

selected slice characterization tests

affected module tests

Host endpoint/integration tests if present

ArchitectureBoundaryTests

TMAR foundation tests

Host-write guard

source-size guard

transaction guard

Contracts guards

No broad new test-project creation.

Evidence:
docs/evidence/TB-TMAR-HOST-W4/tests.md

14. Orders Frontend Readiness

Reassess only if this Host slice materially affects order/admin workflow boundary.

Return exactly:
Orders-Frontend-Readiness: IMPROVED
or
Orders-Frontend-Readiness: UNCHANGED

Do NOT migrate frontend Orders.

Evidence:
docs/evidence/TB-TMAR-HOST-W4/orders-frontend-readiness.md

15. Host Recovery State

Return exactly one:
Host-Recovery-State: CONTINUE_HOST
Host-Recovery-State: READY_TO_PIVOT

READY_TO_PIVOT requires:

remaining Host direct-write debt is lower value, blocked by ownership/consistency design, or safe to defer

new Host writes are frozen

CQRS path is established for new features

continuing Host cleanup is no longer highest-value architecture work

Evidence:
docs/evidence/TB-TMAR-HOST-W4/host-recovery-state.md

16. Architecture Priority

Return exactly one:
Architecture-Priority: HOST
Architecture-Priority: CHECKOUT_DESIGN
Architecture-Priority: CONTRACTS
Architecture-Priority: FE_GODFILE

Use evidence, not sequence convenience.

Guidance:

HOST if another coherent direct-write slice clearly remains highest value

CHECKOUT_DESIGN if shared-ACID migration risk now dominates

CONTRACTS if remaining module boundary leakage is now more important

FE_GODFILE only if characterization readiness is actually READY

Evidence:
docs/evidence/TB-TMAR-HOST-W4/architecture-priority.md

17. Next Task Decision

Choose automatically:

A. TB-TMAR-HOST-W5
if CONTINUE_HOST and HOST remains highest value.

B. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if CHECKOUT_DESIGN is highest value.

C. TB-TMAR-CONTRACTS-W7
if CONTRACTS is highest value.

D. TB-TMAR-FE-GODFILE-W1
only if FE_GODFILE is highest value and readiness is READY.

Do not ask the user.

18. Product Safety

No product behavior change.

Do NOT:

change routes

change public API contracts

alter pricing/inventory/seller semantics

alter UI/storefront behavior

redesign checkout

implement Saga

change order lifecycle semantics

19. Recovery Updates

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md only if durable lock changes

docs/architecture/TOOBA-CAPABILITY-MAP.md only if material ownership evidence changes

docs/evidence/TB-TMAR-HOST-W4/recovery-sot.md

Record:

selected Host slice

Host baseline before/after

Orders frontend readiness

Host-Recovery-State

Architecture-Priority

next task

20. Acceptance Criteria

PASS only if:

live Host inventory refreshed

exactly one coherent Host write slice migrated

behavior characterized before migration

Host persistence removed for selected slice

business handler in Application

persistence/transaction in Infrastructure

Host baseline shrinks

no bad dependency expansion

no new cross-context transaction

no product behavior change

tests pass

source-size guards green

Orders frontend readiness assessed

Host recovery state assessed

Architecture-Priority returned

user work preserved

next task chosen automatically

canonical Result delivered

Worker stops completely

21. Result Contract

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
Cache-Compliance
Foundation-Compliance
Architecture-Guards
Source-Size-Compliance
Tests
Orders-Frontend-Readiness
Host-Recovery-State
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