PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R3B
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_GOLDEN_IMPLEMENTATION_SLICE_3
Title: Remove AdminOrderOperations Host Authority
Backend-Only: YES

Architect verification

R3 was independently verified at commit:
d24ac135bba21b80d8b04edc70afa2cc5c7674b3

Accepted R3 progress:

OrdersGrid migrated from Host to Order module

AdminOrdersGridQueryEngine removed from Host

Grid route uses Order.Endpoints -> ISender -> Query/Handler

Grid persistence lives in Order.Infrastructure

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT

Order is still INCOMPLETE.

Exact R3B goal

Move ONLY AdminOrderOperations business/application authority out of Host.

Current Host files in scope:

Host/Tooba.Host/Admin/AdminOrderOperationsEndpoints.cs

Host/Tooba.Host/Admin/AdminOrderOperationsComposer.cs

Host/Tooba.Host/Admin/AdminOrderOperationsModels.cs

Do NOT migrate in this task:

OrderInventoryRecoveryComposer itself

OrderSupplyComposer itself

StorefrontCheckout

StorefrontPendingPayment

StorefrontShipping

Order detail route

Checkout W6

Target architecture

Use Fulfillment for endpoint ownership structure.
Use Offer for CQRS/MediatR structure.

Required flow:
Order.Endpoints
-> ISender
-> explicit Command / Query
-> IRequestHandler
-> Result / Result<T>
-> Order Application ports
-> Order Infrastructure + foreign Contracts only

Endpoints

Move these routes into Order.Endpoints:

GET /v1/admin/orders/{checkoutId}/operations

POST /v1/admin/orders/{checkoutId}/operations

GET /v1/admin/orders/{checkoutId}/return-eligibility

Host implementation of these routes must be removed.
Authorization may remain a tiny Host adapter only.

CQRS structure

Do NOT create one mega ExecuteOperationCommand.

Create explicit use-case folders.

At minimum:
Queries:

GetAdminOrderOperations

ListAdminOrderReturnEligibility

Commands:

explicit commands per real operation family/use-case where behavior differs

Examples:

CancelOrder

RestoreCancelledOrder

ConfirmDeposit

RejectDeposit

RestoreDeposit

UnconfirmDeposit

RecoverInventoryReservation

MarkFulfillmentProcessing

MarkFulfillmentPacked

CreateShipment

AssignTracking

DispatchShipment

DeliverShipment

RequestReturn

ApproveReturn

RejectReturn

RetryRefund

ConsolidatedPackage operations

Path and namespace must align like Offer.

Host removal requirement

After R3B these must be absent from production Host:

AdminOrderOperationsEndpoints.cs

AdminOrderOperationsComposer.cs

AdminOrderOperationsModels.cs

Host must not retain:

operation business decisions

status transition rules

OrderDbContext access for these routes

fulfillment/return/payment/settlement orchestration for these routes

exception-message classification for these routes

Foreign boundaries

Allowed:

Fulfillment.Contracts

Returns.Contracts

Settlement.Contracts

Payment.Contracts

Inventory.Contracts

other stable Contracts only

Forbidden:

foreign Application

foreign Infrastructure

foreign Domain

foreign DbContext

Reuse valid Operations Contracts already added by R3.

Recovery / Supply dependency

AdminOrderOperations currently calls:

OrderInventoryRecoveryComposer

OrderSupplyComposer

Do NOT migrate those composers wholesale in R3B.

Use narrow ports/adapters only if needed.
Host adapter may remain temporarily only if it contains no Order business logic.
Document residuals explicitly.

Result semantics

Follow Offer:

expected failures => Result.Failure(new SemanticError(code))

endpoints => ApiResponseFactory

Forbidden:

PlatformHttpException in Order Application

ex.Message classification

localized exception prose as business identity

manual semantic Results.Json

Behavior parity

Preserve:

permissions

action visibility

cancel/restore rules

payment/deposit operations

fulfillment/shipment/package operations

return/refund operations

inventory recovery action visibility

supply-status dependent behavior

idempotency/concurrency semantics

existing side effects/events/outbox

Do not simplify behavior.

Guards

Add guards proving:

Host AdminOrderOperations files absent

Order endpoints own the routes

endpoints use ISender

handlers exist

path/namespace alignment

no Endpoint -> Infrastructure

no Order Application -> foreign Application/Infrastructure

no Host OrderDbContext for migrated operations path

no ex.Message classification in migrated Order path

Validation

Run:

Order.Tests

new AdminOrderOperations tests

Order architecture guards

focused Host tests for admin order operations

R2C regression tests

protected module guards if Contracts changed

dotnet build src/backend/Tooba.slnx

Frontend must not run/change.

Host delta evidence

Create:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R3B/host-delta.md

It must contain:

Removed from Host

list files/types/functions removed in R3B

Still remaining in Host for Order

list all remaining Order-related Host files/types

Allowed Host adapters

list only auth/composition/bootstrap adapters and why allowed

Recovery

If R3B PASS:
Order remains INCOMPLETE_REFERENCE_REPAIR unless repository audit proves no remaining Order business authority outside deferred slices.

Expected likely next task:
TB-TMAR-ORDER-GOLDEN-001-R4

Do NOT start it.

PASS criteria

PASS only if:

AdminOrderOperations endpoints removed from Host

AdminOrderOperations composer removed from Host

AdminOrderOperations models removed from Host or moved to proper Order layer

routes owned by Order.Endpoints

real MediatR CQRS

behavior parity preserved

foreign boundaries Contracts-only

no Host OrderDbContext authority for migrated operations

stable Result/SemanticError

guards pass

full backend build passes

Checkout remains W5 paused

protected modules remain complete

frontend unchanged

Host delta evidence complete

Otherwise:
Status = INCOMPLETE

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R3B
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R3
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Recovery-Start:
AdminOrderOperations-State:
Order-Endpoint-State:
Order-CQRS-State:
Order-CQRS-Physical-Structure:
Order-Host-DbAuthority:
Order-Foreign-Boundary:
Order-Result-Semantics:
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
Protected-Golden-Modules:
Frontend-Production-Changes:
Recovery-State:
Recovery-Next-Task:
Git:
Blockers:
User-Work-Preserved:
Next-Recommended-Task:

END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.

Do NOT:

start R4

migrate Storefront

start Checkout W6

select another module

poll

fetch next task

END_TOOBA_TASK