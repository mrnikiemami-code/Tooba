PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001-R1
Parent-Task: TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_VALIDATOR_COVERAGE_REPAIR
Title: Complete FluentValidation Coverage for Order Transport Requests
Backend-Only: YES

Architect verdict

Parent quality task is PARTIALLY ACCEPTED.

Accepted:

Application capability foldering

OrderContracts split

path↔namespace alignment

canonical shared ValidationBehavior registration

FluentValidation assembly discovery

stable validation error mapping

MediatR 12.5.0

Rejected:

validator coverage is incomplete

Verified examples with meaningful primitive/input schema but no validator:

DeleteAdminOrderNoteCommand

GetStorefrontCheckoutQuery

HidePendingPaymentCardCommand

CommitStorefrontShippingCommand

SaveStorefrontShippingSelectionCommand

ProjectStorefrontShippingQuery

many Admin Operations commands where only CancelOrder was treated as representative

Exact goal

Complete Order transport-facing input validation coverage.

Do NOT:

change foldering again

redesign ValidationBehavior

change SafeErrorMapper

change endpoint routes

change business logic

change Domain

resume Checkout W6

touch frontend

touch unrelated modules

Canonical validation semantics

Keep current shared foundation:

MediatR
-> ValidationBehavior
-> FluentValidation
-> ValidationException
-> SafeErrorMapper
-> validation.failed + stable field ErrorCodes

This is ACCEPTED for this task.

Do not create a second pipeline.

Required exhaustive audit

Enumerate every Order IRequest<T> Command/Query reachable from Order.Endpoints.

For each request classify exactly:

VALIDATOR_REQUIRED

NO_VALIDATOR_REQUIRED

NO_VALIDATOR_REQUIRED is allowed only when request has no meaningful transport/context primitive validation.

Examples:

parameterless dashboard metric query

pure list query with no user input

A request with Guid/string/body/page/range/version/code input is normally VALIDATOR_REQUIRED.

Required validator coverage

At minimum add validators for these verified gaps where applicable:

Admin completeness

DeleteAdminOrderNoteCommand

GetAdminOrderInvoiceQuery

GetAdminOrderReceiptQuery

ListAdminOrderNotesQuery

any other request with CheckoutId/NoteId/page/take input

Admin operations

Audit ALL operation Commands:

ApproveReturn

AssignConsolidatedPackageTracking

AssignTracking

CancelConsolidatedPackage

CancelShipment

ConfirmDeposit

CorrectTracking

CreateConsolidatedPackage

CreateShipment

DeliverConsolidatedPackage

DeliverShipment

DispatchConsolidatedPackage

DispatchShipment

MarkFulfillmentPacked

MarkFulfillmentProcessing

PackFulfillmentSelected

RecoverInventoryReservation

RejectDeposit

RejectReturn

RequestReturn

RestoreCancelledOrder

RestoreDeposit

RetryRefund

UnconfirmDeposit

UnpackFulfillment

UnprocessFulfillment

plus CancelOrder already covered

Use shared reusable rule helpers/base validators where shapes repeat.
Do NOT duplicate large rule bodies 27 times.

Every concrete MediatR request that requires validation must resolve an IValidator<TRequest> through DI.

Admin recovery/supply/detail/grid

Audit:

AssessOrderInventoryRecoveryQuery

AuditOrderInventoryRecoveryQuery

GetOrderSupplyStatusQuery

GetAdminOrderOperationsQuery

ListAdminOrderReturnEligibilityQuery

QueryAdminCustomersGridQuery

GetSellerOrderCountsQuery if list input has constraints

other inputs

Storefront checkout

Audit:

GetStorefrontCheckoutQuery

PreviewStorefrontCheckoutQuery

SubmitStorefrontCheckoutCommand already covered

Pending payment

Audit:

CancelPendingCheckoutCommand already covered

HidePendingPaymentCardCommand

ListStorefrontPendingPaymentsQuery if it has meaningful input

Shipping

Required audit:

CommitStorefrontShippingCommand

SaveStorefrontShippingSelectionCommand

ProjectStorefrontShippingQuery

For shipping validate only syntactic input:

non-empty CartId

non-null Body

non-negative version

idempotency key required/max length

reasonable primitive string lengths/formats already defined by API contract

Do NOT validate method availability, destination eligibility, pricing/free threshold, delivery business policy, ownership, or cart state in FluentValidation.

Customer

Audit:

ListCustomerOrdersQuery

GetCustomerOrderDashboardSummaryQuery

GetCustomerOrderDetailQuery already covered

RetryCustomerUnpaidOrderCommand already covered

Seller

Audit:

ListSellerOrdersQuery

GetSellerOrderDashboardSummaryQuery

GetSellerOrderDetailQuery already covered

SellerPartyId/ActorUserId primitive non-empty checks belong in validator if request carries them.

Shared rule design

Where many requests have common fields such as:

CheckoutId

ActorUserId

SellerPartyId

operation Request.Code

Prefer reusable FluentValidation extensions/base components.

But PASS requires concrete request validators to be discoverable through DI.

Do not rely on "representative validator" coverage.

Business validation exclusion

Must remain OUTSIDE validators:

entity existence

ownership

access/scope

status transitions

cancellation eligibility

inventory availability

payment state

fulfillment state

return eligibility

settlement state

retry limit

abuse limits

reservation-cycle state

Stable codes

Reuse OrderValidationCodes.

Add missing codes only for primitive/schema input.

No raw English message as API identity.

Guard

Strengthen OrderValidationPipelineTests or add architecture guard that:

Enumerates Order endpoint-reachable Commands/Queries.

Maintains an explicit classification manifest:

VALIDATOR_REQUIRED

NO_VALIDATOR_REQUIRED

For every VALIDATOR_REQUIRED request, confirms an IValidator<TRequest> is resolvable.

Fails when a new input-bearing request is added without classification.

Does not accept one validator as "representative" for another concrete request.

Avoid brittle filename-only assumptions where type/DI inspection can be used.

Tests

Must prove:

several newly covered request types reject invalid input before handler

shared Admin Operations rules apply to more than one concrete command

shipping invalid primitive input short-circuits

valid requests still reach handler

business failures remain outside validators

validator auto-discovery works through actual AddToobaCqrsFoundation registration path

Preserve

Foldering from parent task exactly

Host authority = NONE

Order = COMPLETE_REFERENCE_PATTERN

MediatR = 12.5.0

current ValidationBehavior / SafeErrorMapper

Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT

frontend unchanged

Evidence

Update/create:

docs/evidence/TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001-R1/validator-coverage.md

Include complete table:

| Request | Endpoint family | Classification | Validator | Reason |

No vague "representative" rows.

Validation

Run:

full Tooba.Order.Tests

validator/architecture guards

Host endpoint ownership/reverse audit guards

BuildingBlocks foundation tests if touched

dotnet build src/backend/Tooba.slnx

PASS criteria

PASS only if:

Every endpoint-reachable Order Command/Query is classified.

Every meaningful input-bearing request has a concrete discoverable validator.

No "representative validator" substitution remains.

Shared rule reuse is used where sensible.

Business rules remain outside FluentValidation.

Stable validation codes only.

Actual pipeline short-circuits invalid requests.

Foldering remains clean.

Host authority remains NONE.

Full current-head slnx build passes.

Checkout W6 not started.

Frontend unchanged.

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001-R1
Parent-Task: TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Request-Audit-State:
Total-Endpoint-Reachable-Requests:
Validator-Required-Count:
No-Validator-Required-Count:
Validators-Added-This-Repair:
Admin-Operations-Coverage:
Shipping-Coverage:
Checkout-Pending-Coverage:
Customer-Seller-Coverage:
Concrete-DI-Resolution-State:
Business-Validation-Separation:
Validation-Error-Semantics:
Foldering-Preserved:
Architecture-Guard-Validation:
Focused-Validation:
Full-Validation:
Host-Authority-State:
Order-Final-State:
Checkout-State:
Frontend-Production-Changes:
Residual-Defects:
Git:
User-Work-Preserved:
Next-Recommended-Task:

END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.

Do not start Checkout W6.
Do not start another module.
Do not poll.

END_TOOBA_TASK
