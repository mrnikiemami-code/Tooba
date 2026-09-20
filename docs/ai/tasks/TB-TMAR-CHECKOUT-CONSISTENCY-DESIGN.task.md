PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN

Parent-Task:
TB-TMAR-HOST-W6

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
TMAR Checkout Consistency Design — Replace Shared-ACID Assumptions with a Migration-Safe Consistency Model

Task Type:
DESIGN + EVIDENCE + ARCHITECTURE LOCKS
NO SAGA IMPLEMENTATION IN THIS TASK

0. Architect Intent

HOST-W6 is accepted.

Verified state:

Host-Exit-State = READY_TO_PIVOT

Checkout-Consistency-Design-Readiness = READY

Architecture-Priority = CHECKOUT_DESIGN

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

remaining Host write debt is increasingly order/checkout-adjacent, ownership-ambiguous, or lower value

current Checkout flow still relies on shared-database ACID semantics

existing cross-context TransactionScope debt is already inventoried and frozen by ARCH-TX-001

Order/Application coupling has been materially reduced but still includes workflow-critical edges

frontend Orders migration is blocked by backend/workflow boundary clarity

Primary objective:
design, document, and lock the future microservice-safe consistency model for Checkout BEFORE any distributed workflow implementation.

This task must answer:

what business invariants Checkout must preserve

which bounded contexts participate

where the point-of-no-return is

what can fail before/after that point

what compensation is required

what idempotency is required

how retries, duplicate delivery, timeouts, recovery, and stuck workflows are handled

which interactions remain synchronous and which become event-driven

what state machine / process states are required

what contracts/events are needed later

how current shared-ACID flow maps to a future Process Manager/Saga model

NO implementation of Saga/Process Manager/Event choreography in this task.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-HOST-W6/recovery-sot.md

docs/evidence/TB-TMAR-CONTRACTS-W2/cross-context-transactions.md

docs/evidence/TB-TMAR-CONTRACTS-W2/cross-context-transactions.json

docs/evidence/TB-TMAR-BOUNDARY-V1/order-hub-analysis.md

latest App→App / Infra→App baselines

CheckoutDirectory and all directly participating write-path code

Verify:

branch main

exact HEAD SHA

exact origin/main SHA

HEAD == origin/main

18ca10c9 ancestor

known/clean worktree state

user work preserved

Expected previous accepted tip:
1714868d45bfd4cfce67c1edff67402278bfae6b

If conflicting tracked user work exists:
STOP with RECOVERY_CONFLICT.

No destructive Git operations.
No broad git add ..

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/recovery-start.md

2. Current Checkout Workflow Reconstruction

Reconstruct the CURRENT actual checkout write workflow from code.

Do not rely on prior summaries alone.

Document:

entry point(s)

Application service / Directory

TransactionScope or explicit transaction

participant modules

exact call ordering

exact writes

exact reads used as preconditions

error paths

rollback assumptions

outbox/integration event writes

whether cart conversion, order creation, inventory reservation, payment intent, promotion usage, tax, pricing, offer validation, etc. are inside or outside the same transaction

At minimum identify participation of:

Cart

Order

Inventory

Pricing

Offer

Promotion

Tax

Payment

Wallet if relevant

Fulfillment if relevant

any other actual participant discovered

Produce:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/current-checkout-workflow.md
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/current-checkout-workflow.json

The JSON should include ordered steps and participant ownership.

3. Business Invariants

Identify the business invariants the current shared transaction is protecting.

Do not infer generic ecommerce invariants unless code/evidence supports them.

For each invariant record:

invariant name

current enforcement location

participant modules

what failure would violate it

whether it must remain strongly consistent

whether eventual consistency is acceptable

whether compensation can restore correctness

Examples ONLY if supported by code:

order cannot confirm without inventory reservation

cart must not convert twice

canonical price must be revalidated

promotion usage must not exceed limits

stock must not oversell

payment capture must not happen for invalid order

duplicate checkout must not create duplicate order

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/checkout-invariants.md

4. Point-of-No-Return Analysis

Determine the actual business point(s) where rollback stops being possible or becomes unsafe.

Candidates may include:

external payment authorization/capture

irreversible wallet debit

inventory reservation confirmation

external fulfillment dispatch

final order confirmation

promotion quota consumption

Do not guess.
Use current workflow and integration semantics.

For each candidate:

reversible?

compensatable?

externally observable?

idempotent?

retry-safe?

can be delayed until after internal validation?

Return:
Checkout-Point-Of-No-Return: <identified milestone>
or
Checkout-Point-Of-No-Return: MULTI_STAGE

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/point-of-no-return.md

5. Failure Matrix

Build a failure matrix for each major workflow step.

For each step:

participant

operation

failure before commit

failure after local commit

timeout

duplicate message

retry

partial success

required compensation

manual intervention needed?

user-visible state

Create:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/failure-matrix.md
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/failure-matrix.json

Must cover at least:

inventory reservation failure

order creation failure

cart conversion failure

pricing revalidation failure

promotion failure

payment/wallet failure if in path

event publication failure

compensation failure

timeout between participants

duplicate command/event

6. Proposed Consistency Model

Design the future migration-safe workflow.

Choose based on actual evidence among:

Process Manager

Saga Orchestrator

Choreography

hybrid

Do NOT default to buzzwords.

Architectural preference:

one explicit business workflow coordinator is usually preferable for checkout if many ordered steps and compensations exist

participant modules keep local transaction ownership

no distributed transaction

each local write uses local ACID + Outbox where events are emitted

Document:

coordinator owner

why that owner is correct

orchestration vs choreography decision

synchronous commands vs async events

local transaction boundaries

recovery/retry model

compensation ownership

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/proposed-consistency-model.md

7. Proposed Checkout State Machine

Define explicit workflow states.

Do not reuse current Order status blindly if workflow state is conceptually different.

Separate:
A. Checkout workflow/process state
B. Order business status
if necessary.

Possible process states ONLY if evidence supports:

Started

Validating

InventoryReserved

OrderCreated

PaymentPending

PaymentAuthorized

CartCommitted

Completed

Compensating

Failed

ManualReview

Define:

state

entry condition

allowed next states

timeout policy

compensation path

terminal/non-terminal

user-visible meaning

Create:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/checkout-state-machine.md
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/checkout-state-machine.json

8. Idempotency Model

Define idempotency requirements.

At minimum consider:

checkout submission

order creation

inventory reservation

cart conversion

promotion consumption

payment authorization/capture

wallet debit/credit

compensation commands

integration event consumers

For each:

idempotency key source

storage owner

dedupe window/lifetime

replay semantics

response for duplicate request

Do not introduce implementation yet.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/idempotency-model.md

9. Retry / Timeout / Recovery Model

Define:

which operations may retry automatically

max retry policy conceptually

which failures must not retry blindly

timeout ownership

stuck workflow detection

recovery command/process

operator/manual intervention

poison message handling

dead-letter strategy if messaging transport supports it

replay safety

Do NOT hardcode magic retry numbers in code.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/retry-timeout-recovery.md

10. Compensation Model

For each reversible participant:

original action

compensation action

compensation owner

idempotency requirement

whether compensation is exact reversal or business correction

Examples only if supported:

release inventory reservation

restore cart

refund/credit wallet

cancel pending order

restore promotion quota

If an action has no safe compensation:
mark it as a point-of-no-return concern.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/compensation-model.md

11. Command / Event Boundary Design

Design future cross-module/service contracts.

Do NOT implement all of them.

Classify each interaction:

SYNC_COMMAND

SYNC_QUERY

ASYNC_COMMAND

DOMAIN_EVENT

INTEGRATION_EVENT

For each:

owner

producer

consumer

payload ownership

correlation ID

idempotency key

expected response/ack semantics

local transaction relationship

versioning concern

Examples might include:

ReserveInventory

ReleaseInventory

CreateOrder

CancelOrder

ConvertCart

AuthorizePayment

CheckoutCompleted

CheckoutFailed

Use actual workflow evidence.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/command-event-boundaries.md
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/command-event-boundaries.json

12. Outbox / Inbox Requirements

Audit current Outbox/Integration Event capability for checkout participants.

Document:

which participants already have Outbox

whether Inbox/dedupe exists

where exactly-once is NOT guaranteed

where at-least-once delivery must be assumed

what additional Inbox/idempotent-consumer support would be required before extraction

No broad messaging implementation.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/outbox-inbox-readiness.md

13. Data Ownership / Projection Needs

For each checkout decision currently requiring synchronous foreign-module data, classify future strategy:

CONTRACT_SYNC_QUERY

LOCAL_PROJECTION

EVENT_MAINTAINED_READ_MODEL

KEEP_SYNC_IN_PROCESS_UNTIL_EXTRACTION

NEEDS_DESIGN

Pay attention to:

price

tax

offer validity

promotion eligibility

inventory availability

cart state

seller data

Goal:
avoid turning current in-process hub into 6+ network calls after extraction.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/checkout-read-model-strategy.md

14. Order.Application Hub Decomposition Plan

Do NOT refactor it here.

Document current remaining foreign Application edges and classify each as:

replace with Contracts

move to local projection

process-manager interaction

integration event

retain sync temporarily

Create phased reduction plan:

W1 low-risk query dependencies

W2 workflow commands

W3 process-manager cutover

W4 service extraction readiness

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/order-hub-decomposition-plan.md

15. Frontend Orders Unblock Criteria

Frontend Orders was classified:
NEEDS_BACKEND/WORKFLOW_BOUNDARY_FIRST

Define exact criteria to unblock frontend Orders migration.

At minimum:

backend order workflow boundary stable

order mutations have explicit Application contracts

workflow state semantics documented

no Host-owned hidden business truth

admin API can map to stable Order contracts

no dependency on shared-ACID implementation details

Return:
Orders-Frontend-Unblock-State: READY_AFTER_DESIGN
or
Orders-Frontend-Unblock-State: STILL_BLOCKED

No frontend implementation here.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/orders-frontend-unblock.md

16. Architecture Locks

Add durable locks to:
docs/architecture/TMAR-architecture-locks.md

At minimum:

ARCH-CHECKOUT-001
No new checkout step may rely on cross-bounded-context shared ACID.

ARCH-CHECKOUT-002
All future checkout participant operations must be idempotency-designable and retry-safe at the contract boundary.

ARCH-CHECKOUT-003
Irreversible external side effects must occur only after required preconditions and point-of-no-return rules are explicitly satisfied.

ARCH-CHECKOUT-004
Cross-context checkout integration must use owned Contracts/Commands/Events; no new foreign Application/Domain coupling.

ARCH-CHECKOUT-005
Workflow state must be recoverable after process crash/restart; no in-memory-only authoritative workflow state.

Only add locks that are justified by evidence.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/checkout-locks.md

17. Migration Plan

Produce an incremental migration plan from current shared-ACID monolith to future process-manager model.

Must be reversible and staged.

Suggested stages:

Stage 0:
document/guard only

Stage 1:
introduce explicit workflow state + idempotency primitives inside monolith

Stage 2:
move one participant interaction behind explicit contract while still in-process

Stage 3:
introduce local Outbox/Inbox for workflow messages

Stage 4:
activate Process Manager in monolith

Stage 5:
remove shared cross-context TransactionScope

Stage 6:
extract first participant service

But derive exact stages from evidence.

For each stage:

prerequisites

code changes

data changes

tests

rollback

risk

exit criteria

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/checkout-migration-plan.md

18. No Implementation Scope

This task MUST NOT:

implement Saga

implement Process Manager

remove current TransactionScope

change checkout behavior

change order/cart/inventory/payment state transitions

introduce new distributed messages in production

create new DB tables for workflow state

change public APIs

change frontend

alter product behavior

This is DESIGN + LOCKS + EVIDENCE only.

19. Tests / Validation

Because production behavior must not change:

Run:

architecture tests

existing Checkout/Order/Cart/Inventory relevant tests

TMAR foundation tests

transaction guard tests

source-size guard

No need to force broad product-suite changes.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/tests.md

20. Readiness Decision

Return exactly:

Checkout-Consistency-Implementation-Readiness: READY_FOR_IMPLEMENTATION_W1
or
Checkout-Consistency-Implementation-Readiness: NEEDS_MORE_DESIGN

READY only if:

invariants documented

participant map complete

point-of-no-return understood

failure/compensation model complete

idempotency/retry/recovery defined

state machine defined

command/event boundaries defined

migration plan staged

no major ownership unknown remains

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/implementation-readiness.md

21. Architecture Priority

Return exactly one:
Architecture-Priority: CHECKOUT_IMPLEMENTATION
Architecture-Priority: CONTRACTS
Architecture-Priority: HOST
Architecture-Priority: FE_GODFILE

Guidance:

CHECKOUT_IMPLEMENTATION only if implementation readiness is READY_FOR_IMPLEMENTATION_W1

CONTRACTS if design exposes prerequisite contract debt

HOST if hidden Host write truth still blocks checkout

FE_GODFILE only if frontend risk clearly outranks backend consistency

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/architecture-priority.md

22. Next Task Decision

Choose automatically:

A. TB-TMAR-CHECKOUT-IMPL-W1
only if readiness = READY_FOR_IMPLEMENTATION_W1 and CHECKOUT_IMPLEMENTATION is highest value.

B. TB-TMAR-CONTRACTS-W7
if Contracts prerequisites block implementation.

C. TB-TMAR-HOST-W7
if hidden Host workflow ownership blocks implementation.

D. TB-TMAR-FE-GODFILE-W1
only if FE_GODFILE is highest value and readiness is ready.

Do not ask the user.

23. Recovery Updates

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md only if material ownership facts change

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/recovery-sot.md

Record:

design task accepted state

checkout participant map

point-of-no-return result

implementation readiness

Architecture-Priority

next task

24. Acceptance Criteria

PASS only if:

current checkout workflow reconstructed from code

business invariants documented

point-of-no-return analyzed

failure matrix complete

future consistency model chosen and justified

workflow state machine documented

idempotency model documented

retry/timeout/recovery model documented

compensation model documented

command/event boundaries documented

Outbox/Inbox readiness assessed

read-model/network-call strategy documented

Order hub decomposition plan documented

frontend Orders unblock criteria documented

checkout locks added

staged migration plan exists

no production workflow behavior changed

implementation readiness assessed

Architecture-Priority returned

user work preserved

next task chosen automatically

canonical Result delivered

Worker stops completely

25. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Current-Checkout-Workflow
Checkout-Invariants
Checkout-Point-Of-No-Return
Failure-Matrix
Proposed-Consistency-Model
Checkout-State-Machine
Idempotency-Model
Retry-Timeout-Recovery
Compensation-Model
Command-Event-Boundaries
Outbox-Inbox-Readiness
Checkout-Read-Model-Strategy
Order-Hub-Decomposition-Plan
Orders-Frontend-Unblock-State
Checkout-Locks
Checkout-Migration-Plan
Tests
Checkout-Consistency-Implementation-Readiness
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
