PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R4
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R3B-R3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_GOLDEN_IMPLEMENTATION_SLICE_4
Title: Move Inventory Recovery + Supply Authority Out of Host
Backend-Only: YES

Architect decision

R3B-R3 is ACCEPTED at commit:
51a3eed7c4b8eaf3daed28554a60df77692656e1

Order remains:
INCOMPLETE_REFERENCE_REPAIR

Reference pattern

Endpoint ownership/foldering:
Fulfillment-style

CQRS/MediatR/Result semantics:
Offer-style

Required flow:
Order.Endpoints
-> ISender
-> explicit Query/Command
-> IRequestHandler
-> Result / SemanticError
-> Order ports + foreign Contracts
-> Order.Infrastructure / owning module contracts

Exact R4 scope

Remove remaining Host business authority for:

OrderInventoryRecoveryComposer

OrderSupplyComposer

AdminOrderInventoryRecoverySupplyEndpoints

Routes:

GET /v1/admin/orders/inventory-recovery/audit

GET /v1/admin/orders/{checkoutId}/inventory-recovery

GET /v1/admin/orders/{checkoutId}/supply-status

Also eliminate temporary Host bridges that exist only because these composers live in Host:

HostAdminOrderOperationsInventoryRecoveryAdapter

HostAdminOrderOperationsSupplyAdapter

HostAdminOrderSupplyStatusReader

Only keep a Host adapter if it is genuinely auth/composition-only and contains no Order business logic.

Do NOT touch

StorefrontCheckoutComposer

StorefrontPendingPaymentComposer

StorefrontShippingComposer

Order detail Host surfaces

Checkout W6

Tax/Pricing/frontend

CQRS target

Create explicit Order use cases, e.g.:

Queries:

AuditOrderInventoryRecovery

AssessOrderInventoryRecovery

GetOrderSupplyStatus

Commands:

RecoverOrderInventoryReservation / EnsureOrderSupply only where currently required by real behavior

No mega command.
No generic code dispatcher.

Path and namespace must align.

Ownership rules

After R4:

Host must NOT own:

inventory recovery classification

recovery decision logic

supply status calculation

supply ensure/reacquire rules

OrderDbContext access for these paths

Inventory Application calls for these paths

business error mapping

Host may keep:

HostOrderAdminAuthorizer

tiny security/session adapters only

Foreign boundaries

Order Application must use Contracts/ports only.

Forbidden:

Order.Application -> Inventory.Application

Order.Application -> Inventory.Infrastructure

Order.Application -> Fulfillment.Application

foreign DbContext access

Expected failures must use typed/stable codes.
No ex.Message classification.

Behavior parity

Preserve current:

recovery classes/outcomes

audit behavior

recovery reason semantics

supply states

shortage projection

paid-durable ensure/reacquire behavior

permissions/auth

side effects/events

existing operation behavior that consumes recovery/supply state

Do not simplify.

Host delta evidence

Create:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R4/host-delta.md

Include:

Removed from Host

exact files/types removed

Still remaining in Host for Order

all remaining Order-related Host files after R4

Allowed Host adapters

only legitimate composition/security adapters

Guards

Add/strengthen durable guards proving:

Recovery/Supply Host composers absent

Recovery/Supply Host endpoint file absent

Order.Endpoints owns the three routes

ISender used

Order Application has no foreign Application/Infrastructure refs

no Host OrderDbContext for Recovery/Supply

no message parsing

no fake CQRS dispatcher

Validation

Run:

Order.Tests

Recovery/Supply focused tests

Order architecture guards

affected Inventory tests/guards

affected Host regression tests

dotnet build src/backend/Tooba.slnx

Recovery

On PASS:

record R4

Order remains INCOMPLETE_REFERENCE_REPAIR

next task must be evidence-driven

likely next slice = Storefront Order surfaces

do NOT start next task

Checkout remains:
PAUSED_AT_SAFE_W5_CHECKPOINT

PASS criteria

PASS only if:

OrderInventoryRecoveryComposer absent from Host.

OrderSupplyComposer absent from Host.

AdminOrderInventoryRecoverySupplyEndpoints absent from Host.

Temporary Host recovery/supply bridges removed or proven auth/composition-only.

Routes owned by Order.Endpoints via ISender.

Real CQRS handlers exist.

Behavior parity preserved.

Foreign boundaries Contracts-only.

No Host business DbContext authority for this slice.

Tests/build pass.

Host delta evidence complete.

Checkout W6 not started.

Frontend unchanged.

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R4
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R3B-R3
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Recovery-State:
Inventory-Recovery-State:
Order-Supply-State:
Order-Endpoint-State:
Order-CQRS-State:
Order-Host-DbAuthority:
Order-Foreign-Boundary:
Behavior-Parity:
Architecture-Guard-Validation:
Focused-Validation:
Full-Validation:
Host-Removed-This-Task:
Host-Still-Remaining-For-Order:
Allowed-Host-Adapters:
Residual-Defects:
Order-Overall-State:
Checkout-State:
Frontend-Production-Changes:
Recovery-Next-Task:
Git:
Blockers:
User-Work-Preserved:
Next-Recommended-Task:

END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.

Do not start the next task.
Do not migrate Storefront.
Do not start Checkout W6.
Do not poll.

END_TOOBA_TASK