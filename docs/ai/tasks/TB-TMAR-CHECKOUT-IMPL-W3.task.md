PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-CHECKOUT-IMPL-W3

Parent-Task:
TB-TMAR-CHECKOUT-IMPL-W2

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
TMAR Checkout Implementation Wave 3 — Extract Cart Conversion Contract Seam and Reduce Order Hub Coupling

Task Type:
IMPLEMENTATION — CONTROLLED BOUNDARY EXTRACTION / BEHAVIOR-PRESERVING

0. Architect Intent

CHECKOUT-IMPL-W2 is accepted.

Verified state:

Order-owned in-process CheckoutProcessManager is active

Inventory reservation is behind Tooba.Inventory.Contracts

Order.Application → Inventory.Application is removed

shared TransactionScope across Order + Inventory + Cart remains intact

no async Saga/messaging/compensation runtime is active

Checkout-Implementation-W3-Readiness = READY

W3-Candidate = Cart conversion contract seam

Orders-Frontend-Readiness = STILL_WAITING_FOR_BACKEND_W3

Architecture-Priority = CHECKOUT_IMPLEMENTATION

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

residual concern: Order.Infrastructure → Inventory.Application remains for Cancel/Restore/PaymentBridge; this is NOT the primary W3 scope unless it directly blocks checkout W3

Primary objectives:

extract Cart conversion behind a stable Cart.Contracts seam

reduce Order/Application dependence on Cart.Application

keep current shared TransactionScope and synchronous atomicity intact

keep Process Manager as orchestration owner

preserve idempotency/correlation

do NOT activate async messaging

do NOT execute compensation runtime

reassess whether backend Checkout boundary is now sufficient to unblock frontend Orders boundary work

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/recovery-sot.md

docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/w3-candidate.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/proposed-consistency-model.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/command-event-boundaries.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/checkout-state-machine.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/order-hub-decomposition-plan.md

Verify:

branch main

exact HEAD SHA

exact origin/main SHA

HEAD == origin/main

18ca10c9 ancestor

known/clean worktree state

user work preserved

Expected previous accepted tip:
02fc2ee7ed943b80a4572c6f9cad3eea74eeb0d0

If conflicting tracked user work exists:
STOP with RECOVERY_CONFLICT.

Never use destructive Git operations.
Never use broad git add ..

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/recovery-start.md

2. Verify W2 State Before W3

Confirm from live code:

CheckoutProcessManager is current orchestration owner

Inventory contract seam is active

shared TransactionScope still exists

process-state/idempotency remains active

no async workflow transport exists

Cart conversion is still consumed through Cart.Application or another foreign Application boundary

If accepted assumptions no longer match current code:
STOP with DESIGN_DRIFT.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/w2-verification.md

3. Reconstruct Exact Cart Conversion Usage

Inspect exactly how checkout/order currently invokes Cart conversion.

Document:

current interface/type(s)

exact method(s)

caller(s)

transaction participation

write effects

idempotency assumptions

result shape

error semantics

whether conversion is one-way or has a restoration/recovery counterpart

any Cart.Application dependencies unrelated to checkout

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/cart-conversion-usage.md

4. Extract Minimal Cart Conversion Contract

Create or extend:
Tooba.Cart.Contracts

Target stable boundary should expose only what checkout coordination requires.

Likely conceptual shape:

ICartConversionPort

conversion request

conversion result/reference

correlation/process identity where needed

Derive exact types from current code.

Rules:

no Cart Domain entity leakage

no Cart Application implementation types

no DbContext/repository leakage

no cart business logic duplicated in Order

Cart remains authority for conversion semantics

future transport-adapter friendly

do not create a broad generic Cart service facade

If a recovery/restore contract is clearly required by accepted future compensation design, it may be DEFINED minimally but MUST NOT be activated as compensation runtime in W3.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/cart-contract.md

5. Remove Order.Application → Cart.Application If Safe

Preferred target:
Order.Application → Cart.Contracts

Not:
Order.Application → Cart.Application

If all checkout-specific Order usages can move to Cart.Contracts:

remove ProjectReference

shrink App→App baseline exactly

If non-checkout Order usages still require Cart.Application:

do not fake removal

migrate checkout-specific consumption only

document residual edge precisely

baseline must not widen

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/order-cart-dependency.md

6. Cart Adapter / Ownership

Cart-owned implementation remains in Cart module.

Requirements:

Cart owns conversion semantics

Cart Infrastructure owns persistence

Order does not write Cart tables

no shared repository abstraction

no foreign DbContext

no Host involvement

Document mapping:

in-process adapter now

future transport adapter later

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/cart-adapter.md

7. Integrate Cart Seam Into Process Manager

Update CheckoutProcessManager to use Cart.Contracts.

Preserve exact workflow ordering and current synchronous semantics.

The Process Manager:

coordinates

transitions process state

passes stable process/correlation identity

does NOT implement Cart business rules

Do not move conversion logic into Order.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/process-manager-cart-integration.md

8. Shared TransactionScope Must Remain

CRITICAL:
the current shared TransactionScope remains active in W3.

Required:

Order + Inventory + Cart still participate in same effective atomic boundary

failure in Cart conversion rolls back Order/Inventory effects as before

process state must not falsely mark completion on rollback

contract/adapters must not cause nested independent commits that weaken atomicity

Add explicit characterization.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/transaction-preservation.md

9. State Transition Discipline

Add only W3 transition/milestone detail required by Cart conversion.

Do not prebuild async future state machine.

Document:

process state before Cart conversion

after Cart conversion

completion transition

rollback behavior

duplicate/retry behavior

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/state-transitions.md

10. Idempotency / Correlation Preservation

Verify:

duplicate submit cannot convert same Cart twice into duplicate business effects

ProcessId remains stable

Cart conversion receives stable correlation/idempotency identity where needed

retries do not generate new workflow identity

completed workflow behaves deterministically

No cache-only or in-memory authority.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/idempotency-correlation.md

11. Failure Behavior

Characterize and preserve:

Cart conversion fails after Inventory reservation and Order write inside current TX

Cart already converted / duplicate request

process-state write failure

adapter failure

transient exception

Do NOT execute async compensation.
Current rollback remains correctness mechanism in W3.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/failure-behavior.md

12. Messaging Scope

No distributed workflow messaging in W3.

Do NOT activate:

ConvertCart async command

CartConverted event as workflow progression

compensation messages

timeout/retry queues

background Process Manager runner

Future contracts may remain documented only.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/messaging-scope.md

13. Residual Order Hub Dependency Audit

After Inventory + Cart seam extraction, refresh current Order.Application foreign Application edges.

Classify each residual edge:

CHECKOUT_CRITICAL

NON_CHECKOUT_ORDER_WORKFLOW

LOW_RISK_CONTRACT_CANDIDATE

NEEDS_PROCESS_DESIGN

TEMPORARY_ACCEPTABLE

Pay special attention to Promotion and any remaining dependencies.

Do NOT automatically migrate another edge in this task.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/order-hub-residuals.md

14. Residual Order.Infrastructure → Inventory.Application

Audit only.

Known residual from W2:
Order.Infrastructure → Inventory.Application
for Cancel/Restore/PaymentBridge.

Document:

exact types/methods

whether checkout-related

whether compensation-related

whether it should move to Inventory.Contracts in a later task

whether it blocks future service extraction

Do NOT broaden W3 unless a minimal compile-safe change is strictly required.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/order-infra-inventory-residual.md

15. Layering / Contracts Guards

Verify:

Process Manager in Order.Application

Cart contract in Cart.Contracts

adapter owned by Cart

no new foreign Application dependency

no Domain entity leakage into Contracts

no business handler in Infrastructure

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/layering.md

16. Source-Size / God-File Safety

No new hand-written file >800 LOC.

CheckoutDirectory should shrink or remain stable.
Process Manager must not become a new god class.

Record before/after LOC for:

CheckoutDirectory

CheckoutProcessManager

Cart adapter/contract files

touched Order giant files

No broad decomposition outside this seam.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/source-size-compliance.md

17. Architecture Guards

Add/update guards to enforce:

Order checkout path uses Cart.Contracts

no new Order.Application → Cart.Application

no new App→App edge

Domain→foreign Domain remains clean

Infra→foreign Domain remains clean

shared TransactionScope remains for W3

no async checkout orchestration activated

ARCH-CHECKOUT-001…005 remain active

If full Order→Cart.Application edge is removed:
shrink App→App baseline.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/architecture-guards.md

18. Tests

Required:

Cart conversion contract characterization

Process Manager successful checkout

duplicate submit

Cart conversion failure rollback

Inventory reservation + Cart conversion atomicity

process-state rollback correctness

existing Checkout/Order/Cart/Inventory focused tests

ArchitectureBoundaryTests

Contracts cleanliness

TMAR foundation

source-size guard

checkout architecture guards

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/tests.md

19. Data Migration

Prefer no schema migration in W3.

If any additive schema change is truly required:

Order-owned

additive

no destructive backfill

compatibility documented

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/data-migration.md

20. W4 Readiness

Assess:
Checkout-Implementation-W4-Readiness: READY
or
Checkout-Implementation-W4-Readiness: DEFER

READY requires:

Process Manager coordinates through Inventory.Contracts + Cart.Contracts

shared TransactionScope still preserves behavior

process state/idempotency intact

residual next seam is clear

no new boundary debt

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/w4-readiness.md

21. W4 Candidate

Do NOT implement W4 here.

Choose safest next step from evidence, likely one of:

Promotion contract seam

Order.Infrastructure → Inventory.Contracts cleanup for Cancel/Restore

Inbox/idempotent-consumer primitive

preparation to separate one participant local transaction while Process Manager remains in-process

Return:
W4-Candidate: <name>

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/w4-candidate.md

22. Orders Frontend Readiness

Reassess after W3.

Return:
Orders-Frontend-Readiness: READY_FOR_FE_BOUNDARY_WORK
or
Orders-Frontend-Readiness: STILL_WAITING_FOR_BACKEND_W4

READY requires:

frontend Order admin no longer needs hidden Host/business workflow knowledge

mutation boundaries can target stable Application/Contracts paths

workflow/process state semantics are clear enough for admin UI

shared TransactionScope remains an implementation detail, not an API/UI concern

Do NOT modify frontend Orders.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/orders-frontend-readiness.md

23. Architecture Priority

Return exactly one:
Architecture-Priority: CHECKOUT_IMPLEMENTATION
Architecture-Priority: CONTRACTS
Architecture-Priority: FE_ORDERS
Architecture-Priority: FE_GODFILE

Guidance:

CHECKOUT_IMPLEMENTATION if W4 readiness is READY and next backend seam clearly outranks frontend

FE_ORDERS if frontend boundary work is now safely unblocked and backend W4 can pause without architectural risk

CONTRACTS if residual prerequisite coupling blocks W4

FE_GODFILE only if actual readiness is READY

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/architecture-priority.md

24. Next Task Decision

Choose automatically:

A. TB-TMAR-CHECKOUT-IMPL-W4
if W4 readiness = READY and CHECKOUT_IMPLEMENTATION remains highest value.

B. TB-TMAR-CONTRACTS-W7
if prerequisite Contracts debt is highest value.

C. TB-TMAR-FE-ORDERS-W1
if Orders frontend is READY_FOR_FE_BOUNDARY_WORK and FE_ORDERS is highest value.

D. TB-TMAR-FE-GODFILE-W1
only if FE_GODFILE is highest value and readiness is READY.

Do not ask the user.

25. Product Safety

No user-visible behavior change.

Do NOT:

remove shared TransactionScope

split participant commits

activate async Saga workflow

change cart/order/inventory semantics

change public checkout API

change frontend

execute compensation runtime

26. Recovery Updates

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md only if durable W3 guard is added

docs/architecture/TOOBA-CAPABILITY-MAP.md only if material ownership facts change

docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/recovery-sot.md

Record:

Cart contract seam

Order→Cart dependency status

Process Manager integration

residual Order hub edges

W4 readiness

W4 candidate

Orders frontend readiness

next task

27. Acceptance Criteria

PASS only if:

W2 state verified intact

Cart conversion contract seam extracted

Process Manager uses Cart.Contracts

Order.Application→Cart.Application removed if safely possible, otherwise residual documented without fake cleanup

shared TransactionScope remains intact

rollback semantics preserved

idempotency/correlation preserved

no async workflow messaging activated

no compensation runtime activated

no new bad dependency introduced

source-size/layering guards pass

tests pass

residual Order hub audited

W4 readiness assessed

W4 candidate selected

Orders frontend readiness assessed

user work preserved

canonical Result delivered

Worker stops completely

28. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
W2-Verification
Cart-Conversion-Usage
Cart-Contract
Order-Cart-Dependency
Cart-Adapter
Process-Manager-Cart-Integration
Transaction-Preservation
State-Transitions
Idempotency-Correlation
Failure-Behavior
Messaging-Scope
Order-Hub-Residuals
Order-Infra-Inventory-Residual
Layering
Architecture-Guards
Source-Size-Compliance
Data-Migration
Tests
Checkout-Implementation-W4-Readiness
W4-Candidate
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