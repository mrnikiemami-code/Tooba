PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-CHECKOUT-IMPL-W1

Parent-Task:
TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN

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
TMAR Checkout Implementation Wave 1 — Introduce Durable Workflow State and Idempotency Primitives Without Removing Shared TransactionScope

Task Type:
IMPLEMENTATION — FOUNDATION ONLY / BEHAVIOR-PRESERVING

0. Architect Intent

CHECKOUT-CONSISTENCY-DESIGN is accepted.

Verified state:

Proposed consistency model = Order-owned Process Manager

Checkout-Point-Of-No-Return = MULTI_STAGE

Checkout-Consistency-Implementation-Readiness = READY_FOR_IMPLEMENTATION_W1

Architecture-Priority = CHECKOUT_IMPLEMENTATION

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

current CheckoutDirectory SubmitAsync still uses shared TransactionScope across Order + Inventory + Cart

ARCH-CHECKOUT-001…005 are active

frontend Orders is READY_AFTER_DESIGN, but frontend Orders MUST NOT be migrated in this task

This is Stage 1 foundation only.

Primary objectives:

introduce durable checkout workflow/process state owned by Order

introduce idempotency primitives for checkout submission

add correlation/process identity

persist workflow state in the SAME existing monolith/database environment

keep current TransactionScope and current business behavior intact

prove crash/retry-safe process identity without activating distributed orchestration

do NOT implement Saga/Process Manager execution yet

do NOT remove the shared transaction yet

This task is deliberately conservative.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/recovery-sot.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/proposed-consistency-model.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/checkout-state-machine.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/idempotency-model.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/failure-matrix.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/checkout-migration-plan.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/command-event-boundaries.md

Verify:

branch main

exact HEAD SHA

exact origin/main SHA

HEAD == origin/main

18ca10c9 ancestor

known/clean worktree

user work preserved

Expected previous accepted tip:
e27fd0283154a59c4756a74d1240427148f00a7b

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
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/recovery-start.md

2. Re-Verify Design-to-Code Mapping Before Implementation

Before changing code, map the accepted design artifacts to CURRENT code symbols.

Confirm:

current CheckoutDirectory entry point

current Order write path

current Cart conversion path

current Inventory reservation path

current TransactionScope boundary

existing OrderDbContext/schema/migrations

existing Outbox infrastructure in Order

available IClock / IIdGenerator

exact accepted workflow states from design

If any accepted design assumption no longer matches current code:
STOP implementation and return DESIGN_DRIFT.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/design-code-map.md

3. Order-Owned Checkout Process State

Implement a durable process-state aggregate/entity/value model owned by Order.

Name should reflect checkout workflow/process, not generic Order status.

Required properties should be derived from accepted design and may include:

ProcessId

Checkout/Submission idempotency key

CartId / Cart reference

OrderId if/when assigned

Store/Tenant context as actually required

Current process state

CorrelationId

StartedAt

UpdatedAt

terminal/completion timestamp if justified

failure/recovery metadata only if supported by design

Rules:

do NOT duplicate existing Order business status

process state and Order status remain conceptually separate

no user-facing localized messages stored as authoritative process semantics

state transitions must be explicit and guarded

no arbitrary string state

timestamps through IClock / explicit now

IDs through IIdGenerator where applicable

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/process-state-model.md

4. Persistence Ownership

Persist process state inside Order-owned persistence.

Requirements:

Order owns schema/table/migration

no Host persistence

no Cart/Inventory schema writes from Order persistence

no cross-schema FK

no cross-module database FK

no shared mega-DbContext

migration belongs to Order Infrastructure

Add only the minimum persistence shape needed for W1.

Do NOT create:

distributed workflow tables in BuildingBlocks

generic “Saga” framework tables

shared cross-module persistence abstractions

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/process-state-persistence.md

5. Checkout Submission Idempotency

Implement W1 idempotency for checkout submission according to accepted design.

Requirements:

deterministic idempotency boundary for duplicate checkout submit

Order-owned durable record/state

duplicate submission cannot create a second process/order path

behavior must be compatible with existing API contract

no in-memory-only dedupe

no static dictionary

no cache-only authority

no magic timeout as correctness mechanism

If API already provides an idempotency/request token:
reuse it if design supports it.

If not:
introduce the minimum INTERNAL process identity necessary without breaking external API compatibility.

Do NOT invent a new mandatory public client header unless accepted design explicitly requires it.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/submission-idempotency.md

6. Integrate Foundation Into Current Submit Path Without Changing Business Outcome

Integrate process-state/idempotency foundation into current checkout path.

Critical rule:
CURRENT shared TransactionScope remains active.

Target behavior:

resolve/create durable checkout process identity

execute current checkout flow in same established order

update process state at safe internal milestones

preserve existing Order/Inventory/Cart writes and rollback behavior

mark completion only when current transaction/business flow succeeds

Do NOT:

split participant commits yet

send new production async workflow messages

change call ordering unless strictly required and behavior-equivalent

move point-of-no-return

change response semantics

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/submit-path-integration.md

7. State Transition Discipline

Implement only W1 states needed by current synchronous flow.

Do NOT pre-implement the entire future Saga state machine.

Allowed approach:

minimal initial states from accepted design

explicit transition methods

invalid transition protection

terminal-state protection where relevant

Record exact states implemented now vs deferred.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/state-transitions.md

8. Crash / Retry Characterization

Add tests proving W1 foundations support recovery semantics without claiming distributed recovery is complete.

At minimum:

duplicate submit with same idempotency identity does not create duplicate process/order

completed process returns/behaves deterministically according to current API semantics

invalid repeated transition is rejected

process state survives DbContext/process recreation

rollback path does not leave a falsely completed process

retry after safe pre-completion failure behaves according to accepted design

Do not fake process crash with brittle sleeps/timeouts.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/crash-retry-characterization.md

9. Outbox Compatibility

Audit how the new process state interacts with existing Order Outbox.

W1 must NOT introduce new distributed production messages unless the accepted migration plan explicitly requires a harmless internal event.

Preferred:

process-state mutation and any W1 Outbox entry, if needed, are committed atomically inside Order/local transaction semantics

But:

do not activate Process Manager message choreography

do not add broad Inbox yet unless W1 design explicitly requires a local idempotent-consumer primitive

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/outbox-compatibility.md

10. No Process Manager Runtime Yet

Do NOT implement:

background saga runner

orchestration loop

message-driven participant commands

compensation execution

timeout scheduler

distributed retries

RabbitMQ/SQL Transport workflow orchestration changes

cross-service HTTP/gRPC calls

This W1 is process-state + idempotency foundation only.

11. Current TransactionScope Must Remain

The current shared TransactionScope MUST remain in place.

Guard against accidental removal or partial splitting.

Add/retain characterization proving:

Order + Inventory + Cart current atomic behavior remains unchanged in W1

process-state integration does not weaken rollback semantics

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/transaction-preservation.md

12. CQRS / Layering

If a new submit command/handler is needed:

Handler in Order.Application

process-state persistence abstraction in Application/Domain as appropriate

EF implementation in Order.Infrastructure

Host remains transport only

Do not put business Handler in Infrastructure.

Do not create new foreign Application dependencies.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/layering.md

13. Error / Locale / Time / ID

Follow current locks:

semantic errors

no localized user-facing Domain/Application messages

unlimited-locale safe

IClock

IIdGenerator

no direct DateTime.UtcNow / Guid.NewGuid in protected new code

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/foundation-compliance.md

14. Source-Size / God-File Safety

No new handwritten source >800 LOC.
Existing oversized files are shrink-only.

Do not dump process state logic into existing giant Order files.

Prefer cohesive new files by responsibility:

process state model

transitions

persistence mapping

idempotency service/repository if needed

No ceremonial over-fragmentation either.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/source-size-compliance.md

15. Architecture Guards

Add/update guards so new checkout foundation cannot regress.

At minimum verify:

process state owned by Order only

no cross-schema FK

no new App→App edge

no new Domain→foreign Domain

no new Infra→foreign Domain

shared TransactionScope still present for W1

ARCH-CHECKOUT-001…005 remain active

no in-memory authoritative workflow state

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/architecture-guards.md

16. Tests

Required:

new process-state model tests

idempotency tests

duplicate-submit characterization

rollback/transaction preservation tests

existing Checkout/Order/Cart/Inventory focused tests

TMAR foundation tests

ArchitectureBoundaryTests

source-size guard

checkout architecture guards

No broad unrelated test-project creation.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/tests.md

17. Data Migration / Compatibility

If a new table is introduced:

migration must be additive

no destructive migration

existing orders/checkouts do not require unsafe backfill

nullability/defaults must be justified

deployment must remain compatible with existing data

Document:

forward migration

rollback strategy

whether existing in-flight checkout data exists/persists today

compatibility risk

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/data-migration.md

18. Implementation Readiness for W2

After W1, assess readiness for the next implementation step.

Return exactly:
Checkout-Implementation-W2-Readiness: READY
or
Checkout-Implementation-W2-Readiness: DEFER

READY should require:

durable process state works

idempotency works

current behavior unchanged

transaction preservation proven

no new boundary debt

next participant seam is clear

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/w2-readiness.md

19. Candidate W2 Scope

Do not implement W2 here.

Identify the safest next seam from accepted migration plan, likely one of:

explicit Inventory reservation contract boundary

explicit Cart conversion contract boundary

Order-owned Process Manager activation in-process but still synchronous

Inbox/idempotent-consumer primitive

Choose based on evidence.

Return:
W2-Candidate: <name>

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/w2-candidate.md

20. Orders Frontend Readiness

Because design marked frontend Orders READY_AFTER_DESIGN, reassess after W1 foundation.

Return:
Orders-Frontend-Readiness: READY_FOR_FE_BOUNDARY_WORK
or
Orders-Frontend-Readiness: STILL_WAITING_FOR_BACKEND_W2

Do NOT change frontend Orders.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/orders-frontend-readiness.md

21. Architecture Priority

Return exactly one:
Architecture-Priority: CHECKOUT_IMPLEMENTATION
Architecture-Priority: CONTRACTS
Architecture-Priority: FE_ORDERS
Architecture-Priority: FE_GODFILE

Prefer CHECKOUT_IMPLEMENTATION if W2 readiness is READY and no prerequisite debt blocks it.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/architecture-priority.md

22. Next Task Decision

Choose automatically:

A. TB-TMAR-CHECKOUT-IMPL-W2
if W2 readiness = READY and CHECKOUT_IMPLEMENTATION is highest value.

B. TB-TMAR-CONTRACTS-W7
if a prerequisite Contracts boundary blocks W2.

C. TB-TMAR-FE-ORDERS-W1
only if Orders frontend is READY_FOR_FE_BOUNDARY_WORK and frontend work now clearly outranks backend W2.

D. TB-TMAR-FE-GODFILE-W1
only if FE_GODFILE is highest value and readiness is READY.

Do not ask the user.

23. Product Safety

No user-visible behavior change.

Do NOT:

remove TransactionScope

split commits across contexts

alter price/inventory/promotion/order semantics

change public checkout API contract

change frontend

activate Saga

emit new externally observable workflow behavior

24. Recovery Updates

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md only if durable W1 guard is added

docs/architecture/TOOBA-CAPABILITY-MAP.md only if material ownership facts change

docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/recovery-sot.md

Record:

W1 process-state model

idempotency boundary

transaction preservation

W2 readiness

W2 candidate

Orders frontend readiness

next task

25. Acceptance Criteria

PASS only if:

accepted design maps cleanly to current code

durable Order-owned checkout process state exists

durable submission idempotency exists

duplicate submit cannot create duplicate process/order

current shared TransactionScope remains intact

current Order/Inventory/Cart behavior remains unchanged

no Process Manager runtime/Saga activation occurs

no new bad module dependency is introduced

no in-memory authoritative workflow state

additive migration is safe

source-size/layering guards pass

tests pass

W2 readiness assessed

W2 candidate selected

Orders frontend readiness assessed

user work preserved

canonical Result delivered

Worker stops completely

26. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Design-Code-Map
Process-State-Model
Process-State-Persistence
Submission-Idempotency
Submit-Path-Integration
State-Transitions
Crash-Retry-Characterization
Outbox-Compatibility
Transaction-Preservation
Layering
Foundation-Compliance
Architecture-Guards
Source-Size-Compliance
Data-Migration
Tests
Checkout-Implementation-W2-Readiness
W2-Candidate
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
