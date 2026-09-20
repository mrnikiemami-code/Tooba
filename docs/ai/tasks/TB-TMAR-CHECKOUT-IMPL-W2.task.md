PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-CHECKOUT-IMPL-W2

Parent-Task:
TB-TMAR-CHECKOUT-IMPL-W1

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
TMAR Checkout Implementation Wave 2 — Activate In-Process Process Manager and Extract Inventory Reservation Contract Seam

Task Type:
IMPLEMENTATION — CONTROLLED WORKFLOW ACTIVATION / BEHAVIOR-PRESERVING

0. Architect Intent

CHECKOUT-IMPL-W1 is accepted.

Verified state:

durable Order-owned checkout process state exists

submission idempotency exists

CheckoutSubmitExecutor integrates process-state foundation

shared TransactionScope across Order + Inventory + Cart remains active

no Saga/Process Manager runtime was activated in W1

Checkout-Implementation-W2-Readiness = READY

W2-Candidate = Order-owned Process Manager activation (in-process synchronous) + explicit Inventory reservation contract seam

Orders-Frontend-Readiness = STILL_WAITING_FOR_BACKEND_W2

Architecture-Priority = CHECKOUT_IMPLEMENTATION

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

Primary objectives:

activate an Order-owned in-process synchronous Process Manager/coordinator

move checkout orchestration ownership out of CheckoutDirectory/monolithic submit flow into explicit process orchestration

extract Inventory reservation behind a stable Contracts boundary

preserve the existing shared TransactionScope and current synchronous atomicity

preserve exact call ordering and product behavior

make the future async/distributed seam explicit without using it yet

shrink Order/Application foreign Application coupling if safely possible

do NOT remove TransactionScope

do NOT send async workflow messages

do NOT implement compensation runtime yet

This wave is about orchestration ownership and contract seams, not distribution.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/recovery-sot.md

docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/w2-candidate.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/proposed-consistency-model.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/command-event-boundaries.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/checkout-state-machine.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/failure-matrix.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/compensation-model.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/order-hub-decomposition-plan.md

Verify:

branch main

exact HEAD SHA

exact origin/main SHA

HEAD == origin/main

18ca10c9 ancestor

known/clean worktree

user work preserved

Expected previous accepted tip:
9c1ebd10f0941e809f8f1cd5313d30e579fea68b

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
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/recovery-start.md

2. Verify Current W1 Integration Before W2

Before changing orchestration, verify live code still matches W1 evidence:

checkout_processes persistence exists

process-state model/mapping/migration is active

duplicate-submit protection exists

CheckoutSubmitExecutor is current integration seam

shared TransactionScope still wraps current Order + Inventory + Cart behavior

no Process Manager runtime already exists

no new App→App edge appeared after W1

If drift exists:
STOP with DESIGN_DRIFT.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/w1-verification.md

3. Define the In-Process Process Manager Boundary

Introduce an Order-owned Application-level coordinator/process manager.

Naming must reflect actual domain intent, e.g.:

CheckoutProcessManager

CheckoutWorkflowCoordinator
or equivalent evidence-backed naming.

Do NOT use generic SagaManager.

Responsibilities:

load/create durable checkout process state

coordinate the existing synchronous checkout steps

transition process state explicitly

invoke participant boundaries in the accepted order

preserve current TransactionScope participation

produce deterministic outcome for duplicate submissions

Non-responsibilities:

no EF/DbContext access directly in Application

no background execution

no message polling

no timer loop

no distributed transport

no compensation execution yet

no direct Host dependency

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/process-manager-boundary.md

4. Preserve Current Shared TransactionScope

CRITICAL:

The current shared TransactionScope MUST remain active in W2.

Required:

same effective participants: Order + Inventory + Cart

same atomic rollback semantics

same relative call ordering unless a provably equivalent internal wrapper is introduced

same user-visible success/failure behavior

The new Process Manager may coordinate inside the current synchronous transaction, but MUST NOT split commits.

Add explicit characterization proving:

Inventory reservation succeeds, Order write fails → all current transactional effects roll back as before

Order succeeds, Cart conversion fails → all current transactional effects roll back as before

completed process is not falsely persisted when current checkout transaction fails

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/transaction-preservation.md

5. Extract Inventory Reservation Contract

Inspect current Order.Application → Inventory.Application dependency and exact Inventory types used for checkout reservation.

Create/extend:
Tooba.Inventory.Contracts

Target stable boundary should expose only what checkout/order coordination needs.

Potential contract shape, derive from code:

IInventoryReservationPort

reservation request DTO

reservation result/reference DTO

release/compensation contract ONLY if required by current accepted design and safe to define now

Rules:

no Inventory Domain entities

no Inventory Application implementation types

no DbContext/repository leakage

no stock calculation logic duplicated in Order

Inventory remains authority for reservation semantics

contract must be future transport-adapter friendly

do not create overly generic inventory service facade

If existing Inventory.Contracts already exists, extend minimally.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/inventory-contract.md

6. Remove Order.Application → Inventory.Application If Safe

After extracting the checkout reservation boundary:

Preferred:
Order.Application → Inventory.Contracts

Not:
Order.Application → Inventory.Application

If all current Order checkout usages can safely move to Contracts:

remove ProjectReference

shrink App→App baseline exactly

If non-checkout Order usages still require Inventory.Application:

do NOT fake removal

migrate only checkout-specific consumption

document residual edge precisely

do not widen baseline

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/order-inventory-dependency.md

7. Inventory Adapter / Implementation Ownership

The Inventory participant implementation remains Inventory-owned.

Allowed:

adapter/implementation in Inventory.Application or Infrastructure as appropriate to current pattern

local persistence in Inventory.Infrastructure

Required:

no Order-owned Inventory persistence

no shared repository abstraction crossing boundaries

no Host involvement

no direct foreign DbContext

Document how today's in-process implementation maps later to:

in-process adapter now

HTTP/gRPC/message adapter later

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/inventory-adapter.md

8. Move Orchestration From CheckoutDirectory to Process Manager

Refactor narrowly so CheckoutDirectory/Submit path no longer owns all workflow sequencing details.

Target:

CheckoutDirectory / Submit handler delegates to Process Manager

Process Manager coordinates current steps

participant-specific operations stay with participant modules

process state transitions happen in coordinator

existing TransactionScope remains where semantically safest

Do NOT:

move participant business logic into Process Manager

create a giant coordinator

duplicate pricing/promotion/inventory rules

introduce generic workflow engine

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/orchestration-move.md

9. Process State Transitions

Implement only the W2 transitions necessary to reflect the current synchronous path plus explicit Inventory reservation milestone.

Do not prebuild future async states.

Required:

state transition before/after Inventory reservation as accepted design supports

invalid transition guards

terminal-state behavior

duplicate submit semantics remain deterministic

Document:

W1 states retained

W2 states newly activated

future states still deferred

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/state-transitions.md

10. Failure Behavior

Preserve current external behavior.

Internally, Process Manager should classify failures enough to maintain durable workflow truth.

At minimum:

Inventory reservation failure

Order creation failure after reservation within current shared TX

Cart conversion failure

duplicate submission

process-state persistence failure

Do NOT execute compensation outside current rollback semantics yet.

No async compensation.
No ReleaseInventory call as a distributed recovery path yet unless it already exists in current synchronous flow.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/failure-behavior.md

11. Idempotency Preservation

W1 idempotency must remain authoritative.

Verify:

duplicate submit does not execute Inventory reservation twice in a way that can create duplicate business effects

duplicate completed submit returns deterministic current-compatible result

retries before terminal completion follow accepted W1 semantics

no cache-only/in-memory dedupe introduced

If Inventory reservation itself needs an idempotency/correlation token at the contract boundary:
add it in a stable way now, but do not create distributed Inbox runtime yet.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/idempotency.md

12. Correlation / Process Identity Propagation

Propagate process/correlation identity through the Inventory reservation contract where appropriate.

Rules:

one workflow ProcessId

correlation identity stable across participant calls

no random new correlation per retry

no logging-only identity masquerading as business idempotency

Document exact identifiers:

process id

checkout submission key

inventory reservation id/reference

order id

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/correlation.md

13. Outbox / Messaging Scope

Do NOT activate distributed workflow messaging.

Allowed:

internal/domain event only if it already fits current local transaction semantics and has no externally visible workflow effect

Not allowed:

ReserveInventory async command

InventoryReserved integration event driving progression

compensation messages

timeout messages

process runner queue

transport polling

Keep future message contracts documented only.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/messaging-scope.md

14. Layering / CQRS Compliance

Target:

Process Manager in Order.Application

participant contract in Inventory.Contracts

persistence implementation in correct Infrastructure

Host remains transport only

business handlers remain Application

No business IRequestHandler in Infrastructure.

No new foreign Application dependencies.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/layering.md

15. Source-Size / God-File Safety

This is important because Checkout/Order code is already high-risk.

No new hand-written file >800 LOC.

Do not grow existing CheckoutDirectory god-file.
Prefer to SHRINK it.

Process Manager must stay cohesive, not become a new god class.

If orchestration extraction would make any file >800:
split by explicit responsibility.

Record before/after LOC of:

CheckoutDirectory

new Process Manager

any touched Order giant

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/source-size-compliance.md

16. Architecture Guards

Update/add guards for:

Process Manager owned by Order.Application

no Process Manager in Host/Infrastructure

Order checkout path uses Inventory.Contracts

no new Order.Application → Inventory.Application edge

no new Domain→foreign Domain

no new Infra→foreign Domain

shared TransactionScope still present in W2

no async checkout orchestration transport activated

ARCH-CHECKOUT-001…005 remain active

If Order→Inventory.Application can be fully removed:
shrink App→App baseline.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/architecture-guards.md

17. Tests

Required:

Process Manager:

current successful checkout behavior

duplicate submit

invalid process transition

Inventory reservation failure

Order failure rollback

Cart conversion failure rollback

Inventory contract:

contract characterization

adapter behavior

correlation/idempotency propagation

Architecture:

ArchitectureBoundaryTests

TMAR foundation

Contracts cleanliness

source-size

transaction preservation

checkout architecture guards

Existing focused Checkout/Order/Cart/Inventory tests must remain green.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/tests.md

18. Data Migration

Prefer NO new schema migration in W2 unless a minimal additive field is truly required by accepted design.

If schema change is required:

additive only

Order-owned

no destructive backfill

compatibility documented

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/data-migration.md

19. W3 Readiness

After W2 assess:

Checkout-Implementation-W3-Readiness: READY
or
Checkout-Implementation-W3-Readiness: DEFER

READY requires:

Process Manager owns orchestration

Inventory seam is explicit

TransactionScope still preserves behavior

idempotency intact

no new boundary debt

next participant seam is clear

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/w3-readiness.md

20. W3 Candidate

Do NOT implement W3 here.

Choose safest next stage from evidence, such as:

Cart conversion contract seam

Process Manager activation with local participant transaction separation preparation

Inbox/idempotent-consumer primitive

Promotion/other remaining Order hub contract prerequisite

Return:
W3-Candidate: <name>

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/w3-candidate.md

21. Orders Frontend Readiness

Reassess after W2.

Return exactly:
Orders-Frontend-Readiness: READY_FOR_FE_BOUNDARY_WORK
or
Orders-Frontend-Readiness: STILL_WAITING_FOR_BACKEND_W3

READY requires:

backend Order admin mutations can target stable Application/Contracts boundaries

frontend does not need to understand shared TransactionScope internals

workflow state semantics are explicit enough for admin UI

Do NOT modify frontend Orders.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/orders-frontend-readiness.md

22. Architecture Priority

Return exactly one:
Architecture-Priority: CHECKOUT_IMPLEMENTATION
Architecture-Priority: CONTRACTS
Architecture-Priority: FE_ORDERS
Architecture-Priority: FE_GODFILE

Prefer CHECKOUT_IMPLEMENTATION if W3 readiness is READY and no prerequisite boundary blocks it.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/architecture-priority.md

23. Next Task Decision

Choose automatically:

A. TB-TMAR-CHECKOUT-IMPL-W3
if W3 readiness = READY and CHECKOUT_IMPLEMENTATION is highest value.

B. TB-TMAR-CONTRACTS-W7
if prerequisite Contracts debt blocks W3.

C. TB-TMAR-FE-ORDERS-W1
only if Orders frontend is READY_FOR_FE_BOUNDARY_WORK and FE_ORDERS now outranks backend W3.

D. TB-TMAR-FE-GODFILE-W1
only if FE_GODFILE is highest value and readiness is READY.

Do not ask the user.

24. Product Safety

No user-visible behavior change.

Do NOT:

remove shared TransactionScope

split participant commits

activate async Saga workflow

change pricing/inventory/promotion/order semantics

change public API

change frontend

execute compensation runtime

25. Recovery Updates

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md only if durable W2 guard is added

docs/architecture/TOOBA-CAPABILITY-MAP.md only if material ownership facts change

docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/recovery-sot.md

Record:

Process Manager activation

Inventory contract seam

Order→Inventory dependency status

Transaction preservation

W3 readiness

W3 candidate

Orders frontend readiness

next task

26. Acceptance Criteria

PASS only if:

W1 state/idempotency verified intact

Order-owned in-process Process Manager activated

current shared TransactionScope remains intact

Inventory reservation boundary extracted to Inventory.Contracts

Order checkout orchestration no longer directly depends on Inventory.Application if safely removable

no participant business logic moved into Process Manager

no async workflow messaging activated

current checkout behavior preserved

rollback semantics preserved

idempotency/correlation preserved

CheckoutDirectory shrinks or at minimum does not grow

no new bad dependency introduced

source-size/layering guards pass

tests pass

W3 readiness assessed

W3 candidate selected

Orders frontend readiness assessed

user work preserved

canonical Result delivered

Worker stops completely

27. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
W1-Verification
Process-Manager-Boundary
Transaction-Preservation
Inventory-Contract
Order-Inventory-Dependency
Inventory-Adapter
Orchestration-Move
State-Transitions
Failure-Behavior
Idempotency
Correlation
Messaging-Scope
Layering
Architecture-Guards
Source-Size-Compliance
Data-Migration
Tests
Checkout-Implementation-W3-Readiness
W3-Candidate
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